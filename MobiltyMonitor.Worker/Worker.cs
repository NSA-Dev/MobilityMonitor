using System.Net.Http.Json;
using MobilityMonitor.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MobilityMonitor.Worker;

public class Worker : BackgroundService
{
    private static readonly TimeSpan AnomalyDelayThreshold = TimeSpan.FromMinutes(1);
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _httpClient;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 1. Fetch data
                var result = await _httpClient.GetFromJsonAsync<GtfsResponse>("http://gtfsr.vbn.de/gtfsr_connect.json", stoppingToken);

                if (result?.Entities != null)
                {
                    using var db = new MobilityDbContext();

                    db.Database.EnsureCreated();

                    var heavyDelays = result.Entities
                        .Where(e => e.TripUpdate?.StopTimeUpdates != null &&
                                    e.TripUpdate.StopTimeUpdates.Any(s => s.Arrival?.Delay > AnomalyDelayThreshold.TotalSeconds))
                        .ToList();

                    foreach (var entity in heavyDelays)
                    {
                        var stopUpdate = entity.TripUpdate.StopTimeUpdates.First(s => s.Arrival?.Delay > AnomalyDelayThreshold.TotalSeconds);

                        // Strip # strings 
                        var rawTripId = entity.TripUpdate.Trip.TripId;
                        var cleanTripId = rawTripId.Contains("#") ? rawTripId.Split('#').Last() : rawTripId;

                        // Check for duplicates
                        var existing = db.Anomalies.FirstOrDefault(a => a.TripId == cleanTripId);
                        if (existing != null)
                        {
                            existing.DelaySeconds = stopUpdate.Arrival.Delay ?? 0;
                            existing.DetectedAt = DateTime.UtcNow;
                        }
                        else {
                            var record = new AnomalyRecord
                            {
                                TripId = cleanTripId,
                                RouteId = entity.TripUpdate.Trip.RouteId,
                                DelaySeconds = stopUpdate.Arrival.Delay ?? 0,
                                StopId = stopUpdate.StopId,
                                DetectedAt = DateTime.UtcNow
                            };
                            db.Anomalies.Add(record);
                        }

                    }

                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Successfully synced {count} anomalies.", heavyDelays.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker sync failed. Check database path and internet connection.");
            }

            // Wait a minute before the next fetch
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}