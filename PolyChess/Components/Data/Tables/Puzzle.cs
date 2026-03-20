using Microsoft.EntityFrameworkCore;

namespace PolyChess.Components.Data.Tables
{
	[PrimaryKey(nameof(Name))]
	internal class Puzzle
	{
		public string Name { get; set; } = null!;

		public string Question { get; set; } = null!;

		public string? ImageFilePath { get; set; }

		public string[] Answers { get; set; } = null!;

		public string CorrectAnswer { get; set; } = null!;
	}
}
