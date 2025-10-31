namespace PolyChess.Components.Tournaments
{
    internal class TournamentsScore
    {
        public int TournamentWins { get; set; }

        public int TournamentParticipants { get; set; }

        public TournamentsScore(int tournamentWins, int tournamentParticipants)
        {
            TournamentWins = tournamentWins;
            TournamentParticipants = tournamentParticipants;
        }
    }
}
