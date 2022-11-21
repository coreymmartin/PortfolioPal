using System;
using Dapper;
using System.Data;
using System.Net.Http;
using System.Linq;
using Newtonsoft.Json.Linq;
//using System.Threading;
using System.IO;
using System.Collections.Generic;
using PortfolioPal.Models;
using System.Threading.Tasks;
//using Microsoft.Extensions.Configuration;
//using MySql.Data.MySqlClient;

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
        public List<string> UserSelectedAssetsTrade {get; set;}
        public List<string> UserSelectedAssetsTrade_Stocks {get; set;}
        public List<string> UserSelectedAssetsTrade_Coins {get; set;}
        public List<string> UserSelectedAssetsDividend {get; set;}
        public List<string> AllOptionalAssets {get; set;}
        public List<string> DividendAssets {get; set;}
        public List<Asset> AllTradedAssets {get; set;}
        public List<string> AllTradedAssetsSymbols {get; set;}

        public AssetRepo (IDbConnection conn)
        {
            _conn = conn;
            _clientBroker = new HttpClient();
            _clientBroker.DefaultRequestHeaders.Add("APCA-API-KEY-ID", APCA_API_KEY);
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
                    a.asset_id = JObject.Parse(assets[i].ToString()).GetValue("id").ToString();
                    a.exchange = JObject.Parse(assets[i].ToString()).GetValue("exchange").ToString();
                    a.asset_class = JObject.Parse(assets[i].ToString()).GetValue("class").ToString();
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
            Asset asset = new Asset();
            asset.symbol = JObject.Parse(assetResponse).GetValue("symbol").ToString();
            asset.asset_id = JObject.Parse(assetResponse).GetValue("id").ToString();
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
                temp.current_price = Convert.ToDouble(JObject.Parse(symsMinBar.ToString()).GetValue("c"));
                temp.lastPrice = Convert.ToDouble(JObject.Parse(symsLClose.ToString()).GetValue("c"));
                temp.changeToday = temp.current_price - temp.lastPrice;
                snaps.Add(temp);
            }
            return snaps;
        }

        public List<ChartDataPoint> GetAssetPriceHistory(string symbol, int limit = 25, string start = "none", string timeframe = "15Min")
        {
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
                var asset = new Asset();
                asset.asset_id        = JObject.Parse(portfolio[i].ToString()).GetValue("asset_id").ToString();
                asset.symbol         = JObject.Parse(portfolio[i].ToString()).GetValue("symbol").ToString();
                asset.exchange       = JObject.Parse(portfolio[i].ToString()).GetValue("exchange").ToString();
                asset.asset_class     = JObject.Parse(portfolio[i].ToString()).GetValue("asset_class").ToString();
                asset.qty            = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("qty").ToString());
                asset.side           = JObject.Parse(portfolio[i].ToString()).GetValue("side").ToString();
                asset.market_value    = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("market_value").ToString());
                asset.avg_entry_price      = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("cost_basis").ToString());
                asset.pl_dollars_total = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("unrealized_pl").ToString());
                asset.port_pl_total = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("unrealized_plpc").ToString()) * 100;
                asset.pl_dollars_today = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("unrealized_intraday_pl").ToString());
                asset.port_pl_running = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("unrealized_intraday_plpc").ToString()) * 100;
                asset.current_price          = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("current_price").ToString());
                asset.lastPrice      = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("lastday_price").ToString());
                asset.changeToday    = double.Parse(JObject.Parse(portfolio[i].ToString()).GetValue("change_today").ToString());
                asset.start_price_total = (asset.start_price_total == 0) ? (asset.current_price != 0) ? asset.current_price : 0 : asset.start_price_total;
                asset.asset_pl_total = (((asset.current_price / asset.start_price_total) - 1) * 100);
                asset.port_perf_per_total = ((asset.pl_dollars_total / asset.amount_traded_total) * 100);
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
                case "dividend":
                    fileContent = File.ReadAllLines("TradeData/UserSelectedAssets/Dividend_Assets.txt");
                    break;
                case "coins":
                    fileContent = File.ReadAllLines("TradeData/UserSelectedAssets/Trade_Assets_Coins.txt");
                    break;
                default:
                case "stocks":
                    fileContent = File.ReadAllLines("TradeData/UserSelectedAssets/Trade_Assets_Stocks.txt");
                    break;
            }
            return fileContent.ToList();
        }
        public void SetUserSelectedDB()
        {
            UserSelectedAssetsTrade_Stocks = ReadUserSelectedFile("stocks");
            UserSelectedAssetsTrade_Coins = ReadUserSelectedFile("coins");
            UserSelectedAssetsDividend = ReadUserSelectedFile("dividend");
            UserSelectedAssetsTrade = new List<string>();
            _conn.Execute("DROP TABLE IF EXISTS `userselectedassets`;" +
            "CREATE TABLE `userselectedassets`(`symbol` VARCHAR(15), `asset_class` VARCHAR(25), `classification` VARCHAR(25));");
            foreach (var s in UserSelectedAssetsTrade_Stocks){
                if (!UserSelectedAssetsTrade.Contains(s)) {
                    UserSelectedAssetsTrade.Add(s);
                }
                _conn.Execute("INSERT INTO userselectedassets (symbol, asset_class, classification) VALUES (@symbol, @asset_class, @classification);",
                new { symbol = s, assetClass = "us_equity", classification = "usertrade" });
            }
            foreach (var c in UserSelectedAssetsTrade_Coins){
                if (!UserSelectedAssetsTrade.Contains(c)) {
                    UserSelectedAssetsTrade.Add(c);
                }
                _conn.Execute("INSERT INTO userselectedassets (symbol, asset_class, classification) VALUES (@symbol, @asset_class, @classification);",
                new { symbol = c, assetClass = "crypto", classification = "usertrade" });
            }
            foreach (var asset in UserSelectedAssetsDividend)
                _conn.Execute("INSERT INTO userselectedassets (symbol, asset_class, classification) VALUES (@symbol, @asset_class, @classification);",
                new { symbol = asset, assetClass = "us_equity", classification = "userdividend" });
        }
        public List<string> GetUserSelectedTradeDB()
        {
            SetUserSelectedDB();
            return UserSelectedAssetsTrade;
        }
        public List<string> GetUserSelectedDividendDB()
        {
            SetUserSelectedDB();
            return UserSelectedAssetsDividend;
        }
        public IEnumerable<string> GetUserSelectedTrade()
        {
            if (UserSelectedAssetsTrade == null){
                UserSelectedAssetsTrade = GetUserSelectedTradeDB();
            }
            return UserSelectedAssetsTrade;
        }
        public IEnumerable<string> GetUserSelectedDividend()
        {
            if (UserSelectedAssetsDividend == null){
                UserSelectedAssetsDividend = GetUserSelectedDividendDB();
            }
            return UserSelectedAssetsDividend;
        }
        // you must fix all of this because you changed the number and names of the values! have fun!!!
        public void CreateTradedAssetTable(){
            _conn.Execute("DROP TABLE IF EXISTS `tradedassets`; " +
                "CREATE TABLE `tradedassets`(" +
                    "`asset_id` VARCHAR(50) PRIMARY KEY," +
                    "`symbol` VARCHAR(10)," +
                    "`asset_class` VARCHAR(15)," +
                    "`classification` VARCHAR(15)," +
                    "`tradable` TINYINT," +
                    "`shortable` TINYINT," +
                    "`side` VARCHAR(10)," +
                    "`qty` FLOAT(10)," +
                    "`num_trades_current` FLOAT(10)," +
                    "`num_trades_total` FLOAT(10)," +
                    "`num_buys_current` FLOAT(10)," +
                    "`num_sells_current` FLOAT(10)," +
                    "`num_buys_total` FLOAT(10)," +
                    "`num_sells_total` FLOAT(10)," +
                    "`current_price` FLOAT(10)," +
                    "`market_value` FLOAT(10)," +
                    "`avg_entry_price` FLOAT(10)," +
                    "`hsl_limit_long` FLOAT(10)," +
                    "`tsl_limit_long` FLOAT(10)," +
                    "`tsl_enable_long` FLOAT(10)," +
                    "`hsl_limit_short` FLOAT(10)," +
                    "`tsl_limit_short` FLOAT(10)," +
                    "`tsl_enable_short` FLOAT(10)," +
                    "`hsl_trigger` FLOAT(10)," +
                    "`tsl_trigger` FLOAT(10)," +
                    "`allowance` FLOAT(10)," +
                    "`max_active_price` FLOAT(10)," +
                    "`min_active_price` FLOAT(10)," +
                    "`tradestamp` FLOAT(25)," +
                    "`start_price_current` FLOAT(10)," +
                    "`start_price_total` FLOAT(10)," +
                    "`asset_max_price_current` FLOAT(10)," +
                    "`asset_max_price_total` FLOAT(10)," +
                    "`asset_min_price_current` FLOAT(10)," +
                    "`asset_min_price_total` FLOAT(10)," +
                    "`asset_pl_current` FLOAT(10)," +
                    "`asset_pl_total` FLOAT(10)," +
                    "`port_pl_now` FLOAT(10)," +
                    "`port_pl_running` FLOAT(10)," +
                    "`port_pl_running` FLOAT(10)," +
                    "`port_pl_total` FLOAT(10)," +
                    "`port_perf_per_current` FLOAT(10)," +
                    "`port_perf_per_total` FLOAT(10)," +
                    "`port_max_pl_current` FLOAT(10)," +
                    "`port_max_pl_total` FLOAT(10)," +
                    "`port_min_pl_current` FLOAT(10)," +
                    "`port_min_pl_total` FLOAT(10)," +
                    "`pl_dollars_current` FLOAT(10)," +
                    "`pl_dollars_total` FLOAT(10)," +
                    "`amount_traded_current` FLOAT(10)," +
                    "`amount_traded_total` FLOAT(10)," +
                    "`roi_current` FLOAT(10)," +
                    "`roi_total` FLOAT(10)," +
                    "`hsl_signal` VARCHAR(10)," +
                    "`tsl_signal` VARCHAR(10)," +
                    "`day_signal` VARCHAR(10)," +
                    "`hour_signal` VARCHAR(10)," +
                    "`15Min_signal` VARCHAR(10)," +
                    "`master_signal` VARCHAR(10)," +
                    "`strategy_champ` VARCHAR(15)," +
                    "`strategy_day` VARCHAR(15)," +
                    "`strategy_hour` VARCHAR(15)," +
                    "`strategy_15Min` VARCHAR(15)," +
                    "`diversity_signal` VARCHAR(5)," +
                    "`betty_day_period_long` FLOAT(5)," +
                    "`betty_day_period_short` FLOAT(5)," +
                    "`betty_hour_period_long` FLOAT(5)," +
                    "`betty_hour_period_short` FLOAT(5)," +
                    "`betty_15Min_period_long` FLOAT(5)," +
                    "`betty_15Min_period_short` FLOAT(5)," +
                    "`mercy_day_slow` FLOAT(5)," +
                    "`mercy_day_fast` FLOAT(5)," +
                    "`mercy_day_smooth` FLOAT(5)," +
                    "`mercy_hour_slow` FLOAT(5)," +
                    "`mercy_hour_fast` FLOAT(5)," +
                    "`mercy_hour_smooth` FLOAT(5)," +
                    "`mercy_15Min_slow` FLOAT(5)," +
                    "`mercy_15Min_fast` FLOAT(5)," +
                    "`mercy_15Min_smooth` FLOAT(5)," +
                    "`polly_day_period` FLOAT(5)," +
                    "`polly_day_future` FLOAT(5)," +
                    "`polly_hour_period` FLOAT(5)," +
                    "`polly_hour_future` FLOAT(5)," +
                    "`polly_15Min_period` FLOAT(5)," +
                    "`polly_15Min_future` FLOAT(5)," +
                    "`betty_opt_pl_day` FLOAT(10)," +
                    "`betty_opt_pl_hour` FLOAT(10)," +
                    "`betty_opt_pl_15Min` FLOAT(10)," +
                    "`mercy_opt_pl_day` FLOAT(10)," +
                    "`mercy_opt_pl_hour` FLOAT(10)," +
                    "`mercy_opt_pl_15Min` FLOAT(10)," +
                    "`polly_opt_pl_day` FLOAT(10)," +
                    "`polly_opt_pl_hour` FLOAT(10)," +
                    "`polly_opt_pl_15Min` FLOAT(10)," +
                    "`tsl_long_opt_pl` FLOAT(10)," +
                    "`tsl_short_opt_pl` FLOAT(10)," +
                    "`betty_optimized` FLOAT(15)," +
                    "`betty_optcomplete_day` FLOAT(5)," +
                    "`betty_optcomplete_hour` FLOAT(5)," +
                    "`betty_optcomplete_15Min` FLOAT(5)," +
                    "`mercy_optimized` FLOAT(15)," +
                    "`mercy_optcomplete_day` FLOAT(5)," +
                    "`mercy_optcomplete_hour` FLOAT(5)," +
                    "`mercy_optcomplete_15Min` FLOAT(5)," +
                    "`polly_optimized` FLOAT(15)," +
                    "`polly_optcomplete_day` FLOAT(5)," +
                    "`polly_optcomplete_hour` FLOAT(5)," +
                    "`polly_optcomplete_15Min` FLOAT(5)," +
                    "`tsl_long_optcomplete` FLOAT(5)," +
                    "`tsl_short_optcomplete` FLOAT(5)," +
                    "`tsl_optimized` FLOAT(15)," +
                    "`min_order_qty` FLOAT(10)," +
                    "`inc_order_qty` FLOAT(10)," +
                    "`status_check` FLOAT(15)," +
                    "`updated` VARCHAR(50)," +
                    "`strategy_timestamp` VARCHAR(50)," +
                    "`created` VARCHAR(50));"
                );
        }

        public IEnumerable<Asset> GetAllTradedAssetsDB(){
            return _conn.Query<Asset>("SELECT * FROM tradedassets ORDER BY symbol ASC;");
        }

        public List<string> GetAllTradedSymbolsDB()
        {
            return _conn.Query<string>("SELECT DISTINCT symbol from tradedassets").ToList();
        }
        
        public Asset GetTradedAssetDB(string symbol){
            return _conn.QuerySingle<Asset>($"SELECT * FROM tradedassets where symbol = '{symbol}';");
        }
        // need to update this to match current traded assets DB - you added all asset parameters. update everywhere!!! please and thank you
        public void AddTradedAssetToDB(Asset asset)
        {
            asset.start_price_total = (asset.start_price_total == 0) ? (asset.current_price != 0) ? asset.current_price : 0 : asset.start_price_total; 
            _conn.Execute("INSERT INTO tradedassets (asset_id, symbol, exchange, asset_class, classification, tradable, shortable, side, qty, num_trades_current, " + 
                    "num_trades_total, num_buys_current, num_sells_current, num_buys_total, num_sells_total, current_price, market_value, avg_entry_price, " +
                    "hsl_limit_long, tsl_limit_long, tsl_enable_long, hsl_limit_short, tsl_limit_short, tsl_enable_short, hsl_trigger, tsl_trigger, allowance, " +
                    "max_active_price, min_active_price, tradestamp, start_price_current, start_price_total, asset_max_price_current, asset_max_price_total, " +
                    "asset_min_price_current, asset_min_price_total, asset_pl_current, asset_pl_total, port_pl_now, port_pl_running, port_pl_current, " +
                    "port_pl_total, port_perf_per_current, port_perf_per_total, port_max_pl_current, port_max_pl_total, port_min_pl_current, port_min_pl_total, " +
                    "pl_dollars_current, pl_dollars_total, amount_traded_current, amount_traded_total, roi_current, roi_total, hsl_signal, tsl_signal, " +
                    "day_signal, hour_signal, 15Min_signal, master_signal, strategy_champ, strategy_day, strategy_hour, strategy_15Min, diversity_signal, " +
                    "betty_day_period_long, betty_day_period_short, betty_hour_period_long, betty_hour_period_short, betty_15Min_period_long, " +
                    "betty_15Min_period_short, mercy_day_slow, mercy_day_fast, mercy_day_smooth, mercy_hour_slow, mercy_hour_fast, mercy_hour_smooth, " +
                    "mercy_15Min_slow, mercy_15Min_fast, mercy_15Min_smooth, polly_day_period, polly_day_future, polly_hour_period, polly_hour_future, " +
                    "polly_15Min_period, polly_15Min_future, betty_opt_pl_day, betty_opt_pl_hour, betty_opt_pl_15Min, mercy_opt_pl_day, mercy_opt_pl_hour, " +
                    "mercy_opt_pl_15Min, polly_opt_pl_day, polly_opt_pl_hour, polly_opt_pl_15Min, tsl_long_opt_pl, tsl_short_opt_pl, betty_optimized, " +
                    "betty_optcomplete_day, betty_optcomplete_hour, betty_optcomplete_15Min, mercy_optimized, mercy_optcomplete_day, mercy_optcomplete_hour, " +
                    "mercy_optcomplete_15Min, polly_optimized, polly_optcomplete_day, polly_optcomplete_hour, polly_optcomplete_15Min, tsl_long_optcomplete, " +
                    "tsl_short_optcomplete, tsl_optimized, min_order_qty, inc_order_qty, status_check, updated, strategy_timestamp, created                ) " + 
                " VALUES (@asset_id, @symbol, @exchange, @asset_class, @classification, @tradable, @shortable, @side, @qty, @num_trades_current, " + 
                    "@num_trades_total, @num_buys_current, @num_sells_current, @num_buys_total, @num_sells_total, @current_price, @market_value, @avg_entry_price, " +
                    "@hsl_limit_long, @tsl_limit_long, @tsl_enable_long, @hsl_limit_short, @tsl_limit_short, @tsl_enable_short, @hsl_trigger, @tsl_trigger, @allowance, " +
                    "@max_active_price, @min_active_price, @tradestamp, @start_price_current, @start_price_total, @asset_max_price_current, @asset_max_price_total, " +
                    "@asset_min_price_current, @asset_min_price_total, @asset_pl_current, @asset_pl_total, @port_pl_now, @port_pl_running, @port_pl_current, " +
                    "@port_pl_total, @port_perf_per_current, @port_perf_per_total, @port_max_pl_current, @port_max_pl_total, @port_min_pl_current, @port_min_pl_total, " +
                    "@pl_dollars_current, @pl_dollars_total, @amount_traded_current, @amount_traded_total, @roi_current, @roi_total, @hsl_signal, @tsl_signal, " +
                    "@signal_day, @signal_hour, @signal_15Min, @master_signal, @strategy_champ, @strategy_day, @strategy_hour, @strategy_15Min, @diversity_signal, " +
                    "@betty_day_period_long, @betty_day_period_short, @betty_hour_period_long, @betty_hour_period_short, @betty_15Min_period_long, " +
                    "@betty_15Min_period_short, @mercy_day_slow, @mercy_day_fast, @mercy_day_smooth, @mercy_hour_slow, @mercy_hour_fast, @mercy_hour_smooth, " +
                    "@mercy_15Min_slow, @mercy_15Min_fast, @mercy_15Min_smooth, @polly_day_period, @polly_day_future, @polly_hour_period, @polly_hour_future, " +
                    "@polly_15Min_period, @polly_15Min_future, @betty_opt_pl_day, @betty_opt_pl_hour, @betty_opt_pl_15Min, @mercy_opt_pl_day, @mercy_opt_pl_hour, " +
                    "@mercy_opt_pl_15Min, @polly_opt_pl_day, @polly_opt_pl_hour, @polly_opt_pl_15Min, @tsl_long_opt_pl, @tsl_short_opt_pl, @betty_optimized, " +
                    "@betty_optcomplete_day, @betty_optcomplete_hour, @betty_optcomplete_15Min, @mercy_optimized, @mercy_optcomplete_day, @mercy_optcomplete_hour, " +
                    "@mercy_optcomplete_15Min, @polly_optimized, @polly_optcomplete_day, @polly_optcomplete_hour, @polly_optcomplete_15Min, @tsl_long_optcomplete, " +
                    "@tsl_short_optcomplete, @tsl_optimized, @min_order_qty, @inc_order_qty, @status_check, @updated, @strategy_timestamp, @created                ) " + 
                " ON DUPLICATE KEY UPDATE asset_id = asset_id;",
            new
            {
                asset_id= asset.asset_id, symbol= asset.symbol, exchange= asset.exchange, asset_class= asset.asset_class, classification= asset.classification, tradable= asset.tradable, shortable= asset.shortable, side= asset.side, qty= asset.qty, num_trades_current= asset.num_trades_current,
                num_trades_total= asset.num_trades_total, num_buys_current= asset.num_buys_current, num_sells_current= asset.num_sells_current, num_buys_total= asset.num_buys_total, num_sells_total= asset.num_sells_total, current_price= asset.current_price, market_value= asset.market_value, avg_entry_price= asset.avg_entry_price,
                hsl_limit_long= asset.hsl_limit_long, tsl_limit_long= asset.tsl_limit_long, tsl_enable_long= asset.tsl_enable_long, hsl_limit_short= asset.hsl_limit_short, tsl_limit_short= asset.tsl_limit_short, tsl_enable_short= asset.tsl_enable_short, hsl_trigger= asset.hsl_trigger, tsl_trigger= asset.tsl_trigger, allowance= asset.allowance,
                max_active_price= asset.max_active_price, min_active_price= asset.min_active_price, tradestamp= asset.tradestamp, start_price_current= asset.start_price_current, start_price_total= asset.start_price_total, asset_max_price_current= asset.asset_max_price_current, asset_max_price_total= asset.asset_max_price_total,
                asset_min_price_current= asset.asset_min_price_current, asset_min_price_total= asset.asset_min_price_total, asset_pl_current= asset.asset_pl_current, asset_pl_total= asset.asset_pl_total, port_pl_now= asset.port_pl_now, port_pl_running= asset.port_pl_running, port_pl_current= asset.port_pl_current,
                port_pl_total= asset.port_pl_total, port_perf_per_current= asset.port_perf_per_current, port_perf_per_total= asset.port_perf_per_total, port_max_pl_current= asset.port_max_pl_current, port_max_pl_total= asset.port_max_pl_total, port_min_pl_current= asset.port_min_pl_current, port_min_pl_total= asset.port_min_pl_total,
                pl_dollars_current= asset.pl_dollars_current, pl_dollars_total= asset.pl_dollars_total, amount_traded_current= asset.amount_traded_current, amount_traded_total= asset.amount_traded_total, roi_current= asset.roi_current, roi_total= asset.roi_total, hsl_signal= asset.hsl_signal, tsl_signal= asset.tsl_signal,
                day_signal= asset.signal_day, hour_signal= asset.signal_hour, signal_15Min= asset.signal_15Min, master_signal= asset.master_signal, strategy_champ= asset.strategy_champ, strategy_day= asset.strategy_day, strategy_hour= asset.strategy_hour, strategy_15Min= asset.strategy_15Min, diversity_signal= asset.diversity_signal,
                betty_day_period_long= asset.betty_day_period_long, betty_day_period_short= asset.betty_day_period_short, betty_hour_period_long= asset.betty_hour_period_long, betty_hour_period_short= asset.betty_hour_period_short, betty_15Min_period_long= asset.betty_15Min_period_long,
                betty_15Min_period_short= asset.betty_15Min_period_short, mercy_day_slow= asset.mercy_day_slow, mercy_day_fast= asset.mercy_day_fast, mercy_day_smooth= asset.mercy_day_smooth, mercy_hour_slow= asset.mercy_hour_slow, mercy_hour_fast= asset.mercy_hour_fast, mercy_hour_smooth= asset.mercy_hour_smooth,
                mercy_15Min_slow= asset.mercy_15Min_slow, mercy_15Min_fast= asset.mercy_15Min_fast, mercy_15Min_smooth= asset.mercy_15Min_smooth, polly_day_period= asset.polly_day_period, polly_day_future= asset.polly_day_future, polly_hour_period= asset.polly_hour_period, polly_hour_future= asset.polly_hour_future,
                polly_15Min_period= asset.polly_15Min_period, polly_15Min_future= asset.polly_15Min_future, betty_opt_pl_day= asset.betty_opt_pl_day, betty_opt_pl_hour= asset.betty_opt_pl_hour, betty_opt_pl_15Min= asset.betty_opt_pl_15Min, mercy_opt_pl_day= asset.mercy_opt_pl_day, mercy_opt_pl_hour= asset.mercy_opt_pl_hour,
                mercy_opt_pl_15Min= asset.mercy_opt_pl_15Min, polly_opt_pl_day= asset.polly_opt_pl_day, polly_opt_pl_hour= asset.polly_opt_pl_hour, polly_opt_pl_15Min= asset.polly_opt_pl_15Min, tsl_long_opt_pl= asset.tsl_long_opt_pl, tsl_short_opt_pl= asset.tsl_short_opt_pl, betty_optimized= asset.betty_optimized,
                betty_optcomplete_day= asset.betty_optcomplete_day, betty_optcomplete_hour= asset.betty_optcomplete_hour, betty_optcomplete_15Min= asset.betty_optcomplete_15Min, mercy_optimized= asset.mercy_optimized, mercy_optcomplete_day= asset.mercy_optcomplete_day, mercy_optcomplete_hour= asset.mercy_optcomplete_hour,
                mercy_optcomplete_15Min= asset.mercy_optcomplete_15Min, polly_optimized= asset.polly_optimized, polly_optcomplete_day= asset.polly_optcomplete_day, polly_optcomplete_hour= asset.polly_optcomplete_hour, polly_optcomplete_15Min= asset.polly_optcomplete_15Min, tsl_long_optcomplete= asset.tsl_long_optcomplete,
                tsl_short_optcomplete= asset.tsl_short_optcomplete, tsl_optimized= asset.tsl_optimized, min_order_qty= asset.min_order_qty, inc_order_qty= asset.inc_order_qty, status_check= asset.status_check, updated= asset.updated, strategy_timestamp= asset.strategy_timestamp, created= asset.created
            });
        }
  
        public void UpdateDailyStatsDB(Asset asset)
        {
            _conn.Execute("UPDATE tradedassets SET qty = @qty, side = @side, market_value = @market_value, avg_entry_price = @avg_entry_price, " +
                "pl_dollars_total = @pl_dollars_total, port_pl_total = @port_pl_total, pl_dollars_today = @pl_dollars_today, " +
                "port_pl_running = @port_pl_running, current_price = @current_price, lastPrice = @lastPrice, changeToday = @changeToday, " +
                "asset_pl_total = @asset_pl_total WHERE asset_id = @asset_id", 
                new { qty = asset.qty, side = asset.side, marketValue = asset.market_value, costBasis = asset.avg_entry_price, 
                    plDollarsTotal = asset.pl_dollars_total, plPercentTotal = asset.port_pl_total, plDollarsToday = asset.pl_dollars_today, 
                    plPercentToday = asset.port_pl_running, price = asset.current_price, lastPrice = asset.lastPrice, changeToday = asset.changeToday, 
                    asset_pl_total = asset.asset_pl_total, assetID = asset.asset_id });
        }
  
          public void ClosedDailyStatsDB(Asset asset)
        {
            _conn.Execute("UPDATE tradedassets SET qty = @qty, side = @side, market_value = @market_value, avg_entry_price = @avg_entry_price, " +
                "current_price = @current_price, lastPrice = @lastPrice, changeToday = @changeToday, port_perf_per_total = @port_perf_per_total, asset_pl_total = @asset_pl_total, " + 
                "start_price_total = @start_price_total WHERE symbol = @symbol", 
                new { qty = 0, side = "closed", marketValue = 0, costBasis = 0, price = asset.current_price, 
                    lastPrice = asset.lastPrice, changeToday = asset.changeToday, port_perf_per_total = asset.port_perf_per_total,  
                    asset_pl_total = asset.asset_pl_total, priceBaseline = asset.start_price_total, symbol = asset.symbol });
        }
  
        public void UpdateRunningStatsDB(Asset asset)
        {

            _conn.Execute("UPDATE tradedassets SET amount_traded_total = @amount_traded_total, num_trades_total = @num_trades_total, num_buys_total = @num_buys_total, " +
                "num_sells_total = @num_sells_total, TotalPLP = @TotalPLP, TotalPLD = @TotalPLD " +
                "WHERE symbol = @symbol", 
                new { TotalTraded = asset.amount_traded_total, NumberTrades = asset.num_trades_total, NumberBuys = asset.num_buys_total, 
                    NumberSells = asset.num_sells_total, TotalPLP = asset.port_pl_total, TotalPLD = asset.pl_dollars_total, symbol = asset.symbol });
        }

        public void UpdateAllAssetStatsDB(Asset asset)
        {
            // need to fix this like paris. just copy paste format it cool cool
            _conn.Execute("UPDATE tradedassets SET qty = @qty, side = @side, market_value = @market_value, avg_entry_price = @avg_entry_price, " +
                "pl_dollars_total = @pl_dollars_total, port_pl_total = @port_pl_total, pl_dollars_today = @pl_dollars_today, " +
                "port_pl_running = @port_pl_running, current_price = @current_price, lastPrice = @lastPrice, changeToday = @changeToday, " +
                "asset_pl_total = @asset_pl_total, amount_traded_total = @amount_traded_total, num_trades_total = @num_trades_total, num_buys_total = @num_buys_total, " +
                "num_sells_total = @num_sells_total, TotalPLP = @TotalPLP, TotalPLD = @TotalPLD, port_perf_per_total = @port_perf_per_total " +
                "WHERE asset_id = @asset_id",
                new
                {
                    qty = asset.qty,
                    side = asset.side,
                    marketValue = asset.market_value,
                    costBasis = asset.avg_entry_price,
                    plDollarsTotal = asset.pl_dollars_total,
                    plPercentTotal = asset.port_pl_total,
                    plDollarsToday = asset.pl_dollars_today,
                    plPercentToday = asset.port_pl_running,
                    price = asset.current_price,
                    lastPrice = asset.lastPrice,
                    changeToday = asset.changeToday,
                    asset_pl_total = asset.asset_pl_total,
                    TotalTraded = asset.amount_traded_total,
                    NumberTrades = asset.num_trades_total,
                    NumberBuys = asset.num_buys_total,
                    NumberSells = asset.num_sells_total,
                    TotalPLP = asset.port_pl_total,
                    TotalPLD = asset.pl_dollars_total,
                    port_perf_per_total = asset.port_perf_per_total,
                    assetID = asset.asset_id
                });
        }
    }
}
