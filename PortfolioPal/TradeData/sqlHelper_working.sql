#mysql

CREATE DATABASE IF NOT EXISTS `portfoliopal`;
USE `portfoliopal`;

DROP TABLE IF EXISTS `potentials`;
CREATE TABLE `potentials` (
    `assetID` VARCHAR(100) PRIMARY KEY,
    `score`	 INT,
    `symbol` VARCHAR(10),
    `price` FLOAT(5),
    `companyName` VARCHAR(100),
    `marketCap` FLOAT(10),
    `week52High` FLOAT(5),
    `week52Low` FLOAT(5),
    `week52Change` FLOAT(10),
    `avg10Volume` FLOAT(10),
    `avg30Volume` FLOAT(10),
    `day200MovingAvg` FLOAT(10),
    `day50MovingAvg` FLOAT(10),
    `employees` FLOAT(10),
    `ttmEPS` FLOAT(10),
    `ttmDividendRate` FLOAT(10),
    `dividendYield` FLOAT(10),
    `nextDividendDate` VARCHAR(25),
    `exDividendDate` VARCHAR(25),
    `dividendFrequency` FLOAT(10),
    `peRatio` FLOAT(10),
    `beta` FLOAT(10),
    `maxChangePercent` FLOAT(10),
    `year5ChangePercent` FLOAT(10),
    `year2ChangePercent` FLOAT(10),
    `year1ChangePercent` FLOAT(10),
    `ytdChangePercent` FLOAT(10),
    `month6ChangePercent` FLOAT(10),
    `month3ChangePercent` FLOAT(10),
    `month1ChangePercent` FLOAT(10),
    `day30ChangePercent` FLOAT(10),
    `day5ChangePercent` FLOAT(10),
    `shortable` TINYINT,
    `exchange` VARCHAR(25),
    `assetClass` VARCHAR(25),
    `updated` TIMESTAMP
);

DROP TABLE IF EXISTS `orders`;
CREATE TABLE `orders`(
	`symbol` VARCHAR(10),
	`filledQty` FLOAT(10),
	`filledPrice` Float(10),
	`side` VARCHAR(10),
	`assetClass` VARCHAR(25),
	`orderType` VARCHAR(25),
	`orderStatus` VARCHAR(25),
	`extendedHours` TINYINT,
	`filledAt` VARCHAR(25),
	`timeInForce` VARCHAR(25),
	`qty` FLOAT(10),
	`assetID` VARCHAR(100),
	`clientOrderID` VARCHAR(100),
	`orderID` VARCHAR(100) PRIMARY KEY
);

