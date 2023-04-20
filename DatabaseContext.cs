using Microsoft.EntityFrameworkCore;
using RoadieMovie;

namespace RoadieMovie
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

        public DbSet<Movie> Movies { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserMovieRating> UserMovieRatings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserMovieRating>()
                .HasKey(umr => new { umr.UserId, umr.MovieId });

            modelBuilder.Entity<UserMovieRating>()
                .HasOne(umr => umr.User)
                .WithMany()
                .HasForeignKey(umr => umr.UserId);

            modelBuilder.Entity<UserMovieRating>()
                .HasOne(umr => umr.Movie)
                .WithMany(m => m.UserMovieRatings)
                .HasForeignKey(umr => umr.MovieId);
        }
    }
}