﻿using System;
using System.Runtime.Serialization;

namespace PortfolioPal.Models
{
    [DataContract]
    public class PieChartDataPoint
    {
        public PieChartDataPoint(string label, double y)
        {
            this.Label = label;
            this.Y = y;
        }
        [DataMember(Name = "label")]
        public string Label = "";

        [DataMember(Name = "y")]
        public Nullable<double> Y = null;
    }
}
