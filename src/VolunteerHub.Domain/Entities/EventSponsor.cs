using VolunteerHub.Domain.Common;

namespace VolunteerHub.Domain.Entities;

public class EventSponsor : AuditableEntity
{
    public Guid SponsorProfileId { get; set; }
    public Guid EventId { get; set; }
    public EventSponsorStatus Status { get; set; } = EventSponsorStatus.Pending;
    public bool IsPubliclyVisible { get; set; }
    public string SponsorshipType { get; set; } = "Monetary";
    public decimal? ProposedValue { get; set; }
    public string? Note { get; set; }
    public string? RejectionReason { get; set; }

    public SponsorProfile SponsorProfile { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public ICollection<SponsorContribution> Contributions { get; set; } = new List<SponsorContribution>();
}
