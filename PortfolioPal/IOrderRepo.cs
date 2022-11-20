using System;
using System.Collections.Generic;
using PortfolioPal.Models;

namespace PortfolioPal
{
    public interface IOrderRepo
    {
        public void CreateOrderTableDB();
        public void GetAllFilledOrders();
        public void GetNewFilledOrders();
        public void GetLatestOrdersBroker();
        public IEnumerable<Order> ReadAllOrdersDB();
        public IEnumerable<Order> ReadAssetOrders(string asset, int limit);
        public void AddOrdersToDB(List<Order> orders);
        public void CalcOrderOverview();
        public void GetAllTradedAssets();
        public List<string> ReadAllTradedAssetsFromOrders();
        public void UpdateAllOrders();
        public Asset GetUpdatedAssetStats(string symbol);
        public List<ChartDataPoint> ExtractOrderData(List<Order> orders, string side);
        public List<ChartDataPoint> ChartDataFiller(List<ChartDataPoint> sampleData, List<ChartDataPoint> modelData);
        public List<Order> GetBatchOrders(string status = "none", int limit = 0, string afterUntilKey = "none", string afterUntilValue = "none", string direction = "none");

    }
}
