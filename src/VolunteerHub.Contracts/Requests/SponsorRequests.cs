using System.ComponentModel.DataAnnotations;

namespace VolunteerHub.Contracts.Requests;

public class CreateSponsorProfileRequest
{
    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? LogoUrl { get; set; }
    [MaxLength(500)]
    public string? WebsiteUrl { get; set; }
    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;
    [MaxLength(50)]
    public string? TaxCode { get; set; }
    [MaxLength(200)]
    public string ContactPersonName { get; set; } = string.Empty;
    [EmailAddress, MaxLength(200)]
    public string ContactPersonEmail { get; set; } = string.Empty;
    [MaxLength(20)]
    public string ContactPersonPhone { get; set; } = string.Empty;
    [MaxLength(100)]
    public string? ContactPersonRole { get; set; }
}

public class UpdateSponsorProfileRequest : CreateSponsorProfileRequest
{
}

public class SponsorEventRequest
{
    [Required]
    public Guid EventId { get; set; }
    [MaxLength(50)]
    public string SponsorshipType { get; set; } = "Monetary";
    [Range(0, double.MaxValue)]
    public decimal? ProposedValue { get; set; }
    [MaxLength(1000)]
    public string? Note { get; set; }
}

public class ApproveRejectEventSponsorRequest
{
    [Required]
    public bool Approve { get; set; }
    [MaxLength(1000)]
    public string? Reason { get; set; }
    public bool IsPubliclyVisible { get; set; } = true;
}

public class ApproveSponsorProfileRequest
{
    [Required]
    public bool Approve { get; set; }
    [MaxLength(1000)]
    public string? Reason { get; set; }
}

public class RecordContributionRequest : IValidatableObject
{
    [Required]
    public Guid EventSponsorId { get; set; }
    [Required]
    public string Type { get; set; } = "Monetary";
    [Range(0, double.MaxValue)]
    public decimal Value { get; set; }
    [MaxLength(2000)]
    public string? Description { get; set; }
    public DateTime? ContributedAt { get; set; }
    [MaxLength(200)]
    public string? ReceiptReference { get; set; }
    [MaxLength(1000)]
    public string? Note { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Value < 0)
            yield return new ValidationResult("Value must be non-negative.", new[] { nameof(Value) });
    }
}
