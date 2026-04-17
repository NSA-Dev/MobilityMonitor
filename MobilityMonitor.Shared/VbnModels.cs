using System.Text.Json.Serialization;

namespace MobilityMonitor.Shared;

// The top-level container for the VBN API response
public class GtfsResponse
{
    [JsonPropertyName("Header")]
    public FeedHeader Header { get; set; } 

    [JsonPropertyName("Entity")]
    public List<FeedEntity> Entities { get; set; } = new();
}

public class FeedHeader
{
    [JsonPropertyName("Timestamp")]
    public long Timestamp { get; set; }
}

public class FeedEntity
{
    [JsonPropertyName("Id")]
    public string Id { get; set; }

    [JsonPropertyName("TripUpdate")]
    public TripUpdate TripUpdate { get; set; }
}

public class TripUpdate
{
    [JsonPropertyName("Trip")]
    public TripDescriptor Trip { get; set; }

    [JsonPropertyName("StopTimeUpdate")]
    public List<StopTimeUpdate> StopTimeUpdates { get; set; } = new();
}

public class TripDescriptor
{
    [JsonPropertyName("TripId")]
    public string TripId { get; set; }

    [JsonPropertyName("RouteId")]
    public string RouteId { get; set; }
}

public class StopTimeUpdate
{
    [JsonPropertyName("StopId")]
    public string StopId { get; set; }

    [JsonPropertyName("Arrival")]
    public StopTimeEvent Arrival { get; set; }
}

public class StopTimeEvent
{
    [JsonPropertyName("Delay")]
    public int? Delay { get; set; } // This is in seconds
}