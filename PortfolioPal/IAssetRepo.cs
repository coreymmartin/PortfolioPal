using System;
using System.Collections.Generic;
using PortfolioPal.Models;

namespace PortfolioPal
{
    public interface IAssetRepo
    {
        public IEnumerable<Asset> GetAllTradedAssetsDB();
        public Asset GetTradedAssetDB(string symbol);
        public List<string> GetAllTradedSymbolsDB();
        public Asset GetAsset(string symbol);
        public void AddTradedAssetToDB(Asset asset);
        public void UpdatePortfolioAssetStats(List<string> assets);
        public void UpdateRunningStatsDB(Asset asset);
        public List<ChartDataPoint> GetAssetPriceHistory(string symbol, int limit = 25, string start = "none", string timeframe = "15Min");
    }
}
