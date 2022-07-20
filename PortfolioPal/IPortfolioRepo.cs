using PortfolioPal.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PortfolioPal
{
    public interface IPortfolioRepo
    {

        public Portfolio GetAccount(Portfolio p);
        public void GetAllPortfolioPositions(Portfolio p);
        public void UpdatePortfolioDiversity(Portfolio p);
        public void GetPortfolioHistory(string period = "1D", string timeframe = "15Min");
        public void CheckMarketOpen(Portfolio p);
    }
}
