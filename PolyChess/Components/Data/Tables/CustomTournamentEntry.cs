namespace PolyChess.Components.Data.Tables
{
	internal class CustomTournamentEntry
	{
		public int Id { get; set; }

		public CustomTournament Tournament { get; set; } = null!;
	
		public Student Student { get; set; } = null!;

		public int Score { get; set; }
	}
}

