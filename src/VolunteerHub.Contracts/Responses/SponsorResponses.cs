namespace VolunteerHub.Contracts.Responses;

public class SponsorProfileResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string ContactPersonName { get; set; } = string.Empty;
    public string ContactPersonEmail { get; set; } = string.Empty;
    public string ContactPersonPhone { get; set; } = string.Empty;
    public string? ContactPersonRole { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
}

public class EventSponsorResponse
{
    public Guid Id { get; set; }
    public Guid SponsorProfileId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsPubliclyVisible { get; set; }
    public string SponsorshipType { get; set; } = string.Empty;
    public decimal? ProposedValue { get; set; }
    public string? Note { get; set; }
    public string? RejectionReason { get; set; }
}

public class SponsorContributionResponse
{
    public Guid Id { get; set; }
    public Guid EventSponsorId { get; set; }
    public Guid SponsorProfileId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string? Description { get; set; }
    public DateTime ContributedAt { get; set; }
    public string? ReceiptReference { get; set; }
    public string? Note { get; set; }
}

public class PublicEventSponsorResponse
{
    public string CompanyName { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string SponsorshipType { get; set; } = string.Empty;
}
