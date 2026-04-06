using System.ComponentModel.DataAnnotations;

namespace VolunteerHub.Contracts.Requests;

public class CheckInRequest
{
    [Required]
    public Guid EventId { get; set; }
    public string Method { get; set; } = "GPS";
    [Range(-90, 90)]
    public double? Latitude { get; set; }
    [Range(-180, 180)]
    public double? Longitude { get; set; }
}

public class CheckOutRequest
{
    [Required]
    public Guid EventId { get; set; }
    public string Method { get; set; } = "GPS";
    [Range(-90, 90)]
    public double? Latitude { get; set; }
    [Range(-180, 180)]
    public double? Longitude { get; set; }
}

public class ManualOverrideRequest
{
    [Required]
    public Guid VolunteerProfileId { get; set; }
    [Required]
    public string NewStatus { get; set; } = string.Empty;
    public DateTime? CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    [Required]
    public string Reason { get; set; } = string.Empty;
}
