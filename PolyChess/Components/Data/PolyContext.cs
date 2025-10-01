using Microsoft.EntityFrameworkCore;
using PolyChess.Components.Data.Tables;

namespace PolyChess.Components.Data
{
    internal class PolyContext : DbContext
    {
        public DbSet<Student> Students { get; set; }

        public DbSet<Attendance> Attendances { get; set; }

        public DbSet<FaqEntry> FaqEntries { get; set; }

        public DbSet<HelpEntry> HelpEntries { get; set; }

        public DbSet<Lesson> Lessons { get; set; }

        public PolyContext(DbContextOptions<PolyContext> options) : base(options)
        {
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.Properties<DateTime>().HaveConversion<DateTimeConverter>();
        }
    }
}
