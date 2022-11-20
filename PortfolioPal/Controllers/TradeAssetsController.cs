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


        public TradeAssetsController(IAssetRepo assetRepo, IOrderRepo orderRepo)
        {
            this.assetRepo = assetRepo;
            this.orderRepo = orderRepo;
        }


        public IActionResult Index()
        {
            return View(assetRepo.GetAllTradedAssetsDB());
        }
        public IActionResult TradedAsset(string symbol)
        {
            TradeAssetVM assetVM = new TradeAssetVM
            {
                asset = assetRepo.GetTradedAssetDB(symbol),
                orders = orderRepo.ReadAssetOrders(symbol, 25)
            };
            var startDate = assetVM.orders.Min(x => x.filledAt);
            var startDateFmt = DateTime.Parse(startDate).ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo);
            var numDays = (DateTime.Today - DateTime.Parse(startDate.ToString())).TotalDays;
            int reqLimit = Convert.ToInt32(numDays) * 50;
            var assetEquity = assetRepo.GetAssetPriceHistory(symbol, reqLimit, startDateFmt, "15Min");
            var orderDataRaw_Buy = orderRepo.ExtractOrderData(assetVM.orders.ToList(), "buy");
            var orderDataRaw_Sell = orderRepo.ExtractOrderData(assetVM.orders.ToList(), "sell");
            var orderDataFit_Buy = orderRepo.ChartDataFiller(orderDataRaw_Buy, assetEquity);
            var orderDataFit_Sell = orderRepo.ChartDataFiller(orderDataRaw_Sell, assetEquity);
            ViewBag.AssetEquity = JsonConvert.SerializeObject(assetEquity);
            ViewBag.OrderDatasBuy = JsonConvert.SerializeObject(orderDataFit_Buy);
            ViewBag.OrderDatasSell = JsonConvert.SerializeObject(orderDataFit_Sell);
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
