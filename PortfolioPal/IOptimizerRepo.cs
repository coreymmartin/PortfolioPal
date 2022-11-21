using PortfolioPal.Models;

namespace PortfolioPal
{
    public interface IOptimizerRepo
    {

        public void GetOptimizationAllAssets();

        public Asset GetOptimizationSingleAsset();

        public void ResetAllOptimizationValues();

        public void ResetAssetOptimizationValues();

        public object GetOptimizationGraphs();



    }
}
