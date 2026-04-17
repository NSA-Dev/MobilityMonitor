using Microsoft.EntityFrameworkCore;
using MobilityMonitor.Shared;

namespace MobilityMonitor.Worker;

public class MobilityDbContext : DbContext
{ 
    public DbSet<AnomalyRecord> Anomalies { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=mobility.db"); 
    }

}
