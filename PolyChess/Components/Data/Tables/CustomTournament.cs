namespace PolyChess.Components.Data.Tables
{
	internal class CustomTournament
	{
		public int Id { get; set; }

		public string Name { get; set; } = null!;

		public string Description { get; set; } = null!;

		public DateTime StartDate { get; set; }
	}
}
