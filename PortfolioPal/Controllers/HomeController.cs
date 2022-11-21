using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PortfolioPal.Models;
using PortfolioPal.ViewModels;
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
        private readonly IOrderRepo orderRepo;


        public HomeController(ILogger<HomeController> logger, IPortfolioRepo portRepo, IOrderRepo orderRepo)
        {
            this.portRepo = portRepo;
            this.orderRepo = orderRepo;
            _logger = logger;
        }

        public IActionResult Index()
        {
            OverviewVM overview = new OverviewVM();
            var p = portRepo.GetAccount();
            portRepo.CheckMarketOpen(p);
            portRepo.GetAllPortfolioPositions(p);
            portRepo.UpdatePortfolioDiversity(p);
            ViewBag.PieDataPoints = JsonConvert.SerializeObject(portRepo.GetDiversityChartValues(p));
            ViewBag.HistoryDataPoints = JsonConvert.SerializeObject(portRepo.GetPortfolioHistory("1D", "5Min"));
            overview.Portfolio = p;
            overview.Performance = portRepo.GetMarketPerformance();
            overview.Orders = orderRepo.GetBatchOrders("closed", 10, "none", "none", "none");
            return View(overview);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
