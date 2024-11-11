using LichessAPI.Types.Arena;
using LichessAPI.Types.Swiss;
using PolyChessTGBot.Database;
using PolyChessTGBot.Logs;

namespace PolyChessTGBot.Managers.Tournaments
{
    public class TournamentsManager
    {
        public readonly Division DivisionC = new(0, 1300);

        public readonly Division DivisionB = new(1301, 1800);

        public readonly Division DivisionA = new(1801, 2100);

        public List<ArenaTournamentInfo> TournamentsList { get; private set; }

        public List<SwissTournamentInfo> SwissTournamentsList { get; private set; }

        public TournamentsManager()
        {
            TournamentsList = [];
            SwissTournamentsList = [];
        }

        public async Task LoadTournaments()
        {
            Program.Logger.Write($"Собираем турниры от {Program.SemesterStartDate:g}...", LogType.Info);
            var tournamentsPath = Path.Combine(Environment.CurrentDirectory, "Tournaments");
            if (!Directory.Exists(tournamentsPath))
                Directory.CreateDirectory(tournamentsPath);

            var swissPath = Path.Combine(Environment.CurrentDirectory, "SwissTournaments");
            if (!Directory.Exists(swissPath))
                Directory.CreateDirectory(swissPath);

            foreach (var filePath in Directory.GetFiles(tournamentsPath))
            {
                var tournamentName = Path.GetFileName(filePath)[..^4];
                var tournament = await Program.Lichess.GetTournament(tournamentName);
                if (tournament != null && tournament.IsSemesterTournament())
                {
                    var tournamentSheet = await Program.Lichess.GetTournamentSheet(File.OpenText(filePath));

                    if (tournamentSheet != null)
                    {
                        List<string> exclude = new(Program.MainConfig.TopPlayers);
                        tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username) || e.Team != null && !Program.MainConfig.InstitutesTeamsIDs.Contains(e.Team))).ToList();
                        var tournamentRating = GenerateTournamentRating(tournamentSheet);
                        TournamentsList.Add(new(tournament, tournamentRating));
                    }
                }
            }

            foreach (var filePath in Directory.GetFiles(swissPath))
            {
                var tournamentName = Path.GetFileName(filePath)[..^4];
                var tournament = await Program.Lichess.GetSwissTournament(tournamentName);
                if (tournament != null && tournament.IsSemesterTournament())
                {
                    var tournamentSheet = await Program.Lichess.GetSwissTournamentSheet(File.OpenText(filePath));

                    if (tournamentSheet != null)
                    {
                        List<string> exclude = new(Program.MainConfig.TopPlayers);
                        tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username))).ToList();
                        var tournamentRating = GenerateTournamentRating(tournamentSheet);
                        SwissTournamentsList.Add(new(tournament, tournamentRating));
                    }
                }
            }

            TournamentsList = [.. from r in TournamentsList orderby r.Tournament.StartDate descending select r];
            SwissTournamentsList = [.. from r in SwissTournamentsList orderby r.Tournament.Started descending select r];
            Program.Logger.Write($"Найдено {TournamentsList.Count} турниров и {SwissTournamentsList.Count} турниров по швейцарской системе!", LogType.Info);
        }

        public DivisionType GetTournamentDivision(SwissSheetEntry entry)
            => GetTournamentDivision(entry.Rating);

        public DivisionType GetTournamentDivision(SheetEntry entry)
            => GetTournamentDivision(entry.Rating);

        public DivisionType GetTournamentDivision(int rating)
        {
            if (DivisionC.InDivision(rating))
                return DivisionType.C;
            else if (DivisionB.InDivision(rating))
                return DivisionType.B;
            else if (DivisionA.InDivision(rating))
                return DivisionType.A;
            return DivisionType.None;
        }

        public static string GetLichessName(SwissSheetEntry entry)
            => entry.Username;

        public static string GetLichessName(SheetEntry entry)
        => entry.Username;

        public static int CalculateScore(SwissSheetEntry entry, bool inDivision)
        {
            int totalScore = -1;
            if (!entry.Absent)
            {
                totalScore = 0;
                if (inDivision)
                    totalScore = 1;
            }
            return totalScore;
        }

        public static int CalculateScore(SheetEntry entry, bool inDivision)
        {
            int totalScore = -1;
            if (entry.Sheet != null)
            {
                int zeroNumbers = entry.Sheet.Scores.Count(c => c == '0');
                int twoNumbers = entry.Sheet.Scores.Count(c => c == '2');
                int fourNumbers = entry.Sheet.Scores.Count(c => c == '4');
                int total = zeroNumbers + twoNumbers + fourNumbers;

                if (inDivision)
                    totalScore = 1;
                else if (total >= 7 && twoNumbers >= 1)
                    totalScore = 0;
            }
            return totalScore;
        }

        public TournamentRating<SwissSheetEntry> GenerateTournamentRating(List<SwissSheetEntry> tournament)
            => GenerateTournamentRating(tournament, GetTournamentDivision, GetLichessName, CalculateScore);

        public TournamentRating<SheetEntry> GenerateTournamentRating(List<SheetEntry> tournament)
            => GenerateTournamentRating(tournament, GetTournamentDivision, GetLichessName, CalculateScore);

        public static TournamentRating<TValue> GenerateTournamentRating<TValue>(List<TValue> tournament, Func<TValue, DivisionType> getDivision, Func<TValue, string> getLichessName, Func<TValue, bool, int> calculateScore)
        {
            Dictionary<string, User> users = [];
            foreach (var user in Program.Data.Users)
                if (!string.IsNullOrEmpty(user.LichessName))
                    users.Add(user.LichessName, user);

            Dictionary<DivisionType, List<TValue>> playersInDivision = new()
                {
                    { DivisionType.A, [] },
                    { DivisionType.B, [] },
                    { DivisionType.C, [] }
                };

            foreach (var entry in tournament)
            {
                var division = getDivision(entry);
                if (division != DivisionType.None && playersInDivision[division].Count < 3)
                    playersInDivision[division].Add(entry);
            }

            List<TournamentUser<TValue>> tournamentUsers = [];

            foreach (var entry in tournament)
            {
                bool inDivision = false;
                for (int i = 0; i < playersInDivision.Count; i++)
                    if (playersInDivision[(DivisionType)i].Contains(entry))
                    {
                        inDivision = true;
                        break;
                    }
                var score = calculateScore(entry, inDivision);
                if (users.TryGetValue(getLichessName(entry), out User? founded))
                    tournamentUsers.Add(new(founded, score, entry));
                else
                    tournamentUsers.Add(new(null, score, entry));
            }

            return new(playersInDivision, tournamentUsers);
        }

        public static string GetTournamentFolder()
            => Path.Combine(Environment.CurrentDirectory, "Tournaments");

        public static string GetSwissTournamentFolder()
            => Path.Combine(Environment.CurrentDirectory, "SwissTournaments");

        public static string GetTournamentPath(string id)
            => Path.Combine(GetTournamentFolder(), id + ".txt");

        public static string GetSwissTournamentPath(string id)
            => Path.Combine(GetSwissTournamentFolder(), id + ".txt");

        public async Task<ArenaTournamentInfo?> UpdateTournament(string id)
        {
            var arenaTournament = await Program.Lichess.GetTournament(id);
            if (arenaTournament != null)
            {
                foreach (var tournament in new List<ArenaTournamentInfo>(TournamentsList))
                    if (tournament.Tournament.ID == id)
                        TournamentsList.Remove(tournament);
                await Program.Lichess.SaveTournamentSheet(GetTournamentPath(id), id);
                if (arenaTournament.IsSemesterTournament())
                {
                    using var reader = File.OpenText(GetTournamentPath(arenaTournament.ID));
                    var result = new ArenaTournamentInfo(arenaTournament, GenerateTournamentRating(await Program.Lichess.GetTournamentSheet(reader)));
                    TournamentsList.Add(result);
                    return result;
                }
            }
            return default;
        }

        public async Task<SwissTournamentInfo?> UpdateSwissTournament(string id)
        {
            var swissTournament = await Program.Lichess.GetSwissTournament(id);
            if (swissTournament != null)
            {
                foreach (var tournament in new List<SwissTournamentInfo>(SwissTournamentsList))
                    if (tournament.Tournament.ID == id)
                        SwissTournamentsList.Remove(tournament);
                await Program.Lichess.SaveSwissTournamentSheet(GetSwissTournamentPath(id), id);
                if (swissTournament.IsSemesterTournament())
                {
                    using var reader = File.OpenText(GetSwissTournamentPath(swissTournament.ID));
                    var result = new SwissTournamentInfo(swissTournament, GenerateTournamentRating(await Program.Lichess.GetSwissTournamentSheet(reader)));
                    SwissTournamentsList.Add(result);
                    return result;
                }
            }
            return default;
        }

        public async Task<List<(string id, string name)>> UpdateTournaments(string teamID)
        {
            List<(string id, string name)> result = [];
            List<ArenaTournamentInfo> arenaTournamentsInfos = [];
            List<SwissTournamentInfo> swissTournamentsInfos = [];
            var swissTournaments = await Program.Lichess.GetTeamSwissTournaments(teamID);
            var arenaTournaments = await Program.Lichess.GetTeamArenaTournaments(teamID);
            List<string> savedSwissTournaments = [];
            List<string> savedArenaTournaments = [];

            foreach (var filePath in Directory.GetFiles(GetTournamentFolder()))
                savedArenaTournaments.Add(Path.GetFileName(filePath)[..^4]);

            foreach (var filePath in Directory.GetFiles(GetSwissTournamentFolder()))
                savedSwissTournaments.Add(Path.GetFileName(filePath)[..^4]);

            swissTournaments = [.. swissTournaments.Except(swissTournaments.Where(t => savedSwissTournaments.Contains(t.ID)))];
            arenaTournaments = [.. arenaTournaments.Except(arenaTournaments.Where(t => savedArenaTournaments.Contains(t.ID)))];

            foreach (var tournament in TournamentsList)
            {
                if (File.GetLastWriteTime(GetTournamentPath(tournament.Tournament.ID)) < tournament.Tournament.StartDate)
                    arenaTournaments.Add(tournament.Tournament);
            }

            foreach (var tournament in SwissTournamentsList)
            {
                if (File.GetLastWriteTime(GetSwissTournamentPath(tournament.Tournament.ID)) < tournament.Tournament.Started)
                    swissTournaments.Add(tournament.Tournament);
            }

            foreach (var swissTournament in swissTournaments)
            {
                result.Add((swissTournament.ID, swissTournament.Name));
                await Program.Lichess.SaveSwissTournamentSheet(GetSwissTournamentPath(swissTournament.ID), swissTournament.ID);
                using var reader = File.OpenText(GetSwissTournamentPath(swissTournament.ID));
                if (swissTournament.IsSemesterTournament())
                    swissTournamentsInfos.Add(new(swissTournament, GenerateTournamentRating(await Program.Lichess.GetSwissTournamentSheet(reader))));
            }

            foreach (var arenaTournament in arenaTournaments)
            {
                result.Add((arenaTournament.ID, arenaTournament.FullName));
                await Program.Lichess.SaveTournamentSheet(GetTournamentPath(arenaTournament.ID), arenaTournament.ID, true);
                using var reader = File.OpenText(GetTournamentPath(arenaTournament.ID));
                if (arenaTournament.IsSemesterTournament())
                    arenaTournamentsInfos.Add(new(arenaTournament, GenerateTournamentRating(await Program.Lichess.GetTournamentSheet(reader))));
            }

            foreach (var tournament in new List<ArenaTournamentInfo>(TournamentsList))
                foreach (var tournamentInfo in arenaTournamentsInfos)
                    if (tournament.Tournament.ID == tournamentInfo.Tournament.ID)
                        TournamentsList.Remove(tournament);

            foreach (var tournament in new List<SwissTournamentInfo>(SwissTournamentsList))
                foreach (var tournamentInfo in swissTournamentsInfos)
                    if (tournament.Tournament.ID == tournamentInfo.Tournament.ID)
                        SwissTournamentsList.Remove(tournament);

            TournamentsList = [.. TournamentsList, .. arenaTournamentsInfos];
            SwissTournamentsList = [.. SwissTournamentsList, .. swissTournamentsInfos];
            return result;
        }
    }
}
