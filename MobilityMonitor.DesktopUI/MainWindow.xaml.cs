using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using MobilityMonitor.Shared;
using MobilityMonitor.Worker;

namespace MobilityMonitor.DesktopUI
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private static readonly TimeSpan AnomalyThreshold = TimeSpan.FromMinutes(10);

        public MainWindow()
        {
            InitializeComponent();
            _ = RunMonitoringLoop();
        }

        private async Task RunMonitoringLoop()
        {
            while (true)
            {
                try
                {
                    var result = await _httpClient.GetFromJsonAsync<GtfsResponse>("http://gtfsr.vbn.de/gtfsr_connect.json");

                    if (result?.Entities != null)
                    {
                        using var db = new MobilityDbContext();

                        var heavyDelays = result.Entities
                            .Where(e => e.TripUpdate?.StopTimeUpdates != null &&
                                        e.TripUpdate.StopTimeUpdates.Any(s => s.Arrival?.Delay > AnomalyThreshold.TotalSeconds))
                            .ToList();

                        foreach (var entity in heavyDelays)
                        {
                            var stopUpdate = entity.TripUpdate.StopTimeUpdates.First(s => s.Arrival?.Delay > AnomalyThreshold.TotalSeconds);

                            db.Anomalies.Add(new AnomalyRecord
                            {
                                TripId = entity.TripUpdate.Trip.TripId,
                                RouteId = entity.TripUpdate.Trip.RouteId,
                                DelaySeconds = stopUpdate.Arrival.Delay ?? 0,
                                StopId = stopUpdate.StopId,
                                DetectedAt = DateTime.UtcNow
                            });
                        }

                        await db.SaveChangesAsync();
                        await Dispatcher.InvokeAsync(() => RefreshTable());
                    }
                }
                catch (Exception)
                {
                    await Dispatcher.InvokeAsync(() => StatusText.Text = "Sync Error. Retrying...");
                }

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private async void RefreshTable()
        {
            try
            {
                using var db = new MobilityDbContext();
                var data = await db.Anomalies.OrderByDescending(a => a.DetectedAt).Take(100).ToListAsync();
                AnomalyGrid.ItemsSource = data;
                StatusText.Text = $"Last Sync: {DateTime.Now:HH:mm:ss} | {data.Count} anomalies found.";
            }
            catch { /* Handle DB locks if necessary */ }
        }
    }
}