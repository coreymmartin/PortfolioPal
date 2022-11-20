namespace PortfolioPal.Models
{
    public class Asset {

    # region asset parameters (from broker)
        public string asset_id { get; set; }
        public string symbol { get; set; }
        public string exchange { get; set; }
        public string asset_class { get; set; }
        public bool shortable { get; set; }
        public double qty { get; set; }
        public string side { get; set; }
        public double market_value { get; set; }
        public double avg_entry_price { get; set; }
        public double pl_dollars_total { get; set; }
        public double pl_dollars_current { get; set; }
        public double port_pl_total { get; set; }
        public double port_pl_running { get; set; }
        public double port_pl_current { get; set; }
        public double current_price { get; set; }
        public double asset_pl_total { get; set; }
        public double start_price_total { get; set; }
        public double lastPrice { get; set; }
        public double pl_dollars_today { get; set; }
        public double changeToday { get; set; }
    # endregion

    # region asset stats (from orders) (broker)
        public double amount_traded_total { get; set; }              
        public int num_trades_total { get; set; }
        public int num_buys_total { get; set; }
        public int num_sells_total { get; set; }
        public double port_perf_per_total { get; set; }
        #endregion

    #region newly added parameters
        public bool tradable { get; set; }
        public string classification { get; set; }
        public double num_trades_current { get; set; }
        public double num_buys_current { get; set; }
        public double num_sells_current { get; set; }
        public double hsl_limit_long { get; set; }
        public double tsl_limit_long { get; set; }
        public double tsl_enable_long { get; set; }
        public double hsl_limit_short { get; set; }
        public double tsl_limit_short { get; set; }
        public double tsl_enable_short { get; set; }
        public double hsl_trigger { get; set; }
        public double tsl_trigger { get; set; }
        public double allowance { get; set; }
        public double max_active_price { get; set; }
        public double min_active_price { get; set; }
        public double tradestamp { get; set; }
        public double start_price_current { get; set; }
        public double asset_max_price_current { get; set; }
        public double asset_max_price_total { get; set; }
        public double asset_min_price_current { get; set; }
        public double asset_min_price_total { get; set; }
        public double asset_pl_current { get; set; }
        public double port_pl_now { get; set; }
        public double port_perf_per_current { get; set; }
        public double port_max_pl_current { get; set; }
        public double port_max_pl_total { get; set; }
        public double port_min_pl_current { get; set; }
        public double port_min_pl_total { get; set; }
        public double amount_traded_current { get; set; }
        public double roi_current { get; set; }
        public double roi_total { get; set; }
        public string hsl_signal { get; set; }
        public string tsl_signal { get; set; }
        public string signal_day { get; set; }
        public string signal_hour { get; set; }
        public string signal_15Min { get; set; }
        public string master_signal { get; set; }
        public string strategy_champ { get; set; }
        public string strategy_day { get; set; }
        public string strategy_hour { get; set; }
        public string strategy_15Min { get; set; }
        public string diversity_signal { get; set; }
        public double betty_day_period_long { get; set; }
        public double betty_day_period_short { get; set; }
        public double betty_hour_period_long { get; set; }
        public double betty_hour_period_short { get; set; }
        public double betty_15Min_period_long { get; set; }
        public double betty_15Min_period_short { get; set; }
        public double mercy_day_slow { get; set; }
        public double mercy_day_fast { get; set; }
        public double mercy_day_smooth { get; set; }
        public double mercy_hour_slow { get; set; }
        public double mercy_hour_fast { get; set; }
        public double mercy_hour_smooth { get; set; }
        public double mercy_15Min_slow { get; set; }
        public double mercy_15Min_fast { get; set; }
        public double mercy_15Min_smooth { get; set; }
        public double polly_day_period { get; set; }
        public double polly_day_future { get; set; }
        public double polly_hour_period { get; set; }
        public double polly_hour_future { get; set; }
        public double polly_15Min_period { get; set; }
        public double polly_15Min_future { get; set; }
        public double betty_opt_pl_day { get; set; }
        public double betty_opt_pl_hour { get; set; }
        public double betty_opt_pl_15Min { get; set; }
        public double mercy_opt_pl_day { get; set; }
        public double mercy_opt_pl_hour { get; set; }
        public double mercy_opt_pl_15Min { get; set; }
        public double polly_opt_pl_day { get; set; }
        public double polly_opt_pl_hour { get; set; }
        public double polly_opt_pl_15Min { get; set; }
        public double tsl_long_opt_pl { get; set; }
        public double tsl_short_opt_pl { get; set; }
        public double betty_optimized { get; set; }
        public double betty_optcomplete_day { get; set; }
        public double betty_optcomplete_hour { get; set; }
        public double betty_optcomplete_15Min { get; set; }
        public double mercy_optimized { get; set; }
        public double mercy_optcomplete_day { get; set; }
        public double mercy_optcomplete_hour { get; set; }
        public double mercy_optcomplete_15Min { get; set; }
        public double polly_optimized { get; set; }
        public double polly_optcomplete_day { get; set; }
        public double polly_optcomplete_hour { get; set; }
        public double polly_optcomplete_15Min { get; set; }
        public double tsl_long_optcomplete { get; set; }
        public double tsl_short_optcomplete { get; set; }
        public double tsl_optimized { get; set; }
        public double min_order_qty { get; set; }
        public double inc_order_qty { get; set; }
        public double status_check { get; set; }
        public string updated { get; set; }
        public string strategy_timestamp { get; set; }
        public string created { get; set; }

    #endregion




    #region new parameters. These ones are okay.
        // asset_id
        // symbol
        // exchange
        // asset_class
        // shortable
        // side
        // qty
        // market_value
        // current_price
        // port_pl_total
        // port_pl_current
        // pl_dollars_total
        // start_price_total
        // amount_traded_total
        // num_trades_total
        // num_buys_total
        // num_sells_total
        // avg_entry_price
        // pl_dollars_current
    #endregion



    }
}
