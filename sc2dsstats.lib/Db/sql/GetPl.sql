CREATE DEFINER=`root`@`localhost` FUNCTION `GetPl`(PLID INT, DATASET char(64))
RETURNS int(11) DETERMINISTIC
BEGIN
DECLARE plpos INT;

IF DATASET = '' THEN
	plpos = select REALPOS from DSPlayers where ID=PLID and NAME='player';
ELSE
	plpos = select dup.Pos from DSPlayser as pl
    left join PLDuplicates as dup on dup.ID = pl.PLDuplicateID
    where dup.Hash = DATASET
END IF;

RETURN (plpos);
END