using DatingManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using static DatingManagementSystem.Controllers.UsersController;

namespace DatingManagementSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<CompatibilityScore> CompatibilityScores { get; set; }

        public DbSet<SkippedUser> SkippedUsers { get; set; }
    }
}