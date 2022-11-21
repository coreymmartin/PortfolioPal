using System.Collections.Generic;
using PortfolioPal.Models;

namespace PortfolioPal
{
    public interface IConfigRepo
    {
        public List<string> ReadUserSelectedAssets(string classification);
        public void WriteUserSelectedAssets(string classification, List<string> assets);
        public AccountConfig ReadAccountConfig();
        public void WriteAccountConfig(AccountConfig accountConfig);

    }
}
