using PortfolioPal.Models;
using System.Collections.Generic;

namespace PortfolioPal.ViewModels
{
    public class OverviewVM
    {
        public Portfolio Portfolio { get; set; }
        public List<Order> Orders { get; set; }
        public MarketPerf Performance { get; set; }
    }
}