# you added a lot of stuff here be sure to fix it everywhere else!
DROP TABLE IF EXISTS `tradedassets`;
CREATE TABLE `tradedassets`(
	`asset_id` VARCHAR(50) PRIMARY KEY,
    `symbol` VARCHAR(10),
    `exchange` VARCHAR(10),
    `asset_class` VARCHAR(15),
    `classification` VARCHAR(25),
    `tradable` TINYINT,
    `shortable` TINYINT,
    `side` VARCHAR(10),
    `qty` FLOAT(10),
    `num_trades_current` FLOAT(10), 
    `num_trades_total` FLOAT(10), 
    `num_buys_current` FLOAT(10), 
    `num_sells_current` FLOAT(10), 
    `num_buys_total` FLOAT(10), 
    `num_sells_total` FLOAT(10), 
    `current_price` FLOAT(10),
    `market_value` FLOAT(10),
    `avg_entry_price` FLOAT(10),
    `hsl_limit_long` FLOAT(10),
    `tsl_limit_long` FLOAT(10),
    `tsl_enable_long` FLOAT(10),
    `hsl_limit_short` FLOAT(10),
    `tsl_limit_short` FLOAT(10),
    `tsl_enable_short` FLOAT(10),
    `hsl_trigger` FLOAT(10),
    `tsl_trigger` FLOAT(10),
    `allowance` FLOAT(10), 
    `max_active_price` FLOAT(10),
    `min_active_price` FLOAT(10),
    `tradestamp` FLOAT(25),
    `start_price_current` FLOAT(10), 
    `start_price_total` FLOAT(10), 
    `asset_max_price_current` FLOAT(10),
    `asset_max_price_total` FLOAT(10),
    `asset_min_price_current` FLOAT(10),
    `asset_min_price_total` FLOAT(10),
    `asset_pl_current` FLOAT(10),
    `asset_pl_total` FLOAT(10),
    `port_pl_now` FLOAT(10), 
    `port_pl_running` FLOAT(10), 
    `port_pl_current` FLOAT(10), 
    `port_pl_total` FLOAT(10),
    `port_perf_per_current` FLOAT(10),
    `port_perf_per_total` FLOAT(10),
    `port_max_pl_current` FLOAT(10),
    `port_max_pl_total` FLOAT(10),
    `port_min_pl_current` FLOAT(10),
    `port_min_pl_total` FLOAT(10),
    `pl_dollars_current` FLOAT(10),
    `pl_dollars_total` FLOAT(10),
    `amount_traded_current` FLOAT(10),
    `amount_traded_total` FLOAT(10),
    `roi_current` FLOAT(10),
    `roi_total` FLOAT(10),
    `hsl_signal` VARCHAR(10), 
    `tsl_signal` VARCHAR(10),
    `day_signal` VARCHAR(10), 
    `hour_signal` VARCHAR(10), 
    `15Min_signal` VARCHAR(10),
    `master_signal` VARCHAR(10),
    `strategy_champ` VARCHAR(15),
    `strategy_day` VARCHAR(15), 
    `strategy_hour` VARCHAR(15), 
    `strategy_15Min` VARCHAR(15), 
    `diversity_signal` VARCHAR(5),
    `betty_day_period_long` FLOAT(5), 
    `betty_day_period_short` FLOAT(5), 
    `betty_hour_period_long` FLOAT(5), 
    `betty_hour_period_short` FLOAT(5),
    `betty_15Min_period_long` FLOAT(5), 
    `betty_15Min_period_short` FLOAT(5), 
    `mercy_day_slow` FLOAT(5),
    `mercy_day_fast` FLOAT(5),
    `mercy_day_smooth` FLOAT(5),
    `mercy_hour_slow` FLOAT(5),
    `mercy_hour_fast` FLOAT(5),
    `mercy_hour_smooth` FLOAT(5),
    `mercy_15Min_slow` FLOAT(5),
    `mercy_15Min_fast` FLOAT(5),
    `mercy_15Min_smooth` FLOAT(5),
    `polly_day_period` FLOAT(5), 
    `polly_day_future` FLOAT(5), 
    `polly_hour_period` FLOAT(5), 
    `polly_hour_future` FLOAT(5),
    `polly_15Min_period` FLOAT(5), 
    `polly_15Min_future` FLOAT(5), 
    `betty_opt_pl_day` FLOAT(10),
    `betty_opt_pl_hour` FLOAT(10),
    `betty_opt_pl_15Min` FLOAT(10),
    `mercy_opt_pl_day` FLOAT(10),
    `mercy_opt_pl_hour` FLOAT(10),
    `mercy_opt_pl_15Min` FLOAT(10),
    `polly_opt_pl_day` FLOAT(10),
    `polly_opt_pl_hour` FLOAT(10),
    `polly_opt_pl_15Min` FLOAT(10),
    `tsl_long_opt_pl` FLOAT(10),
    `tsl_short_opt_pl` FLOAT(10),
    `betty_optimized` FLOAT(15),
    `betty_optcomplete_day` FLOAT(5),
    `betty_optcomplete_hour` FLOAT(5),
    `betty_optcomplete_15Min` FLOAT(5),
    `mercy_optimized` FLOAT(15),
    `mercy_optcomplete_day` FLOAT(5),
    `mercy_optcomplete_hour` FLOAT(5),
    `mercy_optcomplete_15Min` FLOAT(5),
    `polly_optimized` FLOAT(15),
    `polly_optcomplete_day` FLOAT(5),
    `polly_optcomplete_hour` FLOAT(5),
    `polly_optcomplete_15Min` FLOAT(5),
    `tsl_long_optcomplete` FLOAT(5),
    `tsl_short_optcomplete` FLOAT(5),
    `tsl_optimized` FLOAT(15),
    `min_order_qty` FLOAT(10),
    `inc_order_qty` FLOAT(10),
    `status_check` FLOAT(15),
    `updated` VARCHAR(50),
    `strategy_timestamp` VARCHAR(50),
    `created` VARCHAR(50)
);

DROP TABLE IF EXISTS `userselectedassets`;
CREATE TABLE `userselectedassets`(
    `symbol` VARCHAR(15), 
    `assetClass` VARCHAR(25), 
    `classification` VARCHAR(25)
);


-- VIEWS VIEWS VIEWSVIEWSVIEWSVIEWS VIEWS VIEWS
-- filtered assets (all assets from NASDAQ exchange and US_EQUITY assetClass)
CREATE OR REPLACE VIEW `allfilteredpotentials` AS SELECT potentials.* FROM potentials 
WHERE potentials.exchange = 'NASDAQ' AND potentials.assetClass = 'us_equity';

-- updated assets (all assets from filtered, which are updated)
CREATE OR REPLACE VIEW `updatedpotentials` AS SELECT allfilteredpotentials.* FROM allfilteredpotentials 
WHERE allfilteredpotentials.price > 0 AND DATE(allfilteredpotentials.updated) > 0 ORDER BY updated DESC;

