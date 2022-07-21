using Microsoft.AspNetCore.Mvc;
using PortfolioPal.Models;
using PortfolioPal.ViewModels;
using System.Collections.Generic;

namespace PortfolioPal.Controllers
{
    public class TradeAssetsController : Controller
    {
        private readonly IAssetRepo assetRepo;
        private readonly IOrderRepo orderRepo;

        public TradeAssetsController(IAssetRepo repo)
        {
            this.assetRepo = repo;
        }

        public TradeAssetsController(IOrderRepo repo)
        {
            this.orderRepo = repo;
        }


        public IActionResult Index()
        {
            // just show all available assets
            return View(assetRepo.GetAllTradedAssetsDB());
        }
        public IActionResult TradedAsset(string symbol)
        {
            // just show one specific one 
            // so show stats from asset and then also all the orders for that asset. neato!
            TradeAssetVM assetVM = new TradeAssetVM();
            assetVM.asset = assetRepo.GetTradedAssetDB(symbol);
            assetVM.orders = orderRepo.ReadAssetOrders(symbol);
            return View(assetVM);
        }

        public IActionResult UpdateAssets()
        {
            // so we want to grab all traded assets from orders and then make sure they 
            // are in the tradedassets table, if not then we will add them.
            // we can also grab the curret trade assets from our portfolio 
            // and then we can udate their stats. 
            // be able to click on each asset and see the orders and such. how neat.
            var allTradedOrders = orderRepo.ReadAllTradedAssetsFromOrders();
            var allTradedSymbols = assetRepo.GetAllTradedSymbolsDB();
            foreach (var a in allTradedOrders)
            {
                if (!allTradedSymbols.Contains(a))
                {
                    Asset temp = new Asset();
                    temp.symbol = a;
                    assetRepo.AddTradedAssetToDB(temp);
                }
            }
                       
            return RedirectToAction("Index");
        }


    }
}
