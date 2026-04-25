namespace PolyChess.Components.Data.Tables
{
	internal class CustomTournamentAttendance
	{
		public int Id { get; set; }

		public CustomTournament Tournament { get; set; } = null!;

		public Student Student { get; set; } = null!;

		public CustomTournamentParticipantCategory Category { get; set; }

		public bool IsMarkedAtStart { get; set; }

		public bool IsMarkedAtFinish { get; set; }

		public bool IsScoreApplied { get; set; }
	}
}
