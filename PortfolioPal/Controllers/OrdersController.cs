using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PortfolioPal.Models;
using PortfolioPal.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PortfolioPal.Controllers
{
    public class OrdersController : Controller
    {

        private readonly IOrderRepo repo;

        public OrdersController(IOrderRepo repo)
        {
            this.repo = repo;
        }

        public IActionResult Index()
        {
            repo.GetLatestOrdersBroker();
            repo.CalcOrderOverview();
            return View(repo);
        }

        public IActionResult AssetOrders(string symbol)
        {
            var assetOrders = repo.ReadAssetOrders(symbol, 500);
            return View(assetOrders);
        }

        public IActionResult AllOrders()
        {
            var allOrds = repo.ReadAllOrdersDB();
            return View(allOrds);
        }

        public IActionResult GetAllOrders()
        {
            repo.GetAllFilledOrders();
            return RedirectToAction("Index");
        }

        public IActionResult UpdateOrders()
        {
            repo.GetNewFilledOrders();
            return RedirectToAction("AllOrders");
        }
    }
}
