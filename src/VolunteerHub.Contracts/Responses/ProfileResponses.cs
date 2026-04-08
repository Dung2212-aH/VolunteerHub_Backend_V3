namespace VolunteerHub.Contracts.Responses;

public class VolunteerProfileResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Bio { get; set; }
    public string? BloodGroup { get; set; }
    public string? Avatar { get; set; }
    public int TotalVolunteerHours { get; set; }
    public bool IsProfileComplete { get; set; }
    public List<string> Skills { get; set; } = new();
    public string? LanguagesText { get; set; }
    public string? InterestsText { get; set; }
    public List<CompletedParticipationHistoryResponse> CompletedParticipations { get; set; } = new();
}

public class CompletedParticipationHistoryResponse
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
    public double HoursEarned { get; set; }
    public Guid? CertificateId { get; set; }
    public string? CertificateNumber { get; set; }
}
