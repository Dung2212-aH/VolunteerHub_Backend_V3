using VolunteerHub.Domain.Common;

namespace VolunteerHub.Domain.Entities;

public class SponsorProfile : AuditableEntity, ISoftDeletable
{
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

    public SponsorProfileStatus Status { get; set; } = SponsorProfileStatus.PendingApproval;
    public string? RejectionReason { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<EventSponsor> EventSponsors { get; set; } = new List<EventSponsor>();
    public ICollection<SponsorContribution> Contributions { get; set; } = new List<SponsorContribution>();
}
