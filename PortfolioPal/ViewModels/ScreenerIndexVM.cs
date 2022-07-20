using PortfolioPal.Models;
using System.Collections.Generic;

namespace PortfolioPal.ViewModels
{
    public class ScreenerIndexVM
    {
        public int NumSuggestedAssets {get; set;}
        public int NumUserSelectedAssets { get; set; }
        public int NumAllUpdatedAssets { get; set; }
        public int NumAllAvailableAssets { get; set; }
        public int NumAllFilteredAssets { get; set; }
        public List<string> UserRequiredAssets { get; set; }
        public List<string> UserOptionalAssets { get; set; }
        public IEnumerable<Potential> SuggestedTradeAssets { get; set; }
        public IEnumerable<Potential> SuggestedDividendAssets {get; set;}
    }
}
