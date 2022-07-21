CREATE DEFINER=`root`@`localhost` FUNCTION `GetOpp`(pos INT) RETURNS int(11)
    DETERMINISTIC
BEGIN
DECLARE opos INT;

IF pos <= 3 THEN
SET opos = pos + 3;
ELSE
SET opos = pos - 3;
END IF;

RETURN (opos);
END