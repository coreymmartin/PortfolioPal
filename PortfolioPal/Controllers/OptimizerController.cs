using Microsoft.AspNetCore.Mvc;

namespace PortfolioPal.Controllers
{
    public class OptimizerController : Controller
    {
        public IActionResult Index()
        {
            // display user selected assets and their optimization parameters
            return View();
        }
        public IActionResult DisplayAssetOptimization()
        {
            // display optimization data and images (graphs) from optimization for specific asset
            return View();
        }
        public IActionResult ResetOptimizationForAsset()
        {
            // reset optimzation data for single asset
            return RedirectToAction("Index");
        }
        public IActionResult ResetOptimizationForAll()
        {
            // reset optimzation data for all assets
            return RedirectToAction("Index");
        }


    }
}
