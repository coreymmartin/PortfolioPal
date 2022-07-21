using PortfolioPal.Models;
using System.Collections.Generic;

namespace PortfolioPal.ViewModels
{
    public class TradeAssetVM
    {
        public Asset asset { get; set; }
        public IEnumerable<Order> orders { get; set; }
    }
}
