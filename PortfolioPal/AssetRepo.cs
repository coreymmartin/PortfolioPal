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



        public AssetRepo ()
        {
            _clientBroker = new HttpClient();
            _clientBroker.DefaultRequestHeaders.Add("APCA-API-KEY-ID", APCA_API_KEY);       // fix this its messy.
            _clientBroker.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", APCA_API_SECRET);

            var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
            string connString = config.GetConnectionString("portfoliopal");
            _conn = new MySqlConnection(connString);
            // do we need to close this connection? maybe so? because then we keep openning new ones?
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
    }
}
