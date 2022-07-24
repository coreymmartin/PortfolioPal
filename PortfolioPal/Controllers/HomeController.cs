using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
        private readonly IPortfolioRepo portRepo;

        public HomeController(ILogger<HomeController> logger, IPortfolioRepo portRepo)
        {
            this.portRepo = portRepo;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var p = portRepo.GetAccount();
            portRepo.CheckMarketOpen(p);
            portRepo.GetAllPortfolioPositions(p);
            portRepo.UpdatePortfolioDiversity(p);
            ViewBag.PieDataPoints = JsonConvert.SerializeObject(portRepo.GetDiversityChartValues(p));
            ViewBag.HistoryDataPoints = JsonConvert.SerializeObject(portRepo.GetPortfolioHistory("1D", "15Min"));
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
