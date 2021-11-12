CREATE OR REPLACE
    ALGORITHM = UNDEFINED 
    DEFINER = `root`@`%` 
    SQL SECURITY DEFINER
VIEW `DefaultFilterPl` AS
    SELECT 
        `d`.`ID` AS `ID`,
        `d`.`GAMETIME` AS `GAMETIME`,
        `d`.`WINNER` AS `WINNER`,
        `d`.`MAXKILLSUM` AS `MAXKILLSUM`,
        `d0`.`REALPOS` AS `REALPOS`,
        `d0`.`NAME` AS `NAME`,
        `d0`.`RACE` AS `RACE`,
        `d0`.`TEAM` AS `TEAM`,
        `d0`.`KILLSUM` AS `KILLSUM`,
        `d1`.`RACE` AS `OPPRACE`
    FROM
        (`DSReplays` `d`
        INNER JOIN `DSPlayers` `d0` ON ((`d`.`ID` = `d0`.`DSReplayID`)))
        INNER JOIN `DSPlayers` AS `d1` ON `d`.`ID` = `d1`.`DSReplayID` AND `d1`.`REALPOS` = GetOpp(`d0`.`REALPOS`)
    WHERE
        ((`d`.`GAMETIME` > '2018-01-01')
            AND (`d`.`DURATION` > '00:05:00')
            AND (`d`.`MAXLEAVER` < 2000)
            AND (`d`.`MINARMY` > 1500)
            AND (`d`.`MININCOME` > 1500)
            AND (`d`.`MINKILLSUM` > 1500)
            AND (`d`.`PLAYERCOUNT` = 6)
            AND (`d`.`GAMEMODE` IN ('GameModeCommanders' , 'GameModeCommandersHeroic')))