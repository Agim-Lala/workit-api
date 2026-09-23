using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workit.Core.Hiring.Domain;
using Workit.Core.Reviews.Domain;

namespace Workit.Core.Shared.Persistence.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews", table =>
        {
            table.HasCheckConstraint("CK_reviews_rating_range", "\"Rating\" BETWEEN 1 AND 5");
        });
        builder.HasKey(review => review.Id);

        builder.Property(review => review.ReviewerRole)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(review => review.Rating)
            .IsRequired();

        builder.Property(review => review.Comment)
            .HasMaxLength(Review.MaxCommentLength);

        builder.Property(review => review.CreatedAt)
            .IsRequired();

        builder.HasIndex(review => new { review.JobAssignmentId, review.ReviewerRole })
            .IsUnique();

        builder.HasOne<JobAssignment>()
            .WithMany()
            .HasForeignKey(review => review.JobAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
