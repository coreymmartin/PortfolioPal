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
    public class PotentialController : Controller
    {

        private readonly IPotentialRepo potRepo;

        public PotentialController(IPotentialRepo potRepo)
        {
            this.potRepo = potRepo;
        }

        public IActionResult Index()
        {
            ScreenerIndexVM vm = new ScreenerIndexVM();
            potRepo.CreatePotentialViews();
            vm.NumSuggestedAssets = potRepo.GetSQLTableLength("suggestedassets");
            vm.NumUserSelectedAssets = potRepo.GetSQLTableLength("userselectedassets");
            vm.NumAllUpdatedAssets = potRepo.GetSQLTableLength("updatedpotentials");
            vm.NumAllFilteredAssets = potRepo.GetSQLTableLength("allfilteredpotentials");
            vm.NumAllAvailableAssets = potRepo.GetSQLTableLength("potentials");
            vm.SuggestedTradeAssets = potRepo.QueryView("suggestedtradeassets");
            vm.SuggestedDividendAssets = potRepo.QueryView("suggesteddividendassets");
            return View(vm);
        }
        public IActionResult GetMoreData()
        {
            potRepo.GetAllPotentials();
            potRepo.FilterInitialPotentials();
            potRepo.AddMorePotentials();
            potRepo.CreatePotentialViews();
            return RedirectToAction("Index");
        }
        public IActionResult DisplayAllData()
        {
            return View(potRepo.GetPotentialDB());
        }
        public IActionResult Screen()
        {
            potRepo.CreatePotentialViews();
            var screening = potRepo.QueryView("toupdatepotentials");
            screening = potRepo.GetPotentialPrices(screening);
            var updated = potRepo.GetBatchIEXStats(screening);
            foreach (var u in updated)
                potRepo.CalculateStarValue(u);
            potRepo.UpdatePotentialDB(updated);
            return RedirectToAction("Index");
        }
        public IActionResult InitDB()
        {
            potRepo.ClearAllPotentialDB();
            potRepo.GetAllPotentials();
            potRepo.FilterInitialPotentials();
            potRepo.AddMorePotentials();
            potRepo.CreatePotentialViews();
            var screening = potRepo.QueryView("toupdatepotentials");
            screening = potRepo.GetPotentialPrices(screening);
            var updated = potRepo.GetBatchIEXStats(screening);
            foreach (var u in updated)
                potRepo.CalculateStarValue(u);
            potRepo.UpdatePotentialDB(updated);
            return RedirectToAction("Index");
        }
    
    
    
    
    
    
    
    
    
    
    
    }


}
