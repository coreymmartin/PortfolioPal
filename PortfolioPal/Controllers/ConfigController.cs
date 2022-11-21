using System;
using Dapper;
using System.Data;
using System.Net.Http;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using PortfolioPal.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Mvc;
using PortfolioPal.ViewModels;
using Newtonsoft.Json;
using System.Globalization;

namespace PortfolioPal.Controllers
{
    public class ConfigController : Controller
    {
        private readonly IConfigRepo configRepo;

        public ConfigController(IConfigRepo configRepo)
        {
            this.configRepo = configRepo;
        }


        public IActionResult Index()
        {
            // show current account configurations and user selected assets - do in some tables for now?
            ConfigVM configView = new ConfigVM();
            configView.userSelectedStocks    = configRepo.ReadUserSelectedAssets("stocks");
            configView.userSelectedCoins     = configRepo.ReadUserSelectedAssets("coins");
            configView.userSelectedDividends = configRepo.ReadUserSelectedAssets("dividends");
            configView.accountConfig         = configRepo.ReadAccountConfig();

            // just test writing for now. write to new file so we dont fuck anything up. cool cool.
            configRepo.WriteUserSelectedAssets("stocks", configView.userSelectedStocks);
            configRepo.WriteUserSelectedAssets("coins", configView.userSelectedCoins);
            configRepo.WriteUserSelectedAssets("dividends", configView.userSelectedDividends);
            configRepo.WriteAccountConfig(configView.accountConfig);

            return View(configView);
        }
        public IActionResult ReadAccountConfig()
        {
            // refresh/read account configuration, redirect to same page
            return RedirectToAction("Index");
        }
        public IActionResult WriteAccountConfig()
        {
            // update/write account config, redirect to same page
            return RedirectToAction("Index");
        }
        public IActionResult ReadUserSelectedAssets()
        {
            // read/refresh user selected coins, dividends, stocks, redirect to same page
            return RedirectToAction("Index");
        }
        public IActionResult WriteUserSelectedAssets()
        {
            // update/write user selected coins, dividends, stocks, redirect to same page
            return RedirectToAction("Index");
        }

    }
}
