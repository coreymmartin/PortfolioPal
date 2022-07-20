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

        private readonly IPotentialRepo repo = new PotentialRepo();

        public IActionResult Index()
        {
            ScreenerIndexVM vm = new ScreenerIndexVM();
            repo.CreatePotentialViews();
            vm.NumSuggestedAssets = repo.GetSQLTableLength("suggestedassets");
            vm.NumUserSelectedAssets = repo.GetSQLTableLength("userselectedassets");
            vm.NumAllUpdatedAssets = repo.GetSQLTableLength("updatedpotentials");
            vm.NumAllFilteredAssets = repo.GetSQLTableLength("allfilteredpotentials");
            vm.NumAllAvailableAssets = repo.GetSQLTableLength("potentials");
            vm.SuggestedTradeAssets = repo.QueryView("suggestedtradeassets");
            vm.SuggestedDividendAssets = repo.QueryView("suggesteddividendassets");
            return View(vm);
        }
        public IActionResult GetMoreData()
        {
            repo.GetAllPotentials();
            repo.FilterInitialPotentials();
            repo.AddMorePotentials();
            repo.CreatePotentialViews();
            return RedirectToAction("Index");
        }
        public IActionResult DisplayAllData()
        {
            return View(repo.GetPotentialDB());
        }
        public IActionResult Screen()
        {
            repo.CreatePotentialViews();
            var screening = repo.QueryView("toupdatepotentials");
            screening = repo.GetPotentialPrices(screening);
            var updated = repo.GetBatchIEXStats(screening);
            foreach (var u in updated)
                repo.CalculateStarValue(u);
            repo.UpdatePotentialDB(updated);
            return RedirectToAction("Index");
        }
        public IActionResult InitDB()
        {
            repo.ClearAllPotentialDB();
            repo.GetAllPotentials();
            repo.FilterInitialPotentials();
            repo.AddMorePotentials();
            repo.CreatePotentialViews();
            var screening = repo.QueryView("toupdatepotentials");
            screening = repo.GetPotentialPrices(screening);
            var updated = repo.GetBatchIEXStats(screening);
            foreach (var u in updated)
                repo.CalculateStarValue(u);
            repo.UpdatePotentialDB(updated);
            return RedirectToAction("Index");
        }
    
    
    
    
    
    
    
    
    
    
    
    }


}
