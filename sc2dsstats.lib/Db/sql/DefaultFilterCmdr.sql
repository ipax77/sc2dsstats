CREATE 
    ALGORITHM = UNDEFINED 
    DEFINER = `root`@`%` 
    SQL SECURITY DEFINER
VIEW `DefaultFilterCmdr` AS
    SELECT 
        `d`.`ID` AS `ID`,
        `d`.`REPLAY` AS `REPLAY`,
        `d`.`GAMETIME` AS `GAMETIME`,
        `d`.`WINNER` AS `WINNER`,
        `d`.`DURATION` AS `DURATION`,
        `d`.`MINKILLSUM` AS `MINKILLSUM`,
        `d`.`MAXKILLSUM` AS `MAXKILLSUM`,
        `d`.`MINARMY` AS `MINARMY`,
        `d`.`MININCOME` AS `MININCOME`,
        `d`.`MAXLEAVER` AS `MAXLEAVER`,
        `d`.`PLAYERCOUNT` AS `PLAYERCOUNT`,
        `d`.`REPORTED` AS `REPORTED`,
        `d`.`ISBRAWL` AS `ISBRAWL`,
        `d`.`GAMEMODE` AS `GAMEMODE`,
        `d`.`VERSION` AS `VERSION`,
        `d`.`HASH` AS `HASH`
    FROM
        `DSReplays` `d`
    WHERE
        ((`d`.`DURATION` > '00:05:00')
            AND (`d`.`MAXLEAVER` < 2000)
            AND (`d`.`MINARMY` > 1500)
            AND (`d`.`MININCOME` > 1500)
            AND (`d`.`MINKILLSUM` > 1500)
            AND (`d`.`PLAYERCOUNT` = 6)
            AND (`d`.`GAMEMODE` IN ('GameModeCommanders' , 'GameModeCommandersHeroic')))