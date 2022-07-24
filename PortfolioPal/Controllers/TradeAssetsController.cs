using System;
using Dapper;
using System.Data;
using System.Net.Http;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using PortfolioPal.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Mvc;
using PortfolioPal.ViewModels;
using Newtonsoft.Json;
using System.Globalization;

namespace PortfolioPal.Controllers
{
    public class TradeAssetsController : Controller
    {
        private readonly IAssetRepo assetRepo;
        private readonly IOrderRepo orderRepo;
        //private readonly IPortfolioRepo portRepo;

        //public TradeAssetsController(IAssetRepo repo)
        //{
        //    this.assetRepo = repo;
        //}
        //
        //public TradeAssetsController(IOrderRepo repo)
        //{
        //    this.orderRepo = repo;
        //}

        public TradeAssetsController(IAssetRepo assetRepo, IOrderRepo orderRepo /*, IPortfolioRepo portRepo*/)
        {
            this.assetRepo = assetRepo;
            this.orderRepo = orderRepo;
            //this.portRepo = portRepo;
        }


        public IActionResult Index()
        {
            return View(assetRepo.GetAllTradedAssetsDB());
        }
        public IActionResult TradedAsset(string symbol)
        {
            TradeAssetVM assetVM = new TradeAssetVM();
            assetVM.asset = assetRepo.GetTradedAssetDB(symbol);
            assetVM.orders = orderRepo.ReadAssetOrders(symbol, 25);
            var startDate = assetVM.orders.Min(x => x.filledAt);
            var startDateFmt = DateTime.Parse(startDate).ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo);
            var numDays = (DateTime.Today - DateTime.Parse(startDate.ToString())).TotalDays;
            int reqLimit = Convert.ToInt32(numDays) * 50;
            var assetEquity = assetRepo.GetAssetPriceHistory(symbol, reqLimit, startDateFmt, "15Min");
            var orderDataRaw = orderRepo.ExtractOrderData(assetVM.orders.ToList());
            var orderDataFit = orderRepo.ChartDataFiller(orderDataRaw, assetEquity);
            ViewBag.AssetEquity = JsonConvert.SerializeObject(assetEquity);
            ViewBag.OrderDatas = JsonConvert.SerializeObject(orderDataFit);
            return View(assetVM);
        }

        public IActionResult UpdateAssets()
        {
            var allTradedOrders = orderRepo.ReadAllTradedAssetsFromOrders();
            var allTradedSymbols = assetRepo.GetAllTradedSymbolsDB();
            foreach (var a in allTradedOrders) {
                if (!allTradedSymbols.Contains(a)) {
                    assetRepo.AddTradedAssetToDB(assetRepo.GetAsset(a));
                }
            }
            assetRepo.UpdatePortfolioAssetStats(allTradedOrders);
            foreach (var a in allTradedOrders){
                var aUpt = orderRepo.GetUpdatedAssetStats(a);
                assetRepo.UpdateRunningStatsDB(aUpt);
            }
            return RedirectToAction("Index");
        }
    }
}
