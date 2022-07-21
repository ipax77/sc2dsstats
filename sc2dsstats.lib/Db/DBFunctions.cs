using System;

namespace sc2dsstats.lib.Db
{
    public static class DBFunctions
    {
        public static int GetOpp(int pos)
        {
            if (pos <= 3)
                return pos + 3;
            else
                return pos - 3;

            /*
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
             */
        }

        public static int GetPl(int repid, string plhash)
        {
            throw new NotImplementedException();
        }
    }
}
