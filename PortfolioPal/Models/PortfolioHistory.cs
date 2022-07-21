namespace PortfolioPal.Models
{
    public class PortfolioHistory
    {
        public PortfolioHistory()
        {

        }


        public string[] Timestamp {get;set;}
        public string[] Equity {get;set;}
        public string[] PL {get;set;}
        public string[] PLP {get;set;}
        public string[] BaseValue {get;set;}
        public string[] Timeframe { get; set; }

    }
}
