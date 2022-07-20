namespace PortfolioPal.Models
{
    public class Order
    {

        public string orderID {get; set;}
        public string clientOrderId {get; set;}
        public string status {get; set;}
        public string side {get; set;}
        public string filledAt {get; set;}
        public string symbol {get; set;}
        public string assetID {get; set;}
        public string assetClass {get; set;}
        public double filledQty {get; set;}
        public double qty {get; set;}
        public string orderType {get; set;}
        public double filledPrice {get; set;}
        public string timeInForce {get; set;}
        public bool extendedHours {get; set;}

        public Order()
        {

        }

    }
}
