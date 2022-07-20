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

namespace PortfolioPal
{
    public class PortfolioRepo : IPortfolioRepo
    {
        private HttpClient _clientBroker;
        private readonly IDbConnection _conn;
        private string APCA_API_KEY = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_key").ToString();
        private string APCA_API_SECRET = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_secret").ToString();
        private string APCA_API_URL = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_url").ToString();


        private List<string> okPeriod = new List<string>() { "1D", "1W", "1M", "A" };
        private List<string> okTimeframe = new List<string>() { "15Min", "1H", "1D", "1W" };


        public PortfolioRepo ()
        {
            _clientBroker = new HttpClient();
            _clientBroker.DefaultRequestHeaders.Add("APCA-API-KEY-ID", APCA_API_KEY);
            _clientBroker.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", APCA_API_SECRET);
     
            var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
            string connString = config.GetConnectionString("portfoliopal");
            _conn = new MySqlConnection(connString);
            // do we need to close this connection? maybe so? because then we keep openning new ones?
        }

        public Portfolio GetAccount(Portfolio p)
        {
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
                asset.assetID        = JObject.Parse(positions[i].ToString()).GetValue("asset_id").ToString();
                asset.symbol         = JObject.Parse(positions[i].ToString()).GetValue("symbol").ToString();
                asset.exchange       = JObject.Parse(positions[i].ToString()).GetValue("exchange").ToString();
                asset.assetClass     = JObject.Parse(positions[i].ToString()).GetValue("asset_class").ToString();
                asset.qty            = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("qty").ToString());
                asset.side           = JObject.Parse(positions[i].ToString()).GetValue("side").ToString();
                asset.marketValue    = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("market_value").ToString());
                asset.costBasis      = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("cost_basis").ToString());
                asset.plDollarsTotal = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("unrealized_pl").ToString());
                asset.plPercentTotal = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("unrealized_plpc").ToString()) * 100;
                asset.plDollarsToday = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("unrealized_intraday_pl").ToString());
                asset.plPercentToday = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("unrealized_intraday_plpc").ToString()) * 100;
                asset.price          = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("current_price").ToString());
                asset.lastPrice      = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("lastday_price").ToString());
                asset.changeToday    = double.Parse(JObject.Parse(positions[i].ToString()).GetValue("change_today").ToString());
                p.portfolioPositions.Add(asset);
                if (asset.assetClass == "us_equity")
                    p.stockPositions.Add(asset);
                else if (asset.assetClass == "crypto")
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
                    longHolding += asset.marketValue;
                else
                    shortHolding += asset.marketValue;
            foreach (var asset in p.dividendPositions)
                dividendHolding += asset.marketValue;
            foreach (var asset in p.cryptoPositions)
                cryptoHolding += asset.marketValue;
            p.longHoldingActual     = longHolding;
            p.shortHoldingActual    = shortHolding;
            p.stockHoldingActual    = longHolding + Math.Abs(shortHolding);
            p.dividendHoldingActual = dividendHolding;
            p.cryptoHoldingActual   = cryptoHolding;
        }

        public void GetPortfolioHistory(string period = "1D", string timeframe = "15Min")
        {
            if (!okPeriod.Contains(period))
                period = "1D";
            if (!okTimeframe.Contains(timeframe))
                timeframe = "15Min";
            var historyURL = $"{APCA_API_URL}/v2/account/portfolio/history?period={period}&timeframe={timeframe}";
            var historyResponse = _clientBroker.GetStringAsync(historyURL).Result;
            var historyTimestamp = JArray.Parse(JObject.Parse(historyResponse).GetValue("timestamp").ToString()).ToArray();
            var historyEquity = JArray.Parse(JObject.Parse(historyResponse).GetValue("equity").ToString()).ToArray();
            var historyPL = JArray.Parse(JObject.Parse(historyResponse).GetValue("profit_loss").ToString()).ToArray();
            var historyPLP = JArray.Parse(JObject.Parse(historyResponse).GetValue("profit_loss_pct").ToString()).ToArray();
            var historyBaseValue = JObject.Parse(historyResponse).GetValue("base_value").ToString();
            var historyTimeframe = JObject.Parse(historyResponse).GetValue("timeframe").ToString();
            // so you have all this data but youre not sure what to do with it yet. but you have it!
        }

        public void CheckMarketOpen(Portfolio p)
        {
            new Clock();
            p.marketOpen = Clock.marketOpen;
        }

    }
}
