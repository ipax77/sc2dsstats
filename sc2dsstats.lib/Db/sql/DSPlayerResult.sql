CREATE TABLE DSPlayerResult AS    
  SELECT
    ID,
    GAMETIME,
    WINNER,
    MAXKILLSUM,
    REALPOS,
    NAME,
    RACE,
    TEAM,
    KILLSUM,
    OPPRACE
  FROM DefaultFilterPl;