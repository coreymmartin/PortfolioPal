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

        public double TotalTraded {get; set;}
        public int NumAssetsTraded {get; set;}
        public double TotalNumberTrades {get; set;}
        public double TotalNumberBuys {get; set;}
        public double TotalNumberSells {get; set;}

        public OrderRepo()
        {
            _clientBroker = new HttpClient();
            _clientBroker.DefaultRequestHeaders.Add("APCA-API-KEY-ID", APCA_API_KEY);
            _clientBroker.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", APCA_API_SECRET);
         
            var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
            string connString = config.GetConnectionString("portfoliopal");
            _conn = new MySqlConnection(connString);
            // do we need to close this connection? maybe so? because then we keep openning new ones?

            RecentFilledOrders = new List<Order>();
        }

        public void CreateOrderTableDB()
        {
            _conn.Execute("DROP TABLE IF EXISTS `orders`; " +
                "CREATE TABLE `orders`( " +
                "`symbol` VARCHAR(10), " +
                "`side` VARCHAR(10), " +
                "`filledQty` FLOAT(10), " +
                "`filledPrice` Float(10), " +
                "`assetClass` VARCHAR(25), " +
                "`orderType` VARCHAR(25), " +
                "`orderStatus` VARCHAR(25), " +
                "`extendedHours` TINYINT, " +
                "`filledAt` VARCHAR(50), " +
                "`timeInForce` VARCHAR(25), " +
                "`qty` FLOAT(10), " +
                "`assetID` VARCHAR(100), " +
                "`clientOrderID` VARCHAR(100), " +
                "`orderID` VARCHAR(100) PRIMARY KEY);");
        }

        public void CheckForTable()
        {
            if (!tableConfirmed){
                if (_conn.ExecuteScalar($"SHOW TABLES LIKE 'orders';") == null)
                CreateOrderTableDB();
                tableConfirmed = true;
            }
        }

        public List<Order> GetBatchOrders(string status, int limit, string afterUntilKey, string afterUntilValue, string direction)
        {
            List<Order> batchOrders = new List<Order>();
            //var ordersURL = $"{APCA_API_URL}/v2/orders?status=closed&limit=500&after={RequestStartDate}&direction=asc";
            //var afterUntilTimeStamp = Convert.ToDateTime(afterUntilValue);
            var afterUntilTimeStamp = (afterUntilValue.Contains(" ")) ? afterUntilValue.Substring(0, afterUntilValue.IndexOf(" ")) : afterUntilValue;
            var ordersURL = $"{APCA_API_URL}/v2/orders?status={status}&limit={limit}&{afterUntilKey}={afterUntilTimeStamp}&direction={direction}";
            var ordersResponse = _clientBroker.GetStringAsync(ordersURL).Result;
            var ordersInfo = JArray.Parse(ordersResponse).ToArray();
            for (var i = 0; i < ordersInfo.Length; i++)
            {
                if (JObject.Parse(ordersInfo[i].ToString()).GetValue("status").ToString() == "filled")
                {
                    var order = new Order();
                    double p1 = 0;
                    double p2 = 0;
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
                    double.TryParse(JObject.Parse(ordersInfo[i].ToString()).GetValue("filled_avg_price").ToString(), out p1);
                    double.TryParse(JObject.Parse(ordersInfo[i].ToString()).GetValue("limit_price").ToString(), out p2);
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
            foreach (var o in orders){
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
            PortfolioRepo portRepo = new PortfolioRepo();
            Portfolio port = portRepo.GetAccount(new Portfolio());
            List<Order> batchOrders = new List<Order>();
            CreateOrderTableDB();
            var RequestStartDate = port.createdAt;
            string newRequestStartDate;
            bool getMore = true;
            do {
                batchOrders = GetBatchOrders("closed", 500, "after", RequestStartDate, "asc");
                if (batchOrders.Count() > 0) {
                    newRequestStartDate = batchOrders.Last().filledAt;
                    AddOrdersToDB(batchOrders);
                    batchOrders.Clear();
                    if (newRequestStartDate == RequestStartDate) {
                        getMore = false; }
                    else {
                        RequestStartDate = newRequestStartDate; }
                } else { 
                    getMore = false; }
            } while (getMore);
        }

        public void GetNewFilledOrders()
        {
            List<Order> batchOrders = new List<Order>();
            var RequestStartDate = GetLatestOrderDB().filledAt;
            bool getMore = true;
            do {
                batchOrders = GetBatchOrders("closed", 500, "after", RequestStartDate, "asc");
                if (batchOrders.Count() > 0) {
                    RequestStartDate = batchOrders.Last().filledAt;
                    AddOrdersToDB(batchOrders);
                    batchOrders.Clear();
                } else { getMore = false; }
            } while (getMore);
        }

        public Order GetLatestOrderDB()
        {
            CheckForTable();
            return _conn.QuerySingle<Order>("SELECT * FROM orders ORDER BY filledAt DESC LIMIT 1");
        }
        public void GetLatestOrdersBroker(int numRecents = 25)
        {
            RecentFilledOrders = GetBatchOrders("filled", numRecents, "until", DateTime.Today.ToShortDateString(), "desc");
        }

        public IEnumerable<Order> ReadLatestOrdersDB(int qty)
        {
            CheckForTable();
            return _conn.Query<Order>($"SELECT * FROM orders ORDER BY filledAt DESC LIMIT {qty}");
        }

        public IEnumerable<Order> ReadAllOrdersDB()
        {
            CheckForTable();
            return _conn.Query<Order>($"SELECT * FROM orders;");
        }

        public IEnumerable<Order> ReadAssetOrders(string asset)
        {
            CheckForTable();
            return _conn.Query<Order>($"SELECT * FROM orders WHERE symbol = {asset} ORDER BY filledAt DESC;");
        }

        public void GetNumTradedAssetsDB(){
            CheckForTable();
            NumAssetsTraded = (int.TryParse(_conn.ExecuteScalar<string>("SELECT COUNT(DISTINCT symbol) FROM orders;"), out int num )) ? num : 0 ;
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


        public void CalcOrderOverview()
        {
            CheckForTable();
            CalcTotalTraded();
            CalcTotalNumberTrades();
            CalcTotalNumberBuys();
            CalcTotalNumberSells();
            GetNumTradedAssetsDB();
            //GetAllTradedAssets();
        }








        /* save this for later when youre smarter and add all this extra stuff to make it work. fool.


        public void CalcAssetTotalTraded(Asset asset)
        {
            asset.TotalTraded = Convert.ToDouble(_conn.ExecuteScalar($"SELECT SUM(filledQty * filledPrice) FROM orders WHERE symbol = {asset.symbol} AND side = 'buy'"));
        }

        public void CalcAssetNumberTrades(Asset asset)
        {
            asset.NumberTrades = Convert.ToDouble(_conn.ExecuteScalar($"SELECT COUNT(*) FROM orders WHERE symbol = {asset.symbol}"));
        }

        public void CalcAssetNumberBuys(Asset asset)
        {
            asset.NumberBuys = Convert.ToDouble(_conn.ExecuteScalar($"SELECT COUNT(*) FROM orders WHERE symbol = {asset.symbol} and side = 'buy'"));
        }

        public void CalcAssetNumberSells(Asset asset)
        {
            asset.NumberSells = Convert.ToDouble(_conn.ExecuteScalar($"SELECT COUNT(*) FROM orders WHERE symbol = {asset.symbol} and side = 'sell'"));
        }

        public void CalcAssetTotalPLD(Asset asset)
        {
            var buyAmount = Convert.ToDouble(_conn.ExecuteScalar($"SELECT SUM(filledQty * filledPrice) FROM orders WHERE symbol = {asset.symbol} and side = 'buy'"));
            var sellAmount = Convert.ToDouble(_conn.ExecuteScalar($"SELECT SUM(filledQty * filledPrice) FROM orders WHERE symbol = {asset.symbol} and side = 'sell'"));
            asset.TotalPLD = buyAmount - sellAmount;
        }

        public void UpdateAssetTotalPLP(Asset asset)
        {
            asset.TotalPLP = asset.TotalPLD / asset.TotalTraded;
        }

        public void GetAllTradedAssets()
        {
            AllTradedAssets = _conn.Query<Asset>("SELECT distinct symbols from tradeassets").ToList();
        }

        public void UpdateAssetStats()
        {
            foreach (var asset in AllTradedAssets){
                CalcAssetTotalTraded(asset);
                CalcAssetNumberTrades(asset);
                CalcAssetNumberBuys(asset);
                CalcAssetNumberSells(asset);
                CalcAssetTotalPLD(asset);
                UpdateAssetTotalPLP(asset);        
            }
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
            UpdateAssetStats();
            UpdateAllAssetStats();
        }

        */

    }
}
