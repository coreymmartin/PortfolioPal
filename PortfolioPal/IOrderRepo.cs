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
        public void GetLatestOrdersBroker(int numRecents = 25);
        public IEnumerable<Order> ReadAllOrdersDB();
        public IEnumerable<Order> ReadAssetOrders(string asset, int limit);
        
        public void AddOrdersToDB(List<Order> orders);
       
       public void CalcOrderOverview();
       public void GetAllTradedAssets();
       
       public List<string> ReadAllTradedAssetsFromOrders();

        public void UpdateAllOrders();
        public Asset GetUpdatedAssetStats(string symbol);
        public List<ChartDataPoint> ExtractOrderData(List<Order> orders);

        public List<ChartDataPoint> ChartDataFiller(List<ChartDataPoint> sampleData, List<ChartDataPoint> modelData);

        //public void RemoveAllOrdersDB();






        // later if you have time
        // public void UpdateOrderStatsAll();
        // public void UpdateOrderStatsAsset(Asset asset);
        // later later 
        //public void GetLatestFailedOrders();
    }
}
