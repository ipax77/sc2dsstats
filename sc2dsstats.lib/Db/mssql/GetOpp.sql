CREATE FUNCTION dbo.GetOpp (@pos int)  
RETURNS int  
WITH EXECUTE AS CALLER  
AS  
BEGIN  
     DECLARE @opos int;  
	IF (@pos <= 3)
		SET @opos = @pos + 3;
	ELSE
		SET @opos = @pos - 3;

    RETURN(@opos);  
END;  
GO  

SELECT dbo.GetOpp(3);
GO