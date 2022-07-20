CREATE DATABASE IF NOT EXISTS `portfoliopal`;
-- USE `portfoliopal`;

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
	`filledAt` VARCHAR(50),
	`timeInForce` VARCHAR(25),
	`qty` FLOAT(10),
	`assetID` VARCHAR(100),
	`clientOrderID` VARCHAR(100),
	`orderID` VARCHAR(100) PRIMARY KEY
);

DROP TABLE IF EXISTS `tradeassets`;
CREATE TABLE `tradeassets`(
	`assetID` VARCHAR(100) PRIMARY KEY,
	`symbol` VARCHAR(10),
	`exchange` VARCHAR(25),
	`assetClass` VARCHAR(25),
	`shortable` TINYINT,
	`qty` FLOAT(10),
	`side` VARCHAR(25),
	`marketValue` FLOAT(10),
	`costBasis` FLOAT(10),
	`plDollarsTotal` FLOAT(10),
	`plPercentTotal` FLOAT(10),
	`plDollarsToday` FLOAT(10),
	`plPercentToday` FLOAT(10),
	`price` FLOAT(10),
	`lastPrice` FLOAT(10),
	`changeToday` FLOAT(10),   
	`TotalTraded` FLOAT(10),
	`NumberTrades` FLOAT(10),
	`NumberBuys` FLOAT(10),
	`NumberSells` FLOAT(10),
	`TotalPLP` FLOAT(10),
	`TotalPLD` FLOAT(10),
	`updated` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
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


DROP TABLE IF EXISTS `tradeassets`;
CREATE TABLE `tradeassets`(
	`assetID` VARCHAR(100) PRIMARY KEY,
	`symbol` VARCHAR(10),
	`exchange` VARCHAR(25),
	`assetClass` VARCHAR(25),
	`shortable` TINYINT,
	`qty` FLOAT(10),
	`side` VARCHAR(25),
	`marketValue` FLOAT(10),
	`costBasis` FLOAT(10),
	`plDollarsTotal` FLOAT(10),
	`plPercentTotal` FLOAT(10),
	`plDollarsToday` FLOAT(10),
	`plPercentToday` FLOAT(10),
	`price` FLOAT(10),
	`lastPrice` FLOAT(10),
	`changeToday` FLOAT(10),   
	`TotalTraded` FLOAT(10),
	`NumberTrades` FLOAT(10),
	`NumberBuys` FLOAT(10),
	`NumberSells` FLOAT(10),
	`TotalPLP` FLOAT(10),
	`TotalPLD` FLOAT(10),
	`updated` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);




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

