using Microsoft.EntityFrameworkCore;
using Synthos.Core.Entities;

namespace Synthos.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AudioMetadata> AudioFiles { get; set; }
}
