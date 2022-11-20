using System;
using System.Net.Http;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using PortfolioPal.Models;
using System.Data;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;

namespace PortfolioPal
{
    public class PortfolioRepo : IPortfolioRepo
    {
        private HttpClient _clientBroker;
        private readonly IDbConnection _conn;
        private string APCA_API_KEY = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_key").ToString();
        private string APCA_API_SECRET = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_secret").ToString();
        private string APCA_API_URL = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_url").ToString();
        private string APCA_DATA_API_URL = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_data_url").ToString();

        private List<string> okPeriod = new List<string>() { "1D", "1W", "1M", "A" };
        private List<string> okTimeframe = new List<string>() { "5Min", "10Min", "15Min", "1H", "1D", "1W" };

        public PortfolioRepo (IDbConnection conn)
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

        public Portfolio GetAccount()
        {
            Portfolio p = new Portfolio();
            var accountURL = $"{APCA_API_URL}/v2/account";
            var accountResponse = _clientBroker.GetStringAsync(accountURL).Result;
            var accountInfo = JObject.Parse(accountResponse).ToString();
            p.accountNumber    = JObject.Parse(accountInfo).GetValue("account_number").ToString();
            p.status           = JObject.Parse(accountInfo).GetValue("status").ToString();
            p.currency         = JObject.Parse(accountInfo).GetValue("currency").ToString();
            p.cryptoStatus     = JObject.Parse(accountInfo).GetValue("crypto_status").ToString();
            p.buyingPower      = double.Parse(JObject.Parse(accountInfo).GetValue("buying_power").ToString());
            p.cash             = double.Parse(JObject.Parse(accountInfo).GetValue("cash").ToString());
            p.accrued_fees     = double.Parse(JObject.Parse(accountInfo).GetValue("accrued_fees").ToString());
            p.portfolioValue   = double.Parse(JObject.Parse(accountInfo).GetValue("portfolio_value").ToString());
            p.patternDayTrader = JObject.Parse(accountInfo).GetValue("pattern_day_trader").ToString();
            p.createdAt        = JObject.Parse(accountInfo).GetValue("created_at").ToString();
            p.equity           = double.Parse(JObject.Parse(accountInfo).GetValue("equity").ToString());
            p.lastEquity       = double.Parse(JObject.Parse(accountInfo).GetValue("last_equity").ToString());
            p.longMarketValue  = double.Parse(JObject.Parse(accountInfo).GetValue("long_market_value").ToString());
            p.shortMarketValue = double.Parse(JObject.Parse(accountInfo).GetValue("short_market_value").ToString());
            p.todaysPLD = p.equity - p.lastEquity;         
            p.todaysPLP = (p.todaysPLD / p.lastEquity) * 100;         
            return p;
        }
        
