using Microsoft.EntityFrameworkCore;
using MobilityMonitor.Shared;

namespace MobilityMonitor.Worker;

public class MobilityDbContext : DbContext
{ 
    public DbSet<AnomalyRecord> Anomalies { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // Create db file in the local AppData dir
        // N: I was thinking about clearing the file on shutdown (data is outdated) 
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "MobilityMonitor");
        Directory.CreateDirectory(folder);

        var dbPath = Path.Combine(folder, "mobility.db"); 
        options.UseSqlite($"Data Source={dbPath}"); 
    }

}
