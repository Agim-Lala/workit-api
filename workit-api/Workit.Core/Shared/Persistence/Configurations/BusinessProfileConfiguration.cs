using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workit.Core.Businesses.Domain;
using Workit.Core.Users.Domain;

namespace Workit.Core.Shared.Persistence.Configurations;

public sealed class BusinessProfileConfiguration : IEntityTypeConfiguration<BusinessProfile>
{
    public void Configure(EntityTypeBuilder<BusinessProfile> builder)
    {
        builder.ToTable("business_profiles");
        builder.HasKey(businessProfile => businessProfile.Id);

        builder.Property(businessProfile => businessProfile.BusinessName)
            .HasMaxLength(BusinessProfile.MaxBusinessNameLength)
            .IsRequired();

        builder.Property(businessProfile => businessProfile.FullAddress)
            .HasMaxLength(BusinessProfile.MaxFullAddressLength)
            .IsRequired();

        builder.Property(businessProfile => businessProfile.Latitude)
            .HasPrecision(9, 6)
            .IsRequired();

        builder.Property(businessProfile => businessProfile.Longitude)
            .HasPrecision(9, 6)
            .IsRequired();

        builder.Property(businessProfile => businessProfile.Phone)
            .HasMaxLength(BusinessProfile.MaxPhoneLength);

        builder.Property(businessProfile => businessProfile.CreatedAt)
            .IsRequired();

        builder.HasIndex(businessProfile => businessProfile.UserId)
            .IsUnique();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<BusinessProfile>(businessProfile => businessProfile.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
