﻿using System;
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
    public class OrderRepo : IOrderRepo
    {
        private HttpClient _clientBroker;
        private readonly IDbConnection _conn;
        private string APCA_API_KEY = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_key").ToString();
        private string APCA_API_SECRET = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_secret").ToString();
        private string APCA_API_URL = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_url").ToString();
        private bool tableConfirmed = false;
        public List<Order> RecentFilledOrders;
        public List<Order> AllFilledOrders;
        public List<Asset> AllTradedAssets;

        public double TotalTraded {get; set;}
        public int NumAssetsTraded {get; set;}
        public double TotalNumberTrades {get; set;}
        public double TotalNumberBuys {get; set;}
        public double TotalNumberSells {get; set;}

        public OrderRepo(IDbConnection conn)
        {
            _conn = conn;
            _clientBroker = new HttpClient();
            _clientBroker.DefaultRequestHeaders.Add("APCA-API-KEY-ID", APCA_API_KEY);
            _clientBroker.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", APCA_API_SECRET);
            RecentFilledOrders = new List<Order>();

        }


        public void CreateOrderTableDB()
        {
            _conn.Execute("DROP TABLE IF EXISTS `orders`; " +
                "CREATE TABLE `orders`( " +
                "`orderIndex` INT AUTO_INCREMENT PRIMARY KEY, " +
                "`symbol` VARCHAR(10), " +
                "`side` VARCHAR(10), " +
                "`filledQty` FLOAT(10), " +
                "`filledPrice` FLOAT(10), " +
                "`assetClass` VARCHAR(25), " +
                "`orderType` VARCHAR(25), " +
                "`orderStatus` VARCHAR(25), " +
                "`extendedHours` TINYINT, " +
                "`filledAt` VARCHAR(25), " +
                "`timeInForce` VARCHAR(25), " +
                "`qty` FLOAT(10), " +
                "`assetID` VARCHAR(100), " +
                "`clientOrderID` VARCHAR(100), " +
                "`orderID` VARCHAR(100));");
        }

        public void CheckForTable()
        {
            if (!tableConfirmed){
                if (_conn.ExecuteScalar($"SHOW TABLES LIKE 'orders';") == null){
                    CreateOrderTableDB();
                } tableConfirmed = true;
            }
        }

        public List<Order> GetBatchOrders(string status = "none", int limit = 0, string afterUntilKey = "none", string afterUntilValue = "none", string direction = "none")
        {
            // initialize values
            string urlQueries = "?";
            var statusLabel = (status == "open" || status == "closed" || status == "all") ? $"status={status}" : "status=closed";
            var limitLabel = (limit == 0 || limit < 0 || limit > 500) ? "limit=100" : $"limit={limit}";
            var afterUntilKeyLabel = (afterUntilKey != "after" && afterUntilKey != "until") ? "" : $"{afterUntilKey}=";
            string afterUntilValueLabel = "";
            if (afterUntilKeyLabel != "" && afterUntilValue != "none") {
                var templabel = (afterUntilValue.Contains(" ")) ? afterUntilValue.Substring(0, afterUntilValue.IndexOf(" ")) : afterUntilValue;
                afterUntilValueLabel = (DateTime.TryParse(templabel, out DateTime goodDate)) ? (direction == "desc" && goodDate == DateTime.Today) ? $"{goodDate.AddDays(1).ToShortDateString()}" : $"{goodDate.ToShortDateString()}" : "";
            }
            var directionLabel = (direction == "asc" || direction == "desc") ? $"direction={direction}" : "";
            urlQueries += (statusLabel != "") ? $"{statusLabel}&" : "";
            urlQueries += (limitLabel != "") ? $"{limitLabel}&" : "";
            urlQueries += (afterUntilKeyLabel != "" && afterUntilValueLabel != "") ? $"{afterUntilKeyLabel}" : "";
            urlQueries += (afterUntilKeyLabel == "" || afterUntilValueLabel == "") ? "" : $"{afterUntilValueLabel}&";
            urlQueries += (directionLabel != "") ? $"{directionLabel}" : "";
            if (urlQueries[^1].ToString() == "&" || urlQueries[^1].ToString() == "?") {
                urlQueries = (urlQueries == "?" || urlQueries == "&") ? "" : urlQueries.Substring(0, urlQueries.Length - 1);
            }
            List<Order> batchOrders = new List<Order>();
            //var ordersURL = $"{APCA_API_URL}/v2/orders?status={status}&limit={limit}&{afterUntilKey}={afterUntilValueLabel}&direction={direction}";
            var ordersURL = $"{APCA_API_URL}/v2/orders{urlQueries}";
            var ordersResponse = _clientBroker.GetStringAsync(ordersURL).Result;
            var ordersInfo = JArray.Parse(ordersResponse).ToArray();
            for (var i = 0; i < ordersInfo.Length; i++)
            {
                if (JObject.Parse(ordersInfo[i].ToString()).GetValue("status").ToString() == "filled")
                {
                    var order = new Order();
                    order.orderID = JObject.Parse(ordersInfo[i].ToString()).GetValue("id").ToString();
                    order.clientOrderId = JObject.Parse(ordersInfo[i].ToString()).GetValue("client_order_id").ToString();
                    order.status = JObject.Parse(ordersInfo[i].ToString()).GetValue("status").ToString();
                    order.filledAt = JObject.Parse(ordersInfo[i].ToString()).GetValue("filled_at").ToString();
                    order.symbol = JObject.Parse(ordersInfo[i].ToString()).GetValue("symbol").ToString();
                    order.side = JObject.Parse(ordersInfo[i].ToString()).GetValue("side").ToString();
                    order.assetID = JObject.Parse(ordersInfo[i].ToString()).GetValue("asset_id").ToString();
                    order.assetClass = JObject.Parse(ordersInfo[i].ToString()).GetValue("asset_class").ToString();
                    order.filledQty = Convert.ToDouble(JObject.Parse(ordersInfo[i].ToString()).GetValue("filled_qty"));
                    order.qty = Convert.ToDouble(JObject.Parse(ordersInfo[i].ToString()).GetValue("qty"));
                    order.orderType = JObject.Parse(ordersInfo[i].ToString()).GetValue("order_type").ToString();
                    double.TryParse(JObject.Parse(ordersInfo[i].ToString()).GetValue("filled_avg_price").ToString(), out double p1);
                    double.TryParse(JObject.Parse(ordersInfo[i].ToString()).GetValue("limit_price").ToString(), out double p2);
                    order.filledPrice = (p1 != p2 || (p1 != 0 && p2 != 0)) ? (p1 != 0) ? p1 : p2 : 0;
                    order.timeInForce = JObject.Parse(ordersInfo[i].ToString()).GetValue("time_in_force").ToString();
                    order.extendedHours = Convert.ToBoolean(JObject.Parse(ordersInfo[i].ToString()).GetValue("extended_hours").ToString());
                    batchOrders.Add(order);
                }
            }
            return batchOrders;
        }

        public void AddOrdersToDB(List<Order> orders)
        {
            foreach (var o in orders) {
                _conn.Execute("INSERT INTO orders (orderID, clientOrderId, orderStatus, filledAt, symbol, side, assetID, assetClass, filledQty, qty, orderType, filledPrice, " +
                    "timeInForce, extendedHours) VALUES (@orderID, @clientOrderId, @status, @filledAt, @symbol, @side, @assetID, @assetClass, @filledQty, @qty, @orderType, " +
                    "@filledPrice, @timeInForce, @extendedHours) ON DUPLICATE KEY UPDATE orderID = orderID;",
                new
                {
                    orderID = o.orderID, clientOrderId = o.clientOrderId, status = o.status, filledAt = o.filledAt, symbol = o.symbol, o.side,
                    assetID = o.assetID, assetClass = o.assetClass, filledQty = o.filledQty, qty = o.qty, orderType = o.orderType,
                    filledPrice = o.filledPrice, timeInForce = o.timeInForce, extendedHours = o.extendedHours
                });
            }
        }

        public void GetAllFilledOrders()
        {
            PortfolioRepo portRepo = new PortfolioRepo(_conn);
            var port = portRepo.GetAccount();
            List<Order> batchOrders;
            CreateOrderTableDB();
            var RequestStartDate = port.createdAt;
            string newRequestStartDate;
            bool getMore = true;
            do {
                batchOrders = GetBatchOrders("closed", 500, "after", RequestStartDate, "asc");
                if (batchOrders.Count() > 0) {
                    newRequestStartDate = batchOrders.Last().filledAt;
                    if (newRequestStartDate == RequestStartDate) {
                        getMore = false; }
                    else {
                        AddOrdersToDB(batchOrders);
                        batchOrders.Clear();
                        RequestStartDate = newRequestStartDate; }
                } else {
                    getMore = false; }
            } while (getMore);
        }

        public void GetNewFilledOrders()
        {
            List<Order> batchOrders;
            string newRequestStartDate;
            var RequestStartDate = GetLatestOrderDB().filledAt;
            bool getMore = true;
            do {
                batchOrders = GetBatchOrders("closed", 500, "after", RequestStartDate, "asc");
                if (batchOrders.Count() > 0) {
                    newRequestStartDate = batchOrders.Last().filledAt;
                    if (newRequestStartDate == RequestStartDate) {
                        getMore = false; }
                    else {
                        AddOrdersToDB(batchOrders);
                        batchOrders.Clear();
                        RequestStartDate = newRequestStartDate; }
                }
                else {
                    getMore = false; }
            } while (getMore);
        }

        public Order GetLatestOrderDB()
        {
            CheckForTable();
            return _conn.QuerySingle<Order>("SELECT * FROM orders ORDER BY orderIndex DESC LIMIT 1");
        }
        public void GetLatestOrdersBroker(int numRecents = 100)
        {
            //RecentFilledOrders = GetBatchOrders("filled", numRecents, "until", DateTime.Today.ToShortDateString(), "desc");
            //RecentFilledOrders = GetBatchOrders("", numRecents, "", "", "");
            RecentFilledOrders = GetBatchOrders();
        }

        public IEnumerable<Order> ReadLatestOrdersDB(int qty)
        {
            CheckForTable();
            return _conn.Query<Order>($"SELECT * FROM orders ORDER BY orderIndex DESC LIMIT {qty}");
        }

        public IEnumerable<Order> ReadAllOrdersDB()
        {
            CheckForTable();
            return _conn.Query<Order>($"SELECT * FROM orders ORDER BY orderIndex DESC;");
        }

        public IEnumerable<Order> ReadAssetOrders(string asset, int limit = 500)
        {
            CheckForTable();
            return _conn.Query<Order>($"SELECT * FROM orders WHERE symbol = '{asset}' ORDER BY orderIndex DESC LIMIT {limit};");
        }

        public void GetNumTradedAssetsDB() {
            CheckForTable();
            NumAssetsTraded = (int.TryParse(_conn.ExecuteScalar<string>("SELECT COUNT(DISTINCT symbol) FROM orders;"), out int num)) ? num : 0;
        }

        public void CalcTotalTraded()
        {
            TotalTraded = Convert.ToDouble(_conn.ExecuteScalar($"SELECT SUM(filledQty * filledPrice) FROM orders WHERE side = 'buy'"));
        }

        public void CalcTotalNumberTrades()
        {
            TotalNumberTrades = Convert.ToDouble(_conn.ExecuteScalar($"SELECT COUNT(*) FROM orders"));
        }

        public void CalcTotalNumberBuys()
        {
            TotalNumberBuys = Convert.ToDouble(_conn.ExecuteScalar($"SELECT COUNT(*) FROM orders WHERE side = 'buy'"));
        }

        public void CalcTotalNumberSells()
        {
            TotalNumberSells = Convert.ToDouble(_conn.ExecuteScalar($"SELECT COUNT(*) FROM orders WHERE side = 'sell'"));
        }

        public void CalcAssetTotalTraded(Asset asset)
        {
            asset.TotalTraded = Convert.ToDouble(_conn.ExecuteScalar($"SELECT SUM(filledQty * filledPrice) FROM orders WHERE symbol = '{asset.symbol}' AND side = 'buy'"));
        }

        public void CalcAssetNumberTrades(Asset asset)
        {
            asset.NumberTrades = Convert.ToInt32(_conn.ExecuteScalar($"SELECT COUNT(*) FROM orders WHERE symbol = '{asset.symbol}'"));
        }

        public void CalcAssetNumberBuys(Asset asset)
        {
            asset.NumberBuys = Convert.ToInt32(_conn.ExecuteScalar($"SELECT COUNT(*) FROM orders WHERE symbol = '{asset.symbol}' and side = 'buy'"));
        }

        public void CalcAssetNumberSells(Asset asset)
        {
            asset.NumberSells = Convert.ToInt32(_conn.ExecuteScalar($"SELECT COUNT(*) FROM orders WHERE symbol = '{asset.symbol}' and side = 'sell'"));
        }

        public void CalcAssetTotalPLD(Asset asset)
        {
            var buyAmount = Convert.ToDouble(_conn.ExecuteScalar($"SELECT SUM(filledQty * filledPrice) FROM orders WHERE symbol = '{asset.symbol}' and side = 'buy'"));
            var sellAmount = Convert.ToDouble(_conn.ExecuteScalar($"SELECT SUM(filledQty * filledPrice) FROM orders WHERE symbol = '{asset.symbol}' and side = 'sell'"));
            asset.TotalPLD = buyAmount - sellAmount;
        }

        public void UpdateAssetTotalPLP(Asset asset)
        {
            asset.TotalPLP = (asset.TotalPLD / asset.TotalTraded) * 100;
        }

        public void GetAllTradedAssets()
        {
            AllTradedAssets = _conn.Query<Asset>("SELECT distinct symbols from tradedassets").ToList();
        }

        public List<string> ReadAllTradedAssetsFromOrders()
        {
            return _conn.Query<string>("SELECT distinct symbol from orders").ToList();
        }

        public List<ChartDataPoint> ExtractOrderData(List<Order> orders)
        {
            string _date;
            double _price;
            List<ChartDataPoint> orderDataPoints = new List<ChartDataPoint>();
            orders.Reverse();
            foreach (var o in orders)
            {
                _date = o.filledAt;
                _price = (double.TryParse(o.filledPrice.ToString(), out double d)) ? d : 0;
                if (_price != 0){
                    orderDataPoints.Add(new ChartDataPoint(_date.ToString(), _price));
                }
            }
            return orderDataPoints;
        }

        public List<ChartDataPoint> ChartDataFiller(List<ChartDataPoint> sampleData, List<ChartDataPoint> modelData)
        {
            int sampleCountMax = sampleData.Count();
            int sampleCount = 0;
            List<ChartDataPoint> filledData = new List<ChartDataPoint>();
            foreach(var m in modelData)
            {
                if (Convert.ToDateTime(m.Label) < Convert.ToDateTime(sampleData[sampleCount].Label))
                {
                    filledData.Add(new ChartDataPoint(m.Label, null));
                }
                else if (Convert.ToDateTime(m.Label) >= Convert.ToDateTime(sampleData[sampleCount].Label))
                {
                    filledData.Add(new ChartDataPoint(sampleData[sampleCount].Label, Convert.ToDouble(sampleData[sampleCount].Y)));
                    sampleCount++;
                }
                if (sampleCount >= sampleCountMax)
                {
                    break;
                }
            }
            return filledData;
        }


        public void CalcOrderOverview()
        {
            CheckForTable();
            CalcTotalTraded();
            CalcTotalNumberTrades();
            CalcTotalNumberBuys();
            CalcTotalNumberSells();
            GetNumTradedAssetsDB();
        }


        public Asset GetUpdatedAssetStats(string symbol)
        {
            Asset asset = new Asset();
            asset.symbol = symbol;
            CalcAssetTotalTraded(asset);
            CalcAssetNumberTrades(asset);
            CalcAssetNumberBuys(asset);
            CalcAssetNumberSells(asset);
            CalcAssetTotalPLD(asset);
            UpdateAssetTotalPLP(asset);
            return asset;
        }

        public void UpdateAllAssetStats()
        {
            CalcTotalTraded();
            CalcTotalNumberTrades();
            CalcTotalNumberBuys();
            CalcTotalNumberSells();
        }

        public void UpdateAllOrders()
        {
            GetNewFilledOrders();
            GetAllTradedAssets();
            foreach (var a in AllTradedAssets){
                GetUpdatedAssetStats(a.symbol);
            }
            UpdateAllAssetStats();
            // now what?! do we even use this?
        }
   
   
   
   
   
   
   
   
   
   
   
   
    }
}