using PortfolioPal.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PortfolioPal
{
    public interface IPortfolioRepo
    {
        public Portfolio GetAccount();
        public void GetAllPortfolioPositions(Portfolio p);
        public void UpdatePortfolioDiversity(Portfolio p);
        public List<ChartDataPoint> GetPortfolioHistory(string period = "1D", string timeframe = "15Min");
        public void CheckMarketOpen(Portfolio p);
        public List<PieChartDataPoint> GetDiversityChartValues(Portfolio p);
        public MarketPerf GetMarketPerformance();
    }
}
