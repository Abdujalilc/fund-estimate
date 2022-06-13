using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ViewModel
{
    public class FundEstimateModel
    {
        [JsonIgnore]
        public int FundEstimateId { get; set; }
        public string TYPE { get; set; }
        public int ID { get; set; }
        public string ACTION { get; set; }
        public string ACC { get; set; }
        public int FINYEAR { get; set; }
        public int SMETA_TYPE { get; set; }
        public int EXPENSE { get; set; }
        public int MONTH { get; set; }
        public double SUMPAY { get; set; }
        public DateTime DOCDATE { get; set; }
        public int FundEstimateLogId { get; set; }
    }
}