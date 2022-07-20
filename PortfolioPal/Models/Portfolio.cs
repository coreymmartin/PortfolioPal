using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;

namespace PortfolioPal.Models
{
    public class Portfolio
    {

    #region constants - read these from config later
        public const double ShortDiversity = 0.70;
        public const double StockDiversity = 0.50;
        public const double CryptoDiversity = 0.30;
        public const double DividendDiversity = 0.15;
        public const double DiversityTolerance = 0.15;

    #endregion

    #region portfolio positions
        public List<Asset> portfolioPositions { get; set; }
        public List<Asset> dividendPositions { get; set; }
        public List<Asset> stockPositions { get; set; }
        public List<Asset> cryptoPositions { get; set; }

    #endregion


    #region account stuff / misc
        public bool marketOpen {get; set;}
        public long deposits { get; set; }
        public long withdraws { get; set; }

    #endregion

    #region portfolio info / account data
        public string accountNumber { get; set; }
        public string status { get; set; }
        public string currency { get; set; }
        public string cryptoStatus { get; set; }
        public double buyingPower { get; set; }
        public double cash { get; set; }
        public double accrued_fees { get; set; }
        public double portfolioValue { get; set; }
        public string patternDayTrader { get; set; }
        public string createdAt { get; set; }
        public double equity { get; set; }
        public double lastEquity { get; set; }
        public double todaysPLD { get; set; }
        public double todaysPLP { get; set; }
        public double longMarketValue { get; set; }
        public double shortMarketValue { get; set; }

    #endregion

    #region portfolio diversity / allowance (calculated)
        public double stockHoldingLimit { get; set; }
        public double cryptoHoldingLimit { get; set; }
        public double dividendHoldingLimit { get; set; }
        public double stockHoldingActual { get; set; }
        public double longHoldingActual { get; set; }
        public double shortHoldingActual { get; set; }
        public double cryptoHoldingActual { get; set; }
        public double dividendHoldingActual { get; set; }

    #endregion


    # region order stats stuff
        public int TotalTrades { get; set; }
        public double TotalTraded { get; set; }
        // add more stuff like favorite, best, worst, random fun facts, yada yada. thank you.









    # endregion




        public Portfolio()
        {
            portfolioPositions = new List<Asset>();
            dividendPositions  = new List<Asset>();
            stockPositions     = new List<Asset>();
            cryptoPositions    = new List<Asset>();
        }

    }
}
