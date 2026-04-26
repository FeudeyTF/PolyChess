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

		public DbSet<Puzzle> Puzzles { get; set; }

		public DbSet<CustomTournament> Tournaments { get; set; }

		public DbSet<CustomTournamentEntry> TournamentEntries { get; set; } 

		public PolyContext(DbContextOptions<PolyContext> options) : base(options)
		{
		}

		protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
		{
			base.ConfigureConventions(configurationBuilder);
			configurationBuilder.Properties<DateTime>().HaveConversion<DateTimeConverter>();
		}

		public List<Student> GetStudentsByIdentifier(string text)
		{
			List<Student> students = [];
			var splittedName = text.Split(' ');
			if (splittedName.Length >= 3)
			{
				var surname = splittedName[0];
				var name = splittedName[1];
				var patronomic = splittedName[2];
				students.AddRange(Students.Where(s => s.Name == name && s.Surname == surname && s.Patronymic == patronomic));
			}
			else if (splittedName.Length == 2)
			{
				var surname = splittedName[0];
				var name = splittedName[1];
				students.AddRange(Students.Where(s => s.Name == name && s.Surname == surname));
			}
			else
			{
				var name = splittedName[0];
				students.AddRange(Students.Where(s => s.Name == name || s.Surname == name || (!string.IsNullOrEmpty(s.LichessId) && s.LichessId == name)));
			}

			return students;
		}
	}
}
