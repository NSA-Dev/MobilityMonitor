using System.ComponentModel.DataAnnotations; 

namespace MobilityMonitor.Shared; 
public class AnomalyRecord
{
    [Key] // DB unique ID
    public int Id { get; set; }
    public string TripId { get; set; } = string.Empty;
    public string RouteId { get; set; } = string.Empty;
    public int DelaySeconds { get; set;  }
    public DateTime DetectedAt { get; set;  } 
    public string StopId { get; set; } = string.Empty;
}

