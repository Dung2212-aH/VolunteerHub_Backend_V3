namespace VolunteerHub.Contracts.Responses;

public class OrganizerProfileResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string OrganizationType { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Mission { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LegalDocumentUrl { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public DateTime? VerifiedAt { get; set; }
    public string? RejectionReason { get; set; }
}