        public void GetAllPortfolioPositions(Portfolio p)
        {
            var positionsURL = $"{APCA_API_URL}/v2/positions";
            var positionsResponse = _clientBroker.GetStringAsync(positionsURL).Result;
            var positions = JArray.Parse(positionsResponse).ToArray();
            for (int i = 0; i < positions.Length; i++)
            {
                var asset            = new Asset();
                asset.asset_id        = JObject.Parse(positions[i].ToString()).GetValue("asset_id").ToString();
                asset.symbol         = JObject.Parse(positions[i].ToString()).GetValue("symbol").ToString();
                asset.exchange       = JObject.Parse(positions[i].ToString()).GetValue("exchange").ToString();
                asset.asset_class     = JObject.Parse(positions[i].ToString()).GetValue("asset_class").ToString();
                asset.qty            = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("qty").ToString());
                asset.side           = JObject.Parse(positions[i].ToString()).GetValue("side").ToString();
                asset.market_value    = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("market_value").ToString());
                asset.avg_entry_price      = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("cost_basis").ToString());
                asset.pl_dollars_total = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("unrealized_pl").ToString());
                asset.port_pl_total = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("unrealized_plpc").ToString()) * 100;
                asset.pl_dollars_today = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("unrealized_intraday_pl").ToString());
                asset.port_pl_current = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("unrealized_intraday_plpc").ToString()) * 100;
                asset.current_price          = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("current_price").ToString());
                asset.lastPrice      = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("lastday_price").ToString());
                asset.changeToday    = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("change_today").ToString());
                p.portfolioPositions.Add(asset);
                if (asset.asset_class == "us_equity")
                    p.stockPositions.Add(asset);
                else if (asset.asset_class == "crypto")
                    p.cryptoPositions.Add(asset);
            }
        }
        
        public void UpdatePortfolioDiversity(Portfolio p)
        {
            double longHolding        = 0;
            double shortHolding       = 0;
            double cryptoHolding      = 0;
            double dividendHolding    = 0;
            p.stockHoldingLimit    = p.equity * Portfolio.StockDiversity; 
            p.cryptoHoldingLimit   = p.equity * Portfolio.CryptoDiversity; 
            p.dividendHoldingLimit = p.equity * Portfolio.DividendDiversity;
            foreach (var asset in p.stockPositions)
                if (asset.side == "long")
                    longHolding += asset.market_value;
                else
                    shortHolding += asset.market_value;
            foreach (var asset in p.dividendPositions)
                dividendHolding += asset.market_value;
            foreach (var asset in p.cryptoPositions)
                cryptoHolding += asset.market_value;
            p.longHoldingActual     = longHolding;
            p.shortHoldingActual    = shortHolding;
            p.stockHoldingActual    = longHolding + Math.Abs(shortHolding);
            p.dividendHoldingActual = dividendHolding;
            p.cryptoHoldingActual   = cryptoHolding;
        }

        public List<PieChartDataPoint> GetDiversityChartValues(Portfolio p){
            List<PieChartDataPoint> pieData = new List<PieChartDataPoint>();
            pieData.Add(new PieChartDataPoint("Stocks", p.stockHoldingActual));
            pieData.Add(new PieChartDataPoint("Dividends",10));
            pieData.Add(new PieChartDataPoint("Crypto", p.cryptoHoldingActual));
            return pieData;
        }

        public List<ChartDataPoint> GetPortfolioHistory(string period = "1D", string timeframe = "15Min")
        {
            List<ChartDataPoint> portHistoryData = new List<ChartDataPoint>();
            if (!okPeriod.Contains(period))
                period = "1D";
            if (!okTimeframe.Contains(timeframe))
                timeframe = "15Min";
            var historyURL = $"{APCA_API_URL}/v2/account/portfolio/history?period={period}&timeframe={timeframe}";
            var historyResponse = _clientBroker.GetStringAsync(historyURL).Result;

            var historyTimestamp = JObject.Parse(historyResponse.ToString()).GetValue("timestamp").ToList();
            var historyEquity = JObject.Parse(historyResponse.ToString()).GetValue("equity").ToList();
            var maxEntries = Math.Max(historyTimestamp.Count(), historyEquity.Count());
            double equity;
            DateTime timestamp;
            double tempEquity = 0;
            for (var i = 0; i < maxEntries; i++){
                if (historyTimestamp[i] != null && historyEquity[i] != null){
                    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    timestamp = dateTime.AddSeconds(Convert.ToDouble(historyTimestamp[i])).ToLocalTime();

                    equity = (Double.TryParse(historyEquity[i].ToString(), out double e)) ? e : tempEquity;
                    if (equity > 0){
                        portHistoryData.Add(new ChartDataPoint(timestamp.ToString(), equity));
                        tempEquity = e;
                    }
                }
            }
            return portHistoryData;
        }

        public void CheckMarketOpen(Portfolio p)
        {
            new Clock();
            p.marketOpen = Clock.marketOpen;
        }

        public void UpdatePortfolioTradeAssetsDB(){

        }

        public MarketPerf GetMarketPerformance()
        {
            MarketPerf perf = new MarketPerf();
            var assetURL = $"{APCA_DATA_API_URL}/v2/stocks/snapshots?symbols=SPY,DIA,IWM";
            var assetResponse = AwaitClientGetReponse(_clientBroker, assetURL).Result;
            var syms = JObject.Parse(assetResponse.ToString());
            foreach (var a in syms)
            {
                var symsMinBar = JObject.Parse(syms[a.Key].ToString())["minuteBar"];
                var symsLClose = JObject.Parse(syms[a.Key].ToString())["prevDailyBar"];
                var price = Convert.ToDouble(JObject.Parse(symsMinBar.ToString()).GetValue("c"));
                var lastPrice = Convert.ToDouble(JObject.Parse(symsLClose.ToString()).GetValue("c"));
                var pcChangeToday = ((price - lastPrice) / lastPrice) * 100;
                if (a.Key == "SPY"){
                    perf.SNPPerf = pcChangeToday;
                }
                else if (a.Key == "DIA"){
                    perf.DOWPerf = pcChangeToday;
                }
                else if (a.Key == "IWM"){
                    perf.RussellPerf = pcChangeToday;
                }
            }
            return perf;
        }
    }
}
