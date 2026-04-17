using System.Net.Http.Json;
using MobilityMonitor.Shared;

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
                    // We create a new DB context for every loop to keep it fresh
                    using var db = new MobilityDbContext();

                    var heavyDelays = result.Entities
                        .Where(e => e.TripUpdate?.StopTimeUpdates != null &&
                                    e.TripUpdate.StopTimeUpdates.Any(s => s.Arrival?.Delay > AnomalyDelayThreshold.TotalSeconds))
                        .ToList();

                    foreach (var entity in heavyDelays)
                    {
                        var stopUpdate = entity.TripUpdate.StopTimeUpdates.First(s => s.Arrival?.Delay > AnomalyDelayThreshold.TotalSeconds);

                        // Create the record
                        var record = new AnomalyRecord
                        {
                            TripId = entity.TripUpdate.Trip.TripId,
                            RouteId = entity.TripUpdate.Trip.RouteId,
                            DelaySeconds = stopUpdate.Arrival.Delay ?? 0,
                            StopId = stopUpdate.StopId,
                            DetectedAt = DateTime.UtcNow
                        };

                        // Add to the database
                        db.Anomalies.Add(record);
                    }

                    // Save all changes at once
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Saved {count} anomalies to the database.", heavyDelays.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing data.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}