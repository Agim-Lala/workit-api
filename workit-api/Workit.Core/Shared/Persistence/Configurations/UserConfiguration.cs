using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workit.Core.Users.Domain;

namespace Workit.Core.Shared.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Email).HasMaxLength(User.MaxEmailLength).IsRequired();
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Property(user => user.PasswordHash).HasMaxLength(User.MaxPasswordHashLength).IsRequired();
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(User.MaxRoleLength).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();
    }
}
