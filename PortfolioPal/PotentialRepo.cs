using System;
using Dapper;
using System.Data;
using System.Net.Http;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using PortfolioPal.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace PortfolioPal
{
    public class PotentialRepo : IPotentialRepo
    {
    # region props

        public int RequestsPerCycle = 50;
        private HttpClient _clientBroker;
        private HttpClient _clientIEX;
        
        private readonly IDbConnection _conn;
        private string APCA_API_KEY = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_key").ToString();
        private string APCA_API_SECRET = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_secret").ToString();
        private string APCA_API_URL = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_url").ToString();
        private string APCA_DATA_API_URL = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_data_url").ToString();
        private string IEX_API_KEY = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("iex_key_test").ToString();
        private string IEX_API_SECRET = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("iex_secret_test").ToString();
        private string IEX_API_URL = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("iex_url_test").ToString();
        public List<Potential> AllPotentialAssets {get; set;}
        public List<string> AllPotentialAssetsSymbols {get; set;}
        public List<Potential> FilteredPotentialAssets {get; set;}
        public List<Potential> SuggestedPotentialAssets {get; set;}
        public List<string> UserSelectedAssetsRequired {get; set;}
        public List<string> UserSelectedAssetsOptional {get; set;}
        public List<string> AllOptionalAssets {get; set;}
        public List<string> DividendAssets {get; set;}

        public PotentialRepo (IDbConnection conn)
        {
            _conn = conn;
            _clientBroker = new HttpClient();
            _clientIEX = new HttpClient();
            _clientBroker.DefaultRequestHeaders.Add("APCA-API-KEY-ID", APCA_API_KEY);       // fix this its messy.
            _clientBroker.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", APCA_API_SECRET);

            //var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
            //string connString = config.GetConnectionString("portfoliopal");
            //_conn = new MySqlConnection(connString);
        }
    # endregion

    # region misc functions
        public double GetDoubleJObj(string obj, string value)
        {
            return (double.TryParse(JObject.Parse(obj).GetValue(value).ToString(), out double objDouble)) ? objDouble : 0;
        }

        public async Task<string> AwaitClientGetReponse(HttpClient client, string url)
        {
            return await client.GetStringAsync(url);   
        }

        public double CalculateFrequency(Potential asset)
        {
            double freq = 0;
            if (asset.exDividendDate.Length > 0 && asset.nextDividendDate.Length > 0){
                var nextDate = DateTime.Parse(asset.nextDividendDate);
                var lastDate = DateTime.Parse(asset.exDividendDate);
                var dateDiff = nextDate - lastDate;
                freq = 365 / dateDiff.Days;
            }
            else if (asset.exDividendDate.Length > 0 || asset.nextDividendDate.Length > 0){
                freq = 1;
            }
            return freq;
        }
    # endregion

    # region handling functions

        public List<Potential> AddUserSelectedToPotentials(List<string> assets)
        {
            List<Potential> newPots = new List<Potential>();
            foreach (var a in assets){
                if (AllPotentialAssetsSymbols.Contains(a)){
                    newPots.Add(AllPotentialAssets.First(x => x.symbol == a));                    
                }
            }
            return newPots;
        }

        public void GetAllPotentials()
        {
            AllPotentialAssets = new List<Potential>();
            AllPotentialAssetsSymbols = new List<string>();
            AssetRepo aRepo = new AssetRepo(_conn);
            var potentials = aRepo.GetAllTradableAssets();
            for (var i = 0; i < potentials.Count(); i++){
                var pot = new Potential
                {
                    symbol = potentials[i].symbol,
                    assetID = potentials[i].assetID,
                    exchange = potentials[i].exchange,
                    assetClass = potentials[i].assetClass,
                    shortable = potentials[i].shortable
                };
                if (!AllPotentialAssets.Contains(pot))
                    AllPotentialAssets.Add(pot);
                if (!AllPotentialAssetsSymbols.Contains(pot.symbol))
                    AllPotentialAssetsSymbols.Add(pot.symbol);
            }
        }

        public void GetUserSelectedAssets()
        {
            AssetRepo aRepo = new AssetRepo(_conn);
            UserSelectedAssetsRequired = aRepo.GetUserSelectedRequiredDB();
            UserSelectedAssetsOptional = aRepo.GetUserSelectedOptionalDB();
        }

        public void AddMorePotentials(bool CheckForUserSelected = true)
        {
            List<string> currentDBSymbols = new List<string>();
            List<string> toAddUserSelected = new List<string>();
            List<Potential> toAddPotentials = new List<Potential>();
            if (UserSelectedAssetsRequired == null || UserSelectedAssetsOptional == null) {
                GetUserSelectedAssets();
            }
            var dbPotentials = GetPotentialDB();
            foreach (var d in dbPotentials){
                currentDBSymbols.Add(d.symbol);
            } var curDBSymsArr = currentDBSymbols.ToArray();
            if (CheckForUserSelected) {
                foreach (var r in UserSelectedAssetsRequired){
                    if (!currentDBSymbols.Contains(r))
                        toAddUserSelected.Add(r);
                }
                foreach (var o in UserSelectedAssetsOptional){
                    if (!currentDBSymbols.Contains(o)){
                        toAddUserSelected.Add(o);
                    }
                }
                if (toAddUserSelected.Count() > 0){
                    var addedPots = AddUserSelectedToPotentials(toAddUserSelected);
                    foreach (var a in addedPots){
                        AddPotentialDB(a);
                    }
                    AddMorePotentials(false);
                }
            }
            if (CheckForUserSelected || toAddUserSelected.Count() == 0) {
                if ((dbPotentials.Count()) < FilteredPotentialAssets.Count()) {
                    int moreAdded = 0;
                    var moreToAdd = (RequestsPerCycle < (FilteredPotentialAssets.Count() - (dbPotentials.Count()))) ? RequestsPerCycle : (FilteredPotentialAssets.Count() - dbPotentials.Count());
                    for (var i = 0 ; i < FilteredPotentialAssets.Count(); i++) {
                        if (!curDBSymsArr.Contains(FilteredPotentialAssets[i].symbol)) {
                            toAddPotentials.Add(FilteredPotentialAssets[i]);
                            moreAdded++;
                            if (moreToAdd <= moreAdded) { 
                                break; 
                            }
                        }
                    }
                }
                foreach (var add in toAddPotentials) {
                    AddPotentialDB(add);
                }
            }
        }

        public IEnumerable<Potential> GetPotentialPrices(IEnumerable<Potential> assets)
        {
            if (assets.Count() > 0){
                string stringSymbols = "";
                foreach (var a in assets){
                    stringSymbols += $"{a.symbol},";
                }
                stringSymbols = stringSymbols.Trim().Substring(0, stringSymbols.Trim().LastIndexOf(",") - 1);
                var pricesURL = $"{APCA_DATA_API_URL}/v2/stocks/bars/latest?symbols={stringSymbols}";
                var pricesResponse = AwaitClientGetReponse(_clientBroker, pricesURL);
                var prices = JObject.Parse(pricesResponse.Result).GetValue("bars");
                List<JToken> priceTokens = prices.ToList();
                List<string> priceSymbols = new List<string>();
                for (var t = 0; t < priceTokens.Count(); t++){
                    priceSymbols.Add(priceTokens[t].Path.Substring(priceTokens[t].Path.IndexOf(".") + 1));
                }
                foreach (var a in assets) {
                    a.price = (priceSymbols.Contains(a.symbol)) ? double.Parse(prices[a.symbol]["c"].ToString()) : 0;
                }
            }
            return assets;
        }

        public void FilterInitialPotentials()
        {
            FilteredPotentialAssets = new List<Potential>();
            foreach (var pot in AllPotentialAssets){
                if ((pot.exchange.ToLower() == "nasdaq" || pot.assetClass == "us_equity") && (!pot.symbol.Contains("."))) {
                    if (!FilteredPotentialAssets.Contains(pot)){
                        FilteredPotentialAssets.Add(pot);
                    }
                }
            }
        }

        public int GetSQLTableLength(string table)
        {
           
            //var checkTable = _conn.ExecuteScalar($"SHOW TABLES LIKE '{table}';");
            //if (checkTable != null){
            if (_conn.ExecuteScalar($"SHOW TABLES LIKE '{table}';") != null){
                return (int.TryParse(_conn.ExecuteScalar($"SELECT COUNT(*) FROM {table};").ToString(), out int len)) ? len : 0;
            } return 0; 
        }

        public void CreatePotentialViews()
        {
            // time to create views - split these into separate functions later please.
            // filtered assets (all assets from NASDAQ exchange and US_EQUITY assetClass)
            _conn.Execute("CREATE OR REPLACE VIEW `allfilteredpotentials` AS SELECT potentials.* FROM potentials " +
            "WHERE potentials.exchange = 'NASDAQ' OR potentials.exchange = 'NYSE' AND potentials.assetClass = 'us_equity';");
            // updated assets (all assets from filtered, which are updated)
            _conn.Execute("CREATE OR REPLACE VIEW `updatedpotentials` AS SELECT allfilteredpotentials.* FROM allfilteredpotentials " +
            "WHERE allfilteredpotentials.price > 0 AND DATE(allfilteredpotentials.updated) > 0 ORDER BY updated DESC;");
            // expired assets (all assets from filtered, which are updated but past expiration)
            _conn.Execute("CREATE OR REPLACE VIEW `expiredpotentials` AS SELECT updatedpotentials.* FROM updatedpotentials " + 
            "WHERE DATE(updatedpotentials.updated) > 0 AND DATE(updatedpotentials.updated) <= CURDATE() -  INTERVAL 30 DAY;");
            // non updated/expired assets (all assets which are not updated or have been updated and are now expired)
            _conn.Execute("CREATE OR REPLACE VIEW `notupdatedpotentials` AS SELECT allfilteredpotentials.* FROM allfilteredpotentials " +
            "WHERE allfilteredpotentials.updated IS NULL OR DATE(allfilteredpotentials.updated) <= CURDATE() - INTERVAL 30 DAY;");
            // selected assets P.I ((all assets to updated (limit/max assets to update ~ 50?)
            _conn.Execute("CREATE OR REPLACE VIEW `notupdateduserselected` AS SELECT notupdatedpotentials.* FROM notupdatedpotentials " +
            "INNER JOIN userselectedassets ON notupdatedpotentials.symbol = userselectedassets.symbol;");
            // selected assets P.II ((all assets to updated (limit/max assets to update ~ 50?)
            _conn.Execute("CREATE OR REPLACE VIEW `toupdatepotentials` AS SELECT notupdatedpotentials.* FROM notupdatedpotentials " +
            "UNION " +
            "SELECT notupdateduserselected.* FROM notupdateduserselected " +
            "ORDER BY updated ASC " +
            "LIMIT 75;");
            // suggested assets - top scoring boys.
            _conn.Execute("CREATE OR REPLACE VIEW `allsuggestedassets` AS SELECT allfilteredpotentials.* FROM allfilteredpotentials " +
            "LEFT JOIN expiredpotentials ON allfilteredpotentials.assetID = expiredpotentials.assetID " +
            "WHERE allfilteredpotentials.SCORE > 0 ORDER BY allfilteredpotentials.SCORE DESC LIMIT 100;");
            // suggested assets (dividends - filtered and updated dividend assets with highest scores)
            // do dividend first then we can dump the rest to the suggested trade you lazy boy :)
            _conn.Execute("CREATE OR REPLACE VIEW `suggesteddividendassets` AS SELECT allsuggestedassets.* FROM allsuggestedassets " +
            "WHERE dividendYield > 0 ORDER BY allsuggestedassets.SCORE DESC LIMIT 10;");
            // suggested assets (trade - filtered and updated trade assets with highest scores (ignores scores from dividends))
            _conn.Execute("CREATE OR REPLACE VIEW `suggestedtradeassets` AS SELECT allsuggestedassets.* FROM allsuggestedassets " +
            "LEFT JOIN suggesteddividendassets ON allsuggestedassets.assetID = suggesteddividendassets.assetID " +
            "WHERE suggesteddividendassets.assetID IS NULL;");
        }

        public IEnumerable<Potential> QueryView(string view)
        {   // probs change this to a switch case thing and have more control with tables/views. neat.
            List<string> acceptableViews = new List<string>()
            { "allfilteredpotentials", "updatedpotentials", "expiredpotentials",
                "notupdatedpotentials", "notupdateduserselected", "toupdatepotentials",
                "suggestedtradeassets", "suggesteddividendassets", "allsuggestedassets" };
            if (!acceptableViews.Contains(view.ToLower())){
                view = "toupdatepotentials";
            }
            return _conn.Query<Potential>($"SELECT * FROM {view};");
        }

        public int GetStatsCredits()
        {
            // do we need this http _clientIEX thing here? probs not. I hope not.
            var iexCreditsURL = $"{IEX_API_URL}/stable/account/metadata?token={IEX_API_SECRET}";
            try {
                var iexCreditsResponse = AwaitClientGetReponse(_clientIEX, iexCreditsURL);
                var creditsLimit = double.Parse(JObject.Parse(iexCreditsResponse.Result.ToString()).GetValue("creditLimit").ToString());
                var currentCreditsUsed = double.Parse(JObject.Parse(iexCreditsResponse.Result.ToString()).GetValue("creditsUsed").ToString());
                return Convert.ToInt32(creditsLimit - currentCreditsUsed);
            } catch { return 0; }
        }

        public void IEXStats(Potential asset)
        {
            // hey it fucked up on an unknown symbol. it had a period. 
            // so you include exclude period symbols in initial filtering of potentials. cool thanks. !!!
            // credits = 5
            var iexStatsURL = $"{IEX_API_URL}/stable/stock/{asset.symbol}/stats/?token={IEX_API_KEY}";
            var iexStatsResponse = AwaitClientGetReponse(_clientIEX, iexStatsURL);
            var iexStats = JObject.Parse(iexStatsResponse.Result.ToString()).ToString();
            asset.companyName = (JObject.Parse(iexStats).ContainsKey("companyName")) ? JObject.Parse(iexStats).GetValue("companyName").ToString() : "whoops";
            asset.marketCap = (JObject.Parse(iexStats).ContainsKey("marketcap")) ? GetDoubleJObj(iexStats, "marketcap") : 0;
            asset.week52High = (JObject.Parse(iexStats).ContainsKey("week52high")) ? GetDoubleJObj(iexStats, "week52high") : 0;
            asset.week52Low = (JObject.Parse(iexStats).ContainsKey("week52low")) ? GetDoubleJObj(iexStats, "week52low") : 0;
            asset.week52Change = (JObject.Parse(iexStats).ContainsKey("week52change")) ? GetDoubleJObj(iexStats, "week52change") : 0;
            asset.avg10Volume = (JObject.Parse(iexStats).ContainsKey("avg10Volume")) ? GetDoubleJObj(iexStats, "avg10Volume") : 0;
            asset.avg30Volume = (JObject.Parse(iexStats).ContainsKey("avg30Volume")) ? GetDoubleJObj(iexStats, "avg30Volume") : 0;
            asset.day200MovingAvg = (JObject.Parse(iexStats).ContainsKey("day200MovingAvg")) ? GetDoubleJObj(iexStats, "day200MovingAvg") : 0;
            asset.day50MovingAvg = (JObject.Parse(iexStats).ContainsKey("day50MovingAvg")) ? GetDoubleJObj(iexStats, "day50MovingAvg") : 0;
            asset.employees = (JObject.Parse(iexStats).ContainsKey("employees")) ? GetDoubleJObj(iexStats, "employees") : 0;
            asset.ttmEPS = (JObject.Parse(iexStats).ContainsKey("ttmEPS")) ? GetDoubleJObj(iexStats, "ttmEPS") : 0;
            asset.ttmDividendRate = (JObject.Parse(iexStats).ContainsKey("ttmDividendRate")) ? GetDoubleJObj(iexStats, "ttmDividendRate") : 0;
            asset.dividendYield = (JObject.Parse(iexStats).ContainsKey("dividendYield")) ? GetDoubleJObj(iexStats, "dividendYield") : 0;
            asset.nextDividendDate = (JObject.Parse(iexStats).ContainsKey("nextDividendDate")) ? JObject.Parse(iexStats).GetValue("nextDividendDate").ToString() : "";
            asset.exDividendDate = (JObject.Parse(iexStats).ContainsKey("exDividendDate")) ? JObject.Parse(iexStats).GetValue("exDividendDate").ToString() : "";
            asset.dividendFrequency = CalculateFrequency(asset);
            asset.peRatio = (JObject.Parse(iexStats).ContainsKey("peRatio")) ? GetDoubleJObj(iexStats, "peRatio") : 0;
            asset.beta = (JObject.Parse(iexStats).ContainsKey("beta")) ? GetDoubleJObj(iexStats, "beta") : 0;
            asset.maxChangePercent = (JObject.Parse(iexStats).ContainsKey("maxChangePercent")) ? GetDoubleJObj(iexStats, "maxChangePercent") : 0;
            asset.year5ChangePercent = (JObject.Parse(iexStats).ContainsKey("year5ChangePercent")) ? GetDoubleJObj(iexStats, "year5ChangePercent") : 0;
            asset.year2ChangePercent = (JObject.Parse(iexStats).ContainsKey("year2ChangePercent")) ? GetDoubleJObj(iexStats, "year2ChangePercent") : 0;
            asset.year1ChangePercent = (JObject.Parse(iexStats).ContainsKey("year1ChangePercent")) ? GetDoubleJObj(iexStats, "year1ChangePercent") : 0;
            asset.ytdChangePercent = (JObject.Parse(iexStats).ContainsKey("ytdChangePercent")) ? GetDoubleJObj(iexStats, "ytdChangePercent") : 0;
            asset.month6ChangePercent = (JObject.Parse(iexStats).ContainsKey("month6ChangePercent")) ? GetDoubleJObj(iexStats, "month6ChangePercent") : 0;
            asset.month3ChangePercent = (JObject.Parse(iexStats).ContainsKey("month3ChangePercent")) ? GetDoubleJObj(iexStats, "month3ChangePercent") : 0;
            asset.month1ChangePercent = (JObject.Parse(iexStats).ContainsKey("month1ChangePercent")) ? GetDoubleJObj(iexStats, "month1ChangePercent") : 0;
            asset.day30ChangePercent = (JObject.Parse(iexStats).ContainsKey("day30ChangePercent")) ? GetDoubleJObj(iexStats, "day30ChangePercent") : 0;
            asset.day5ChangePercent = (JObject.Parse(iexStats).ContainsKey("day5ChangePercent")) ? GetDoubleJObj(iexStats, "day5ChangePercent") : 0;
        }

        public List<Potential> GetBatchIEXStats(IEnumerable<Potential> assets, int creditCost = 5)
        {
            List<Potential> updatedStats = new List<Potential>();
            var limit = (Math.Min(assets.Count(), RequestsPerCycle));
            var remainingCredits = GetStatsCredits();
            if (remainingCredits > (creditCost)) {
                foreach (var a in assets) {
                    IEXStats(a);
                    updatedStats.Add(a);
                    remainingCredits -= creditCost;
                    if (remainingCredits < creditCost) { 
                        break; 
                    } 
                    else {
                        Thread.Sleep(200); 
                    } 
                }
            }
            return updatedStats;
        }
    # endregion

    #region more SQL stuff
        public IEnumerable<Potential> GetPotentialDB()
        {
            return _conn.Query<Potential>("SELECT * FROM POTENTIALS;");
        }
        
        public Potential GetPotentialDB(Potential potential)
        {
            return _conn.QuerySingle<Potential>("SELECT * FROM POTENTIALS WHERE assetID = @potential.assetID", 
                new {assetID = potential.assetID});
        }

        public void AddPotentialDB(Potential potential)
        {
            _conn.Execute("INSERT INTO potentials (ASSETID, SCORE, SYMBOL, PRICE, COMPANYNAME, MARKETCAP, WEEK52HIGH, WEEK52LOW, WEEK52CHANGE, " +
                "AVG10VOLUME, AVG30VOLUME, DAY200MOVINGAVG, DAY50MOVINGAVG, EMPLOYEES, TTMEPS, TTMDIVIDENDRATE, DIVIDENDYIELD, NEXTDIVIDENDDATE, " +
                "EXDIVIDENDDATE, DIVIDENDFREQUENCY, PERATIO, BETA, MAXCHANGEPERCENT, YEAR5CHANGEPERCENT, YEAR2CHANGEPERCENT, YEAR1CHANGEPERCENT, " +
                "YTDCHANGEPERCENT, MONTH6CHANGEPERCENT, MONTH3CHANGEPERCENT, MONTH1CHANGEPERCENT, DAY30CHANGEPERCENT, DAY5CHANGEPERCENT, SHORTABLE, " +
                "EXCHANGE, ASSETCLASS) VALUES " +
                "(@assetID, @score, @symbol, @price, @companyName, @marketCap, @week52High, @week52Low, @week52Change, @avg10Volume, @avg30Volume, " +
                "@day200MovingAvg, @day50MovingAvg, @employees, @ttmEPS, @ttmDividendRate, @dividendYield, @nextDividendDate, @exDividendDate, " +
                "@dividendFrequency, @peRatio, @beta, @maxChangePercent, @year5ChangePercent, @year2ChangePercent,@year1ChangePercent, @ytdChangePercent, " +
                "@month6ChangePercent, @month3ChangePercent, @month1ChangePercent, @day30ChangePercent, @day5ChangePercent, @shortable, @exchange, @assetClass) " +
                "ON DUPLICATE KEY UPDATE assetID = assetID;",
            new 
            {
                assetID = potential.assetID, score = potential.score, symbol = potential.symbol, price = potential.price,
                companyName = potential.companyName, marketCap = potential.marketCap, week52High = potential.week52High, week52Low = potential.week52Low, 
                week52Change = potential.week52Change, avg10Volume = potential.avg10Volume, avg30Volume = potential.avg30Volume, 
                day200MovingAvg = potential.day200MovingAvg, day50MovingAvg = potential.day50MovingAvg, employees = potential.employees, 
                ttmEPS = potential.ttmEPS, ttmDividendRate = potential.ttmDividendRate, dividendYield = potential.dividendYield, 
                nextDividendDate = potential.nextDividendDate, exDividendDate = potential.exDividendDate, dividendFrequency = potential.dividendFrequency, 
                peRatio = potential.peRatio, beta = potential.beta, maxChangePercent = potential.maxChangePercent, 
                year5ChangePercent = potential.year5ChangePercent, year2ChangePercent = potential.year2ChangePercent, 
                year1ChangePercent = potential.year1ChangePercent, ytdChangePercent = potential.ytdChangePercent, month6ChangePercent = potential.month6ChangePercent, 
                month3ChangePercent = potential.month3ChangePercent, month1ChangePercent = potential.month1ChangePercent, day30ChangePercent = potential.day30ChangePercent, 
                day5ChangePercent = potential.day5ChangePercent, shortable = potential.shortable, exchange = potential.exchange, assetClass = potential.assetClass
            });
        }

        public void UpdatePotentialDB(Potential potential, bool updateStamp = true)
        {   
            string timestamp = " ";
            if (updateStamp) {
                timestamp = ", UPDATED = @updated ";
            }
            _conn.Execute("UPDATE POTENTIALS SET SYMBOL = @symbol, SCORE = @score, PRICE = @price, COMPANYNAME = @companyName, MARKETCAP = @marketCap, " + 
            "WEEK52HIGH = @week52High, WEEK52LOW = @week52Low, WEEK52CHANGE = @week52Change, AVG10VOLUME = @avg10Volume, AVG30VOLUME = @avg30Volume, " + 
            "DAY200MOVINGAVG = @day200MovingAvg, DAY50MOVINGAVG = @day50MovingAvg, EMPLOYEES = @employees, TTMEPS = @ttmEPS, " + 
            "TTMDIVIDENDRATE = @ttmDividendRate, DIVIDENDYIELD = @dividendYield, NEXTDIVIDENDDATE = @nextDividendDate, EXDIVIDENDDATE = @exDividendDate, " + 
            "DIVIDENDFREQUENCY = @dividendFrequency, PERATIO = @peRatio, BETA = @beta, MAXCHANGEPERCENT = @maxChangePercent, " + 
            "YEAR5CHANGEPERCENT = @year5ChangePercent, YEAR2CHANGEPERCENT = @year2ChangePercent, YEAR1CHANGEPERCENT = @year1ChangePercent, " + 
            "YTDCHANGEPERCENT = @ytdChangePercent, MONTH6CHANGEPERCENT = @month6ChangePercent, MONTH3CHANGEPERCENT = @month3ChangePercent, " + 
            "MONTH1CHANGEPERCENT = @month1ChangePercent, DAY30CHANGEPERCENT = @day30ChangePercent, DAY5CHANGEPERCENT = @day5ChangePercent, " + 
            $"SHORTABLE = @shortable, EXCHANGE = @exchange, ASSETCLASS = @assetClass{timestamp}WHERE ASSETID = @assetID", 
                new 
                { 
                    symbol = potential.symbol, score = potential.score, price = potential.price, companyName = potential.companyName, 
                    marketCap = potential.marketCap, week52High = potential.week52High, week52Low = potential.week52Low, 
                    week52Change = potential.week52Change, avg10Volume = potential.avg10Volume, avg30Volume = potential.avg30Volume, 
                    day200MovingAvg = potential.day200MovingAvg, day50MovingAvg = potential.day50MovingAvg, employees = potential.employees, 
                    ttmEPS = potential.ttmEPS, ttmDividendRate = potential.ttmDividendRate, dividendYield = potential.dividendYield, 
                    nextDividendDate = potential.nextDividendDate, exDividendDate = potential.exDividendDate, dividendFrequency = potential.dividendFrequency, 
                    peRatio = potential.peRatio, beta = potential.beta, maxChangePercent = potential.maxChangePercent, 
                    year5ChangePercent = potential.year5ChangePercent, year2ChangePercent = potential.year2ChangePercent, 
                    year1ChangePercent = potential.year1ChangePercent, ytdChangePercent = potential.ytdChangePercent, month6ChangePercent = potential.month6ChangePercent, 
                    month3ChangePercent = potential.month3ChangePercent, month1ChangePercent = potential.month1ChangePercent, day30ChangePercent = potential.day30ChangePercent, 
                    day5ChangePercent = potential.day5ChangePercent, shortable = potential.shortable, exchange = potential.exchange, assetClass = potential.assetClass, updated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 
                    assetID = potential.assetID
                });
        }

        public void UpdatePotentialDB(IEnumerable<Potential> potentials, bool updateStamp = true)
        {   
            foreach (var p in potentials){
                UpdatePotentialDB(p, updateStamp);
            }
        }

        public void ClearAllPotentialDB()
        {
            _conn.Execute("DROP TABLE IF EXISTS `potentials`; " +
            "CREATE TABLE `potentials` (" +
                "`assetID` VARCHAR(100) PRIMARY KEY," +
                "`score`	 INT," +
                "`symbol` VARCHAR(100)," +
                "`price` FLOAT(5)," +
                "`companyName` VARCHAR(100)," +
                "`marketCap` FLOAT(10)," +
                "`week52High` FLOAT(5)," +
                "`week52Low` FLOAT(5)," +
                "`week52Change` FLOAT(10)," +
                "`avg10Volume` FLOAT(10)," +
                "`avg30Volume` FLOAT(10)," +
                "`day200MovingAvg` FLOAT(10)," +
                "`day50MovingAvg` FLOAT(10)," +
                "`employees` FLOAT(10)," +
                "`ttmEPS` FLOAT(10)," +
                "`ttmDividendRate` FLOAT(10)," +
                "`dividendYield` FLOAT(10)," +
                "`nextDividendDate` VARCHAR(25)," +
                "`exDividendDate` VARCHAR(25)," +
                "`dividendFrequency` FLOAT(10)," +
                "`peRatio` FLOAT(10)," +
                "`beta` FLOAT(10)," +
                "`maxChangePercent` FLOAT(10)," +
                "`year5ChangePercent` FLOAT(10)," +
                "`year2ChangePercent` FLOAT(10)," +
                "`year1ChangePercent` FLOAT(10)," +
                "`ytdChangePercent` FLOAT(10)," +
                "`month6ChangePercent` FLOAT(10)," +
                "`month3ChangePercent` FLOAT(10)," +
                "`month1ChangePercent` FLOAT(10)," +
                "`day30ChangePercent` FLOAT(10)," +
                "`day5ChangePercent` FLOAT(10)," +
                "`shortable` TINYINT," +
                "`exchange` VARCHAR(25)," +
                "`assetClass` VARCHAR(25)," +
                "`updated` TIMESTAMP);" +
            "DELETE FROM potentials WHERE potentials.assetID = NULL;");
        }

        public void ClearPotentialDB(Potential potential)
        {
            _conn.Execute("DELETE FROM POTENTIALS WHERE assetID = @assetID;", new { assetID = potential.assetID });
        }
        
        public void CalculateStarValue(Potential p)
        {
            double score = 0;
            score += (p.price >= 1 && p.price <= 200) ? 1 : 0;
            score += (p.avg10Volume >= (p.avg30Volume * 0.9)) ? 1 : 0;      
            score += p.ttmEPS / 10;
            score =- p.peRatio / 100;
            if (p.dividendYield > 0) {
                score += ((p.week52High - p.price) < (p.price - p.week52Low)) ? 1 : 0;
                score += (p.day50MovingAvg > p.day200MovingAvg) ? 1 : -1;
                score += (p.ttmDividendRate > 0) ? 1 : 0;
                score += (0.03 <= p.dividendYield && p.dividendYield <= 0.10) ? 1 : (0.02 < p.dividendYield && p.dividendYield < 0.03 || 0.10 < p.dividendYield && p.dividendYield <= 0.15) ? 0.5 : 0;
                score += (p.dividendFrequency > 3) ? 3 : (p.dividendFrequency > 0) ? p.dividendFrequency : 0 ;
                score -= p.beta;
            }
            if (p.shortable) {
                score += p.beta;
                double pattern = ((p.year5ChangePercent > 0 && (p.year2ChangePercent > 0 || p.year1ChangePercent > 0)) || (p.year5ChangePercent < 0 && ( p.year2ChangePercent < 0 || p.year1ChangePercent < 0))) ? (p.year5ChangePercent > 0) ? 1 : -1 : 0 ;
                score += (pattern > 0 && p.year5ChangePercent > 0 || pattern < 0 && p.year5ChangePercent < 0) ? 1 : 0; 
                score += (pattern > 0 && p.year2ChangePercent > 0 || pattern < 0 && p.year2ChangePercent < 0) ? 1 : 0; 
                score += (pattern > 0 && p.year1ChangePercent > 0 || pattern < 0 && p.year1ChangePercent < 0) ? 1 : 0; 
                score += (pattern > 0 && p.month6ChangePercent > 0 || pattern < 0 && p.month6ChangePercent < 0) ? 1 : 0;
                score += (pattern > 0 && p.month3ChangePercent > 0 || pattern < 0 && p.month3ChangePercent < 0) ? 1 : 0;
                score += (pattern > 0 && p.month1ChangePercent > 0 || pattern < 0 && p.month1ChangePercent < 0) ? 1 : 0;
                score += (p.day5ChangePercent > 0 && p.day30ChangePercent > 0 || p.day5ChangePercent < 0 && p.day30ChangePercent < 0) ? 1 : 0;
            }
            else if (!p.shortable) {
                score -= p.beta;
                score += (p.year5ChangePercent > 0 || p.year5ChangePercent < 0) ? 1 : 0; 
                score += (p.year2ChangePercent > 0 || p.year2ChangePercent < 0) ? 1 : 0; 
                score += (p.year1ChangePercent > 0 || p.year1ChangePercent < 0) ? 1 : 0; 
                score += (p.month6ChangePercent > 0 || p.month6ChangePercent < 0) ? 1 : 0;
                score += (p.month3ChangePercent > 0 || p.month3ChangePercent < 0) ? 1 : 0;
                score += (p.month1ChangePercent > 0 || p.month1ChangePercent < 0) ? 1 : 0;
                score += (p.day5ChangePercent > 0 && p.day30ChangePercent > 0 || p.day5ChangePercent < 0 && p.day30ChangePercent < 0) ? 1 : 0;
            }
            p.score = score;
        }

    #endregion

    }
}
