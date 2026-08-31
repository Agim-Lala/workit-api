using Workit.Core.Users.Domain;

namespace Workit.Core.Users.Shared;

public sealed record UserDto(Guid Id, string Email, UserRole Role);
