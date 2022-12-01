﻿namespace PortfolioPal.Models
{
    public class AccountConfig
    {

    #region Account/Trade Parameters
        public bool bAllowBuyNHold {get; set; }        //": true,
        public bool bAllowDayTrade {get; set; }        //": true,
        public bool bAllowShortOrder {get; set; }      //": true,
        public bool bCloseNOptimized {get; set; }      //": false,
        public bool bDoubleCheckStrats {get; set; }    //": false,
        public bool bIgnoreFirst15Min {get; set; }     //": true,
        public bool bDontPlaceOrders {get; set; }      //": true,
        public bool bKillOnOpen {get; set; }           //": true,
        public bool bMarketCloseTSL {get; set; }       //": false,
        public bool bOvernightShorts {get; set; }      //": true,
        public bool bPaperTrade {get; set; }           //": true,
        public bool bPreMarketTrade {get; set; }       //": true,
        public bool bPostMarketTrade {get; set; }      //": true,
        public bool bResetParameters {get; set; }      //": false,
        public bool bStrictDiversity {get; set; }      //": false,
        public bool bTradeOnAvoid {get; set; }         //": false,
        public bool bTradeNOptimized {get; set; }      //": true,
        public bool bUse_HardSL {get; set; }           //": true,
        public bool bUse_TrailSL {get; set; }          //": true,
        public bool bUseChampStrat {get; set; }        //": true,
        public bool bReaffirmTSLStrategy {get; set; }  //": false,
        public bool bRefreshTradeSignals {get; set; }  //": true,
        public double iDayTradeMinimum {get; set; }      //": 35000,
        public double iOrderFillWaitTime {get; set; }    //": 5,
        public double iShortMinimum {get; set; }         //": 10000,
        public double rAssetEquityPercent {get; set; }   //": 0.05,
        public double rBetty_Min_Can2Can {get; set; }    //": 0.002,
        public double rBetty_Min_dCan {get; set; }       //": 0.002,
        public double rDiversity_Tolerance {get; set; }  //": 0.1,
        public double rStock_Diversity {get; set; }      //": 0.60,
        public double rCoin_Diversity {get; set; }       //": 0.00,
        public double rDividend_Diversity {get; set; }   //": 0.35,
        public double rEODPercentShares {get; set; }     //": 0.75,
        public double rEOD_TrailSL {get; set; }          //": 0.011,
        public double rInitialBalance {get; set; }       //": 27000,
        public double rPolly_Confidence_3 {get; set; }   //": 0.85,
        public double rPolly_Confidence_2 {get; set; }   //": 0.9,
        public double rPolly_Confidence_1 {get; set; }   //": 1.0,
        public double rPolly_Limiter {get; set; }        //": 0.003,
        public double rRecoveryPercent {get; set; }      //": 0.015,
        public double rRecoveryReset_HSL {get; set; }    //": 259200,
        public double rRecoveryReset_TSL {get; set; }    //": 86400,
        public double rLimitOrderAdjustment {get; set; } //": 0.0005,
        public double rMinAssetAllowance {get; set; }    //": 250,
        public double rPercentToShort {get; set; }       //": 0.75,
        public double rStatusUpdateRefresh {get; set; }  //": 604800,
        public string sMarketOrderForce {get; set; }     //": "day",
        public string sOHLC {get; set; }                 //": "CLOSE"

    #endregion

    #region Optimizer Parameters

        public bool bOptimizeBettyDay {get; set; }     //": true,
        public bool bOptimizeBettyHour {get; set; }    //": true,
        public bool bOptimizeBetty15Min {get; set; }   //": true,
        public bool bOptimizeMercyDay {get; set; }     //": true,
        public bool bOptimizeMercyHour {get; set; }    //": true,
        public bool bOptimizeMercy15Min {get; set; }   //": true,
        public bool bOptimizePollyDay {get; set; }     //": true,
        public bool bOptimizePollyHour {get; set; }    //": true,
        public bool bOptimizePolly15Min {get; set; }   //": true,
        public bool bOptimizeTSL {get; set; }          //": true,
        public bool bSavePlots {get; set; }            //": true,
        public double rInitialBalance_Opt {get; set; }   //": 1000,
        public double iMaxAssetsToOptimize {get; set; }  //": 5,
        public double iMaxStocksToOptimize {get; set; }  //": 5,
        public double iMaxCoinsToOptimize {get; set; }   //": 5,
        public double iOptimizeStepsLong {get; set; }    //": 4,
        public double iOptimizeStepsShort {get; set; }   //": 4,
        public double iOptimizeStepsFast {get; set; }    //": 4,
        public double iOptimizeStepsSlow {get; set; }    //": 3,
        public double iOptimizeStepsSig {get; set; }     //": 3,
        public double iOptimizeStepsFuture {get; set; }  //": 2,
        public double iOptCandles_Limit {get; set; }     //": 9999,
        public double iOptCandles_day {get; set; }       //": 60,
        public double iOptCandles_hour {get; set; }      //": 650,
        public double iOptCandles_15Min {get; set; }     //": 2500,
        public double iOptCandles_TSL {get; set; }       //": 2500,
        public double rOptExpiration {get; set; }        //": 3024000,
        public double rOptTooFresh {get; set; }          //": 345600,
        public double rOptimizePortLimit {get; set; }    //": -0.20,
        public double rOptimizePerfLimit {get; set; }    //": -0.10,
        public double rHSL_Limit_Lower {get; set; }      //": 0.025,
        public double rHSL_Limit_Upper {get; set; }      //": 0.1,
        public double rTSL_Enable_Lower {get; set; }     //": 0.005,
        public double rTSL_Enable_Upper {get; set; }     //": 0.03,
        public double rTSL_Limit_Lower {get; set; }      //": 0.01,
        public double rTSL_Limit_Upper {get; set; }      //": 0.04,
        public double rHSL_Opt_Steps {get; set; }        //": 0.005,
        public double rTSL_Opt_Steps {get; set; }        //": 0.0025,
        public double rMinOptPLAllow_day {get; set; }    //": 0.05,
        public double rMinOptPLAllow_hour {get; set; }   //": 0.06,
        public double rMinOptPLAllow_15Min {get; set; }  //": 0.07,
        public double rMinOptPLAllow_TSL {get; set; }    //": 0.08,

    #endregion

    }
}
