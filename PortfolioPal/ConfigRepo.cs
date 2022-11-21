using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using PortfolioPal.Models;

namespace PortfolioPal
{
    public class ConfigRepo : IConfigRepo
    {

        private string userSelectedStockPath    = "C:/Users/corey/Documents/mrmartin/trading/iCoreyPoopsmith/Configs/Universe/UserSelected/UserSelected_Stocks.txt";
        private string userSelectedCoinPath     = "C:/Users/corey/Documents/mrmartin/trading/iCoreyPoopsmith/Configs/Universe/UserSelected/UserSelected_Coins.txt";
        private string userSelectedDividendPath = "C:/Users/corey/Documents/mrmartin/trading/iCoreyPoopsmith/Configs/Universe/UserSelected/UserSelected_Dividends.txt";
        private string userSelectedStockPathWRITE    = "C:/Users/corey/Documents/mrmartin/trading/iCoreyPoopsmith/Configs/Universe/UserSelected/UserSelected_Stocks_WRITE.txt";
        private string userSelectedCoinPathWRITE     = "C:/Users/corey/Documents/mrmartin/trading/iCoreyPoopsmith/Configs/Universe/UserSelected/UserSelected_Coins_WRITE.txt";
        private string userSelectedDividendPathWRITE = "C:/Users/corey/Documents/mrmartin/trading/iCoreyPoopsmith/Configs/Universe/UserSelected/UserSelected_Dividends_WRITE.txt";
        private string accountConfigPath        = "C:/Users/corey/Documents/mrmartin/trading/iCoreyPoopsmith/Configs/TradeParameters/Account_Config";
        private string accountConfigPathWRITE        = "C:/Users/corey/Documents/mrmartin/trading/iCoreyPoopsmith/Configs/TradeParameters/Account_Config_WRITE";

        public ConfigRepo(){

        }


        public AccountConfig ReadAccountConfig()
        {

            AccountConfig accountConfig;
            string config_raw = File.ReadAllText(accountConfigPath);
            accountConfig = JsonSerializer.Deserialize<AccountConfig>(config_raw);
            return accountConfig;

        }

        public List<string> ReadUserSelectedAssets(string classification)
        {
            List<string> userSelectedAssets = new List<string>();
            var filepath = (classification == "stocks") ? userSelectedStockPath : (classification == "coins") ? userSelectedCoinPath : userSelectedDividendPath;  
            string[] raw = File.ReadAllLines(filepath);
            foreach(string r in raw){
                userSelectedAssets.Add(r);
            }
            return userSelectedAssets;
        }

        public void WriteAccountConfig(AccountConfig accountConfig)
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;
            string accountConfigJSON = JsonSerializer.Serialize(accountConfig, options);
            File.WriteAllText(accountConfigPathWRITE, accountConfigJSON);
        }

        public void WriteUserSelectedAssets(string classification, List<string> assets)
        {
            var filepath = (classification == "stocks") ? userSelectedStockPathWRITE : (classification == "coins") ? userSelectedCoinPathWRITE : userSelectedDividendPathWRITE;
            StreamWriter sw = new StreamWriter(filepath, false);
            foreach (var asset in assets){
                sw.WriteLine(asset);
            }
            sw.Close();
        }
    }
}
