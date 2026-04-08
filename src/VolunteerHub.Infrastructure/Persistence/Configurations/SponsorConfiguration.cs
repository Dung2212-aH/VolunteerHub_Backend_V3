using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Infrastructure.Persistence.Configurations;

public class SponsorProfileConfiguration : IEntityTypeConfiguration<SponsorProfile>
{
    public void Configure(EntityTypeBuilder<SponsorProfile> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.Property(x => x.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.LogoUrl).HasMaxLength(500);
        builder.Property(x => x.WebsiteUrl).HasMaxLength(500);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.TaxCode).HasMaxLength(50);
        builder.Property(x => x.ContactPersonName).HasMaxLength(200);
        builder.Property(x => x.ContactPersonEmail).HasMaxLength(200);
        builder.Property(x => x.ContactPersonPhone).HasMaxLength(20);
        builder.Property(x => x.ContactPersonRole).HasMaxLength(100);
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.HasMany(x => x.EventSponsors).WithOne(x => x.SponsorProfile).HasForeignKey(x => x.SponsorProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Contributions).WithOne(x => x.SponsorProfile).HasForeignKey(x => x.SponsorProfileId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EventSponsorConfiguration : IEntityTypeConfiguration<EventSponsor>
{
    public void Configure(EntityTypeBuilder<EventSponsor> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.EventId);
        builder.HasIndex(x => x.SponsorProfileId);
        builder.HasIndex(x => x.Status);
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.Property(x => x.SponsorshipType).HasMaxLength(50);
        builder.Property(x => x.ProposedValue).HasPrecision(18, 2);
        builder.HasOne(x => x.Event).WithMany(x => x.EventSponsors).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Contributions).WithOne(x => x.EventSponsor).HasForeignKey(x => x.EventSponsorId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SponsorContributionConfiguration : IEntityTypeConfiguration<SponsorContribution>
{
    public void Configure(EntityTypeBuilder<SponsorContribution> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.EventSponsorId);
        builder.HasIndex(x => x.SponsorProfileId);
        builder.HasIndex(x => x.Status);
        builder.Property(x => x.Value).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.ReceiptReference).HasMaxLength(200);
        builder.Property(x => x.Note).HasMaxLength(1000);
    }
}