-- expired assets (all assets from filtered, which are updated but past expiration)
CREATE OR REPLACE VIEW `expiredpotentials` AS SELECT updatedpotentials.* FROM updatedpotentials 
WHERE DATE(updatedpotentials.updated) > 0 AND DATE(updatedpotentials.updated) <= CURDATE() -  INTERVAL 30 DAY;

-- non updated/expired assets (all assets which are not updated or have been updated and are now expired)
CREATE OR REPLACE VIEW `notupdatedpotentials` AS SELECT allfilteredpotentials.* FROM allfilteredpotentials
WHERE allfilteredpotentials.updated IS NULL OR DATE(allfilteredpotentials.updated) <= CURDATE() - INTERVAL 30 DAY;

-- selected assets P.I ((all assets to updated (limit/max assets to update ~ 50?)
CREATE OR REPLACE VIEW `notupdateduserselected` AS SELECT notupdatedpotentials.* FROM notupdatedpotentials
INNER JOIN userselectedassets ON notupdatedpotentials.symbol = userselectedassets.symbol;

-- selected assets P.II ((all assets to updated (limit/max assets to update ~ 50?)
CREATE OR REPLACE VIEW `toupdatepotentials` AS SELECT notupdatedpotentials.* FROM notupdatedpotentials
UNION 
SELECT notupdateduserselected.* FROM notupdateduserselected
ORDER BY updated ASC
LIMIT 50;


-- suggested assets - top scoring boys.
CREATE OR REPLACE VIEW `allsuggestedassets` AS SELECT allfilteredpotentials.* FROM allfilteredpotentials
LEFT JOIN expiredpotentials ON allfilteredpotentials.assetID = expiredpotentials.assetID
WHERE allfilteredpotentials.SCORE > 0 ORDER BY allfilteredpotentials.SCORE DESC LIMIT 500;

-- suggested assets (dividends - filtered and updated dividend assets with highest scores)
-- do dividend first then we can dump the rest to the suggested trade you lazy boy :)
CREATE OR REPLACE VIEW `suggesteddividendassets` AS SELECT allsuggestedassets.* FROM allsuggestedassets
WHERE dividendYield > 0 ORDER BY allsuggestedassets.SCORE DESC LIMIT 10;

-- suggested assets (trade - filtered and updated trade assets with highest scores (ignores scores from dividends))
CREATE OR REPLACE VIEW `suggestedtradeassets` AS SELECT allsuggestedassets.* FROM allsuggestedassets
LEFT JOIN suggesteddividendassets ON allsuggestedassets.assetID = suggesteddividendassets.assetID
WHERE suggesteddividendassets.assetID IS NULL;



/* SAVE FOR LATER. maybe.

CREATE TABLE IF NOT EXISTS `portfolio` (
	`ShortDiversity` FLOAT(5),
	`StockDiversity` FLOAT(5),
	`CryptoDiversity` FLOAT(5),
	`DividendDiversity` FLOAT(5),
	`DiversityTolerance` FLOAT(5),
	`deposits` FLOAT(10),
	`withdraws` FLOAT(10),
	`accountNumber` VARCHAR(25), 
	`status` VARCHAR(25),
	`currency` VARCHAR(10),
	`cryptoStatus` VARCHAR (10),
	`buyingPower` FLOAT(10),
	`cash` FLOAT(10),
	`accrued_fees` FLOAT(10),
	`portfolioValue` FLOAT(10),
	`patternDayTrader` TINYINT,
	`createdAt` VARCHAR(50),
	`equity` FLOAT(10),
	`lastEquity` FLOAT(10),
	`longMarketValue` FLOAT(10),
	`shortMarketValue` FLOAT(10),
	`stockHoldingLimit` FLOAT(10),
	`cryptoHoldingLimit` FLOAT(10),
	`dividendHoldingLimit` FLOAT(10),
	`stockHoldingActual` FLOAT(10),
	`longHoldingActual` FLOAT(10),
	`shortHoldingActual` FLOAT(10),
	`cryptoHoldingActual` FLOAT(10),
	`dividendHoldingActual` FLOAT(10),
	`updated` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);




DROP TABLE IF EXISTS `allavoidassets`;
CREATE TABLE `allavoidassets`(
        `symbol` VARCHAR(15),
        `assetClass` VARCHAR(25),
		`classification` VARCHAR(15)
);

DROP TABLE IF EXISTS `allunoptimizedassets`;
CREATE TABLE `allunoptimizedassets`(
        `symbol` VARCHAR(15),
        `assetClass` VARCHAR(25),
		`classification` VARCHAR(15)
);



/* SAVE FOR LATER
