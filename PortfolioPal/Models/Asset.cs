namespace PortfolioPal.Models
{
    public class Asset
    {

    # region asset parameters (from broker)

        public string assetID { get; set; }
        public string symbol { get; set; }
        public string exchange { get; set; }
        public string assetClass { get; set; }
        public bool shortable { get; set; }
        public double qty { get; set; }
        public string side { get; set; }
        public double marketValue { get; set; }
        public double costBasis { get; set; }
        public double plDollarsTotal { get; set; }
        public double plPercentTotal { get; set; }
        public double plDollarsToday { get; set; }
        public double plPercentToday { get; set; }
        public double price { get; set; }
        public double lastPrice { get; set; }
        public double changeToday { get; set; }
    
        public double priceBaseline {get; set;}
        public double assetPLP { get; set; }


    # endregion

    # region asset stats (from orders) (broker)
    
        public double TotalTraded { get; set; }
        public int NumberTrades { get; set; }
        public int NumberBuys { get; set; }
        public int NumberSells { get; set; }
        public double TotalPLP { get; set; }
        public double TotalPLD { get; set; }



        public double performance { get; set; } 
    
    
    
    
    
    
    
    # endregion
    
    }
}
