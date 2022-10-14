using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.dbng;

public class ReplayContextFactory : IDesignTimeDbContextFactory<ReplayContext>
{
    public ReplayContext CreateDbContext(string[] args)
    {

        var optionsBuilder = new DbContextOptionsBuilder<ReplayContext>();
        optionsBuilder.UseSqlite("Data Source=/data/dsreplaystest2.db", x =>
        {
            x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            x.MigrationsAssembly("pax.dsstats.dbng");
        });

        return new ReplayContext(optionsBuilder.Options);
    }
}

//public class ReplayContextFactory : IDesignTimeDbContextFactory<ReplayContext>
//{
//    public ReplayContext CreateDbContext(string[] args)
//    {

//        var optionsBuilder = new DbContextOptionsBuilder<ReplayContext>();
//        optionsBuilder.UseSqlite("Data Source=/data/dsreplaystest.db", x =>
//        {
//            x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
//            x.MigrationsAssembly("SqliteMigrations");
//        });

//        return new ReplayContext(optionsBuilder.Options);
//    }
//}