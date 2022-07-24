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
    public class AssetRepo : IAssetRepo
    {
        // you have a lot of work to do here.

        public int RequestsPerCycle = 50;
        private HttpClient _clientBroker;
        private readonly IDbConnection _conn;

        private string APCA_API_KEY = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_key").ToString();
        private string APCA_API_SECRET = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_secret").ToString();
        private string APCA_API_URL = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_url").ToString();
        private string APCA_DATA_API_URL = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_data_url").ToString();

        public List<Asset> AllTradableAssets {get; set;}
        public List<string> AllTradableAssetSymbols {get; set;}
        public List<Asset> FilteredAssets {get; set;}
        public List<string> UserSelectedAssetsRequired {get; set;}
        public List<string> UserSelectedAssetsOptional {get; set;}
        public List<string> AllOptionalAssets {get; set;}
        public List<string> DividendAssets {get; set;}


        public List<Asset> AllTradedAssets {get; set;}
        public List<string> AllTradedAssetsSymbols {get; set;}



        public AssetRepo (IDbConnection conn)
        {
            _conn = conn;
            _clientBroker = new HttpClient();
            _clientBroker.DefaultRequestHeaders.Add("APCA-API-KEY-ID", APCA_API_KEY);       // fix this its messy.
            _clientBroker.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", APCA_API_SECRET);

        }
        

        public async Task<string> AwaitClientGetReponse(HttpClient client, string url)
        {
            return await client.GetStringAsync(url);   
        }

        public List<Asset> GetAllTradableAssets()
        {
            AllTradableAssets = (AllTradableAssets == null) ? new List<Asset>() : AllTradableAssets;
            AllTradableAssetSymbols = (AllTradableAssetSymbols == null) ? new List<string>() : AllTradableAssetSymbols;
            var AllTradableURL = $"{APCA_API_URL}/v2/assets?status=active";
            var AllTradableResponse = AwaitClientGetReponse(_clientBroker, AllTradableURL);
            var assets = JArray.Parse(AllTradableResponse.Result.ToString()).ToList();
            for (var i = 0; i < assets.Count(); i++) {
                if (JObject.Parse(assets[i].ToString()).GetValue("tradable").ToString().ToLower() == "true")
                {
                    var a = new Asset();
                    a.symbol = JObject.Parse(assets[i].ToString()).GetValue("symbol").ToString();
                    a.assetID = JObject.Parse(assets[i].ToString()).GetValue("id").ToString();
                    a.exchange = JObject.Parse(assets[i].ToString()).GetValue("exchange").ToString();
                    a.assetClass = JObject.Parse(assets[i].ToString()).GetValue("class").ToString();
                    a.shortable = bool.Parse((JObject.Parse(assets[i].ToString()).GetValue("shortable").ToString()));
                    if (!AllTradableAssets.Contains(a))
                        AllTradableAssets.Add(a);
                    if (!AllTradableAssetSymbols.Contains(a.symbol))
                        AllTradableAssetSymbols.Add(a.symbol);
                }
            }
            return AllTradableAssets;
        }

        public Asset GetAsset(string symbol)
        {
            var assetURL = $"{APCA_API_URL}/v2/assets/{symbol}";
            var assetResponse = AwaitClientGetReponse(_clientBroker, assetURL).Result;
            //var assetInfo = JObject.Parse(assetResponse.Result);
            Asset asset = new Asset();
            asset.symbol = JObject.Parse(assetResponse).GetValue("symbol").ToString();
            asset.assetID = JObject.Parse(assetResponse).GetValue("id").ToString();
            asset.exchange = JObject.Parse(assetResponse).GetValue("exchange").ToString();
            asset.shortable = Convert.ToBoolean(JObject.Parse(assetResponse).GetValue("shortable"));
            return asset;
        }

        public List<Asset> GetAssetSnapShots(List<string> assets)
        {
            List<Asset> snaps = new List<Asset>();
            var assetURL = $"{APCA_DATA_API_URL}/v2/stocks/snapshots?symbols={string.Join(",", assets)}";
            var assetResponse = AwaitClientGetReponse(_clientBroker, assetURL).Result;
            var syms = JObject.Parse(assetResponse.ToString());
            foreach (var a in syms)
            {
                var symsMinBar = JObject.Parse(syms[a.Key].ToString())["minuteBar"];
                var symsLClose = JObject.Parse(syms[a.Key].ToString())["prevDailyBar"];
                Asset temp = new Asset();
                temp.symbol = a.Key;
                temp.price = Convert.ToDouble(JObject.Parse(symsMinBar.ToString()).GetValue("c"));
                temp.lastPrice = Convert.ToDouble(JObject.Parse(symsLClose.ToString()).GetValue("c"));
                temp.changeToday = temp.price - temp.lastPrice;
                snaps.Add(temp);
            }
            return snaps;
        }

        public List<ChartDataPoint> GetAssetPriceHistory(string symbol, int limit = 25, string start = "none", string timeframe = "15Min")
        {
            // query parameters for timeframe, start, end, limit, yada, yada
            // start/end must be in YYY-MM-DDT00:00:00Z
            // maybe try: dateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo);
            var startLabel = (start == "none") ? "" : $"&start={start}";
            var pricesURL = $"{APCA_DATA_API_URL}/v2/stocks/{symbol}/bars?timeframe={timeframe}&limit={limit}{startLabel}";
            var pricesResponse = AwaitClientGetReponse(_clientBroker, pricesURL).Result;
            var prices = JObject.Parse(pricesResponse.ToString()).GetValue("bars").ToList();
            List<ChartDataPoint> assetHistory = new List<ChartDataPoint>();
            foreach (var p in prices){
                var pricesClose = JObject.Parse(p.ToString()).GetValue("c");
                var pricesTime = JObject.Parse(p.ToString()).GetValue("t");
                assetHistory.Add(new ChartDataPoint(pricesTime.ToString(), Convert.ToDouble(pricesClose)));
            }
            return assetHistory;

        }

       public void UpdatePortfolioAssetStats(List<string> assets)
        {
            List<string> portfolioPositions = new List<string>();
            List<string> refreshPrices = new List<string>();
            var portfolioURL = $"{APCA_API_URL}/v2/positions";
            var portfolioResponse = _clientBroker.GetStringAsync(portfolioURL).Result;
            var portfolio = JArray.Parse(portfolioResponse).ToArray();
            for (int i = 0; i < portfolio.Length; i++)
            {
                var asset            = new Asset();
                asset.assetID        = JObject.Parse(portfolio[i].ToString()).GetValue("asset_id").ToString();
                asset.symbol         = JObject.Parse(portfolio[i].ToString()).GetValue("symbol").ToString();
                asset.exchange       = JObject.Parse(portfolio[i].ToString()).GetValue("exchange").ToString();
                asset.assetClass     = JObject.Parse(portfolio[i].ToString()).GetValue("asset_class").ToString();
                asset.qty            = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("qty").ToString());
                asset.side           = JObject.Parse(portfolio[i].ToString()).GetValue("side").ToString();
                asset.marketValue    = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("market_value").ToString());
                asset.costBasis      = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("cost_basis").ToString());
                asset.plDollarsTotal = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("unrealized_pl").ToString());
                asset.plPercentTotal = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("unrealized_plpc").ToString()) * 100;
                asset.plDollarsToday = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("unrealized_intraday_pl").ToString());
                asset.plPercentToday = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("unrealized_intraday_plpc").ToString()) * 100;
                asset.price          = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("current_price").ToString());
                asset.lastPrice      = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("lastday_price").ToString());
                asset.changeToday    = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("change_today").ToString());
                asset.priceBaseline = (asset.priceBaseline == 0) ? (asset.price != 0) ? asset.price : 0 : asset.priceBaseline;
                asset.assetPLP = (((asset.price / asset.priceBaseline) - 1) * 100);
                asset.performance = ((asset.TotalPLD / asset.TotalTraded) * 100);
                portfolioPositions.Add(asset.symbol);
                UpdateDailyStatsDB(asset);
            }
            foreach(var a in assets) {
                if (!portfolioPositions.Contains(a)){
                    refreshPrices.Add(a);
                }
            }
            var refreshed = GetAssetSnapShots(refreshPrices);
            foreach (var r in refreshed){
                ClosedDailyStatsDB(r);
            }
                
        }

        public List<string> ReadUserSelectedFile(string which)
        {
            string[] fileContent;
            switch (which.ToLower())
            {
                case "optional":
                    fileContent = File.ReadAllLines("TradeData/UserSelectedAssets/Optional_Stocks.txt");
                    break;
                default:
                case "required":
                    fileContent = File.ReadAllLines("TradeData/UserSelectedAssets/Required_Stocks.txt");
                    break;
            }
            return fileContent.ToList();
        }
        public void SetUserSelectedDB()
        {
            UserSelectedAssetsRequired = ReadUserSelectedFile("required");
            UserSelectedAssetsOptional = ReadUserSelectedFile("optional");
            _conn.Execute("DROP TABLE IF EXISTS `userselectedassets`;" +
            "CREATE TABLE `userselectedassets`(`symbol` VARCHAR(15), `assetClass` VARCHAR(25), `classification` VARCHAR(25));");
            foreach (var asset in UserSelectedAssetsRequired)
                _conn.Execute("INSERT INTO userselectedassets (symbol, assetClass, classification) VALUES (@symbol, @assetClass, @classification);",
                new { symbol = asset, assetClass = "us_equity", classification = "userpreferred" });
            foreach (var asset in UserSelectedAssetsOptional)
                _conn.Execute("INSERT INTO userselectedassets (symbol, assetClass, classification) VALUES (@symbol, @assetClass, @classification);",
                new { symbol = asset, assetClass = "us_equity", classification = "useroptional" });
        }
        public List<string> GetUserSelectedRequiredDB()
        {
            SetUserSelectedDB();
            return UserSelectedAssetsRequired;
        }
        public List<string> GetUserSelectedOptionalDB()
        {
            SetUserSelectedDB();
            return UserSelectedAssetsOptional;
        }
        public IEnumerable<string> GetUserSelectedRequired()
        {
            if (UserSelectedAssetsRequired == null){
                UserSelectedAssetsRequired = GetUserSelectedRequiredDB();
            }
            return UserSelectedAssetsRequired;
        }
        public IEnumerable<string> GetUserSelectedOptional()
        {
            if (UserSelectedAssetsOptional == null){
                UserSelectedAssetsOptional = GetUserSelectedOptionalDB();
            }
            return UserSelectedAssetsOptional;
        }
  
        public void CreateTradedAssetTable()
        {
            _conn.Execute("DROP TABLE IF EXISTS `tradedassets`; " +
                "CREATE TABLE `tradedassets`( " +
                    "`assetID` VARCHAR(100) PRIMARY KEY, " +
                    "`symbol` VARCHAR(10), " +
                    "`exchange` VARCHAR(25), " +
                    "`assetClass` VARCHAR(25), " +
                    "`shortable` TINYINT, " +
                    "`qty` FLOAT(10), " +
                    "`side` VARCHAR(25), " +
                    "`marketValue` FLOAT(10), " +
                    "`costBasis` FLOAT(10), " +
                    "`plDollarsTotal` FLOAT(10), " +
                    "`plPercentTotal` FLOAT(10), " +
                    "`plDollarsToday` FLOAT(10), " +
                    "`plPercentToday` FLOAT(10), " +
                    "`price` FLOAT(10), " +
                    "`lastPrice` FLOAT(10), " +
                    "`changeToday` FLOAT(10), " +
                    "`TotalTraded` FLOAT(10), " +
                    "`NumberTrades` FLOAT(10), " +
                    "`NumberBuys` FLOAT(10), " +
                    "`NumberSells` FLOAT(10), " +
                    "`TotalPLP` FLOAT(10), " +
                    "`TotalPLD` FLOAT(10), " +
	                "`priceBaseline` FLOAT(10), " +
                    "`assetPLP` FLOAT(10), " +
                    "`performance` FLOAT(10), " +
                    "`created` TIMESTAMP DEFAULT CURRENT_TIMESTAMP, " +
                    "`updated` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP " +
                ");");
        }

        public IEnumerable<Asset> GetAllTradedAssetsDB(){
            return _conn.Query<Asset>("SELECT * FROM tradedassets ORDER BY ABS(marketValue) DESC;");
        }

        public List<string> GetAllTradedSymbolsDB()
        {
            return _conn.Query<string>("SELECT DISTINCT symbol from tradedassets").ToList();
        }
        
        public Asset GetTradedAssetDB(string symbol){
            return _conn.QuerySingle<Asset>($"SELECT * FROM tradedassets where symbol = '{symbol}';");
        }
  
        public void AddTradedAssetToDB(Asset asset)
        {
            asset.priceBaseline = (asset.priceBaseline == 0) ? (asset.price != 0) ? asset.price : 0 : asset.priceBaseline; 
            _conn.Execute("INSERT INTO tradedassets (assetID, symbol, exchange, assetClass, shortable, qty, side, marketValue, costBasis, " +
                "plDollarsTotal, plPercentTotal, plDollarsToday, plPercentToday, price, lastPrice, changeToday, priceBaseline, assetPLP, " +
                "TotalTraded, NumberTrades, NumberBuys, NumberSells, TotalPLP, TotalPLD, performance) " + 
                " VALUES (@assetID, @symbol, @exchange, @assetClass, @shortable, @qty, @side, @marketValue, @costBasis, @plDollarsTotal, " +
                "@plPercentTotal, @plDollarsToday, @plPercentToday, @price, @lastPrice, @changeToday, @priceBaseline, @assetPLP, @TotalTraded, " +
                "@NumberTrades, @NumberBuys, @NumberSells, @TotalPLP, @TotalPLD, @performance) " + 
                " ON DUPLICATE KEY UPDATE assetID = assetID;",
            new
            {
                assetID = asset.assetID, symbol = asset.symbol, exchange = asset.exchange, assetClass = asset.assetClass, 
                shortable = asset.shortable, qty = asset.qty, side = asset.side, marketValue = asset.marketValue, costBasis = asset.costBasis, 
                plDollarsTotal = asset.plDollarsTotal, plPercentTotal = asset.plPercentTotal, plDollarsToday = asset.plDollarsToday, 
                plPercentToday = asset.plPercentToday, price = asset.price, lastPrice = asset.lastPrice, changeToday = asset.changeToday, 
                priceBaseline = asset.priceBaseline, assetPLP = asset.assetPLP, TotalTraded = asset.TotalTraded, 
                NumberTrades = asset.NumberTrades, NumberBuys = asset.NumberBuys, NumberSells = asset.NumberSells, 
                TotalPLP = asset.TotalPLP, TotalPLD = asset.TotalPLD, performance = asset.performance 
            });
        }
        // maybe do common stts update here. to update stock/asset plp and perf - we can do it all the time whenever we want to update cool. 
  
        public void UpdateDailyStatsDB(Asset asset)
        {
            _conn.Execute("UPDATE tradedassets SET qty = @qty, side = @side, marketValue = @marketValue, costBasis = @costBasis, " +
                "plDollarsTotal = @plDollarsTotal, plPercentTotal = @plPercentTotal, plDollarsToday = @plDollarsToday, " +
                "plPercentToday = @plPercentToday, price = @price, lastPrice = @lastPrice, changeToday = @changeToday, " +
                "assetPLP = @assetPLP WHERE assetID = @assetID", 
                new { qty = asset.qty, side = asset.side, marketValue = asset.marketValue, costBasis = asset.costBasis, 
                    plDollarsTotal = asset.plDollarsTotal, plPercentTotal = asset.plPercentTotal, plDollarsToday = asset.plDollarsToday, 
                    plPercentToday = asset.plPercentToday, price = asset.price, lastPrice = asset.lastPrice, changeToday = asset.changeToday, 
                    assetPLP = asset.assetPLP, assetID = asset.assetID });
        }
  
          public void ClosedDailyStatsDB(Asset asset)
        {
            _conn.Execute("UPDATE tradedassets SET qty = @qty, side = @side, marketValue = @marketValue, costBasis = @costBasis, " +
                "price = @price, lastPrice = @lastPrice, changeToday = @changeToday, performance = @performance, assetPLP = @assetPLP, " + 
                "priceBaseline = @priceBaseline WHERE symbol = @symbol", 
                new { qty = 0, side = "closed", marketValue = 0, costBasis = 0, price = asset.price, 
                    lastPrice = asset.lastPrice, changeToday = asset.changeToday, performance = asset.performance,  
                    assetPLP = asset.assetPLP, priceBaseline = asset.priceBaseline, symbol = asset.symbol });
        }
  
        public void UpdateRunningStatsDB(Asset asset)
        {

            _conn.Execute("UPDATE tradedassets SET TotalTraded = @TotalTraded, NumberTrades = @NumberTrades, NumberBuys = @NumberBuys, " +
                "NumberSells = @NumberSells, TotalPLP = @TotalPLP, TotalPLD = @TotalPLD " +
                "WHERE symbol = @symbol", 
                new { TotalTraded = asset.TotalTraded, NumberTrades = asset.NumberTrades, NumberBuys = asset.NumberBuys, 
                    NumberSells = asset.NumberSells, TotalPLP = asset.TotalPLP, TotalPLD = asset.TotalPLD, symbol = asset.symbol });
        }

        public void UpdateAllAssetStatsDB(Asset asset)
        {
            _conn.Execute("UPDATE tradedassets SET qty = @qty, side = @side, marketValue = @marketValue, costBasis = @costBasis, " +
                "plDollarsTotal = @plDollarsTotal, plPercentTotal = @plPercentTotal, plDollarsToday = @plDollarsToday, " +
                "plPercentToday = @plPercentToday, price = @price, lastPrice = @lastPrice, changeToday = @changeToday, " +
                "assetPLP = @assetPLP, TotalTraded = @TotalTraded, NumberTrades = @NumberTrades, NumberBuys = @NumberBuys, " +
                "NumberSells = @NumberSells, TotalPLP = @TotalPLP, TotalPLD = @TotalPLD, performance = @performance " +
                "WHERE assetID = @assetID",
                new
                {
                    qty = asset.qty,
                    side = asset.side,
                    marketValue = asset.marketValue,
                    costBasis = asset.costBasis,
                    plDollarsTotal = asset.plDollarsTotal,
                    plPercentTotal = asset.plPercentTotal,
                    plDollarsToday = asset.plDollarsToday,
                    plPercentToday = asset.plPercentToday,
                    price = asset.price,
                    lastPrice = asset.lastPrice,
                    changeToday = asset.changeToday,
                    assetPLP = asset.assetPLP,
                    TotalTraded = asset.TotalTraded,
                    NumberTrades = asset.NumberTrades,
                    NumberBuys = asset.NumberBuys,
                    NumberSells = asset.NumberSells,
                    TotalPLP = asset.TotalPLP,
                    TotalPLD = asset.TotalPLD,
                    performance = asset.performance,
                    assetID = asset.assetID
                });
        }
    }
}
