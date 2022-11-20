using System;
using System.Collections.Generic;

namespace PortfolioPal.Models
{
    public class Potential
    {
        public string assetID { get; set; }
        public string symbol { get; set; }
        public double score {get; set;}
        public double price{ get; set; }
        public string companyName {get; set;}
        public double marketCap {get; set;}
        public double week52High {get; set;}
        public double week52Low {get; set;}
        public double week52Change {get; set;}
        public double avg10Volume {get; set;}
        public double avg30Volume {get; set;}
        public double day200MovingAvg {get; set;}
        public double day50MovingAvg {get; set;}
        public double employees {get; set;}
        public double ttmEPS {get; set;}
        public double ttmDividendRate {get; set;}
        public double dividendYield {get; set;}
        public double dividendFrequency {get; set;}
        public string nextDividendDate {get; set;}
        public string exDividendDate {get; set;}
        public double peRatio {get; set;}
        public double beta {get; set;}
        public double maxChangePercent {get; set;}
        public double year5ChangePercent {get; set;}
        public double year2ChangePercent {get; set;}
        public double year1ChangePercent {get; set;}
        public double ytdChangePercent {get; set;}
        public double month6ChangePercent {get; set;}
        public double month3ChangePercent {get; set;}
        public double month1ChangePercent {get; set;}
        public double day30ChangePercent {get; set;}
        public double day5ChangePercent {get; set;}
        public string exchange { get; set; }
        public bool shortable { get; set; }
        public string assetClass { get; set; }
        public string updated {get; set;}
        public Potential()
        {

        }

        /*
        Some Rules:
        - we should read these from config later and be able to ajust them as admin
            dividend yeild (3-8%)
            dividend growth: 3- 10% annually
            years of dividend growth: at least 5
            payout ratio (equity / dividends): ~ less than 50-90%
            debt/equity ratio: <= 2
            expected earning growth: 5 - 15% ? do we need this?
        */

    }
}
