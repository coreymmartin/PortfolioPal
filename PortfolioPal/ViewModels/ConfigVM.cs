using PortfolioPal.Models;
using System.Collections.Generic;

namespace PortfolioPal.ViewModels

{
    public class ConfigVM
    {
        public List<string> userSelectedStocks { get; set; }
        public List<string> userSelectedCoins { get; set; }
        public List<string> userSelectedDividends { get; set; }
        public AccountConfig accountConfig { get; set; }


    }
}
