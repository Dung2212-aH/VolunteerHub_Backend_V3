using VolunteerHub.Domain.Common;

namespace VolunteerHub.Domain.Entities;

public class SponsorContribution : AuditableEntity
{
    public Guid EventSponsorId { get; set; }
    public Guid SponsorProfileId { get; set; }

    public ContributionType Type { get; set; }
    public SponsorContributionStatus Status { get; set; } = SponsorContributionStatus.Pledged;

    /// <summary>Monetary value or estimated value of in-kind contribution (must be >= 0).</summary>
    public decimal Value { get; set; }

    public string? Description { get; set; }
    public DateTime ContributedAt { get; set; }
    public string? ReceiptReference { get; set; }
    public string? Note { get; set; }

    // Navigation
    public EventSponsor EventSponsor { get; set; } = null!;
    public SponsorProfile SponsorProfile { get; set; } = null!;
}
