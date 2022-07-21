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



        public AssetRepo ()
        {
            _clientBroker = new HttpClient();
            _clientBroker.DefaultRequestHeaders.Add("APCA-API-KEY-ID", APCA_API_KEY);       // fix this its messy.
            _clientBroker.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", APCA_API_SECRET);

            var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
            string connString = config.GetConnectionString("portfoliopal");
            _conn = new MySqlConnection(connString);
        }
        

        public async Task<string> AwaitClientGetReponse(HttpClient client, string url)
        {
            return await client.GetStringAsync(url);   
        }

        public List<Asset> GetAllTradableAssets()
        {
            AllTradableAssets = (AllTradableAssets == null) ? new List<Asset>() : AllTradableAssets;
            AllTradableAssetSymbols = (AllTradableAssetSymbols == null) ? new List<string>() : AllTradableAssetSymbols;
            var potentialsURL = $"{APCA_API_URL}/v2/assets?status=active";
            var potentialsResponse = AwaitClientGetReponse(_clientBroker, potentialsURL);
            var assets = JArray.Parse(potentialsResponse.Result.ToString()).ToList();
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

        public Asset GetAsset()
        {
            throw new System.NotImplementedException();
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
            return _conn.Query<Asset>("SELECT * FROM tradedassets;");
        }

        public List<string> GetAllTradedSymbolsDB()
        {
            return _conn.Query<string>("SELECT DISTICT symbol from tradedassets").ToList();
        }
        
        public Asset GetTradedAssetDB(string symbol){
            return _conn.QuerySingle<Asset>($"SELECT * FROM tradedassets where symbol = {symbol};");
        }
  
        public void AddTradedAssetToDB(Asset asset)
        {
            asset.priceBaseline = (asset.priceBaseline == null || asset.priceBaseline == 0) ? (asset.price != 0) ? asset.price : 0 : asset.priceBaseline; 
            _conn.Execute("INSERT INTO tradedassets (assetID, symbol, exchange, assetClass, shortable, qty, side, marketValue, costBasis, " +
                "plDollarsTotal, plPercentTotal, plDollarsToday, plPercentToday, price, lastPrice, changeToday, priceBaseline, assetPLP, " +
                "TotalTraded, NumberTrades, NumberBuys, NumberSells, TotalPLP, TotalPLD, performance) " + 
                " VALUES (@assetID, @symbol, @exchange, @assetClass, @shortable, @qty, @side, @marketValue, @costBasis, @plDollarsTotal, " +
                "@plPercentTotal, @plDollarsToday, @plPercentToday, @price, @lastPrice, @changeToday, @priceBaseline, @assetPLP, @TotalTraded, " +
                "@NumberTrades, @NumberBuys, @NumberSells, @TotalPLP, @TotalPLD, @performance) " + 
                " ON DUPLICATE KEY UPDATE orderID = orderID;",
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
  
        public void UpdateRunningStatsDB(Asset asset)
        {

            _conn.Execute("UPDATE tradedassets SET TotalTraded = @TotalTraded, NumberTrades = @NumberTrades, NumberBuys = @NumberBuys, " +
                "NumberSells = @NumberSells, TotalPLP = @TotalPLP, TotalPLD = @TotalPLD, performance = @performance WHERE assetID = @assetID", 
                new { TotalTraded = asset.TotalTraded, NumberTrades = asset.NumberTrades, NumberBuys = asset.NumberBuys, 
                    NumberSells = asset.NumberSells, TotalPLP = asset.TotalPLP, TotalPLD = asset.TotalPLD, performance = asset.performance,  
                    assetID = asset.assetID });
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
