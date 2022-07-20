using PortfolioPal.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PortfolioPal
{
    public interface IPotentialRepo
    {

        public int GetSQLTableLength(string table);
        public void CalculateStarValue(Potential p);
        public void AddMorePotentials(bool CheckForUserSelected = true);











        public void GetAllPotentials();
        public void FilterInitialPotentials();
        public IEnumerable<Potential> GetPotentialPrices(IEnumerable<Potential> assets);
        public void CreatePotentialViews();
        public List<Potential> GetBatchIEXStats(IEnumerable<Potential> assets, int creditCost = 5);
        public IEnumerable<Potential> QueryView(string view);
        public IEnumerable<Potential> GetPotentialDB();
        public Potential GetPotentialDB(Potential potential);

        public void AddPotentialDB(Potential potential);
        public void UpdatePotentialDB(Potential potential, bool updateStamp);
        public void UpdatePotentialDB(IEnumerable<Potential> potentials, bool updateStamp = true);
        public void ClearAllPotentialDB();
        public void ClearPotentialDB(Potential potential);

    }
}
