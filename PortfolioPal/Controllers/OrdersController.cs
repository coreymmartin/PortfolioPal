﻿using Microsoft.AspNetCore.Mvc;
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

        public IOrderRepo repo = new OrderRepo();

        public IActionResult Index()
        {
            repo.GetLatestOrdersBroker();
            repo.CalcOrderOverview();
            return View(repo);
        }

        public IActionResult DisplayAssetOrders(Asset asset)
        {
            var assetOrders = repo.ReadAssetOrders(asset.symbol);
            return View(assetOrders);
        }

        public IActionResult DisplayAllOrders()
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
            return RedirectToAction("Index");
        }
    }
}