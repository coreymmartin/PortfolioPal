using System;
using System.Collections.Generic;
using PortfolioPal.Models;

namespace PortfolioPal
{
    public interface IAssetRepo
    {
        //public string[] GetAllTradeableAssets();
        //public IEnumerable<Asset> GetAsset();

        public IEnumerable<Asset> GetAllTradedAssetsDB();
        public Asset GetTradedAssetDB(string symbol);

        public List<string> GetAllTradedSymbolsDB();

        public void AddTradedAssetToDB(Asset asset);

    }
}
