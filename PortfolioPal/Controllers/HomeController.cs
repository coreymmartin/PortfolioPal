﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PortfolioPal.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PortfolioPal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IPortfolioRepo repo = new PortfolioRepo();

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var p = new Portfolio();
            p = repo.GetAccount(p);
            repo.CheckMarketOpen(p);
            repo.GetAllPortfolioPositions(p);
            repo.UpdatePortfolioDiversity(p);
            repo.GetPortfolioHistory();
            return View(p);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
