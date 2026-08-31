using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Workit.Core.Shared.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ILoggerFactory? loggerFactory = null)
    : WorkitDbContextBase<AppDbContext>(options, loggerFactory);
