
using Microsoft.EntityFrameworkCore;
using sc2dsstats.db;

public class localContext : sc2dsstatsContext
{
    internal localContext(DbContextOptions options)
    : base(options)
    {

    }

    public localContext(DbContextOptions<localContext> options)
    : base(options)
    {
    }
}