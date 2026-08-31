using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Workit.Core.Shared.Persistence;

public sealed class ReadAppDbContext(
    DbContextOptions<ReadAppDbContext> options,
    ILoggerFactory? loggerFactory = null)
    : WorkitDbContextBase<ReadAppDbContext>(options, loggerFactory);
