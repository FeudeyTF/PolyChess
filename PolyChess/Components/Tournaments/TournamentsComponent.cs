using PolyChess.LichessAPI.Clients;
using PolyChess.LichessAPI.Types.Arena;
using PolyChess.LichessAPI.Types.Swiss;
using PolyChess.Components.Data;
using PolyChess.Components.Data.Tables;
using PolyChess.Configuration;
using PolyChess.Core.Logging;

namespace PolyChess.Components.Tournaments
{
    /// <summary>
    /// TODO: Полностью переписать компонент
    /// </summary>
    internal class TournamentsComponent : IComponent
    {
        public readonly Division DivisionC = new(0, 1300);

        public readonly Division DivisionB = new(1301, 1800);

        public readonly Division DivisionA = new(1801, 4000);

        public List<ArenaTournamentInfo> TournamentsList { get; private set; }

        public List<SwissTournamentInfo> SwissTournamentsList { get; private set; }

        private readonly IMainConfig _mainConfig;

        private readonly ILogger _logger;

        private readonly LichessClient _lichess;

        private readonly PolyContext _context;

        public TournamentsComponent(IMainConfig config, ILogger logger, LichessClient lichess, PolyContext context)
        {
            _mainConfig = config;
            _lichess = lichess;
            _context = context;
            _logger = logger;
            TournamentsList = [];
            SwissTournamentsList = [];
        }

        public async Task StartAsync()
        {
            _logger.Write($"Собираю турниры от {_mainConfig.SemesterStartDate:g}...", LogLevel.Info);
            var tournamentsPath = Path.Combine(Environment.CurrentDirectory, "Tournaments");
            if (!Directory.Exists(tournamentsPath))
                Directory.CreateDirectory(tournamentsPath);

            var swissPath = Path.Combine(Environment.CurrentDirectory, "SwissTournaments");
            if (!Directory.Exists(swissPath))
                Directory.CreateDirectory(swissPath);

            foreach (var filePath in Directory.GetFiles(tournamentsPath))
            {
                var tournamentName = Path.GetFileName(filePath)[..^4];
                var tournament = await _lichess.GetTournament(tournamentName);
                if (tournament != null && IsSemesterTournament(tournament))
                {
                    List<SheetEntry>? tournamentSheet = default;

                    try
                    {
                        using var reader = File.OpenText(filePath);
                        tournamentSheet = await _lichess.GetTournamentSheet(reader);
                    }
                    catch
                    {
                        File.Delete(filePath);
                    }

                    if (tournamentSheet != null)
                    {
                        List<string> exclude = [.. _mainConfig.ClubTeamPlayers];
                        tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username) || e.Team != null && !_mainConfig.InstitutesTeams.Contains(e.Team))).ToList();
                        var tournamentRating = GenerateTournamentRating(tournamentSheet);
                        TournamentsList.Add(new(tournament, tournamentRating));
                    }
                }
            }

            foreach (var filePath in Directory.GetFiles(swissPath))
            {
                var tournamentName = Path.GetFileName(filePath)[..^4];
                var tournament = await _lichess.GetSwissTournament(tournamentName);
                if (tournament != null && IsSemesterTournament(tournament))
                {
                    List<SwissSheetEntry>? tournamentSheet = default;

                    try
                    {
                        using (var reader = File.OpenText(filePath))
                        {
                            tournamentSheet = await _lichess.GetSwissTournamentSheet(reader);
                        }
                    }
                    catch
                    {
                        File.Delete(filePath);
                    }

                    if (tournamentSheet != null)
                    {
                        List<string> exclude = [.. _mainConfig.ClubTeamPlayers];
                        tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username))).ToList();
                        var tournamentRating = GenerateTournamentRating(tournamentSheet);
                        SwissTournamentsList.Add(new(tournament, tournamentRating));
                    }
                }
            }

            TournamentsList = [.. from r in TournamentsList orderby r.Tournament.StartDate descending select r];
            SwissTournamentsList = [.. from r in SwissTournamentsList orderby r.Tournament.Started descending select r];
            _logger.Write($"Найдено {TournamentsList.Count} турниров и {SwissTournamentsList.Count} турниров по швейцарской системе!", LogLevel.Info);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
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
                int oneNumbers = entry.Sheet.Scores.Count(c => c == '1');
                int twoNumbers = entry.Sheet.Scores.Count(c => c == '2');
                int fourNumbers = entry.Sheet.Scores.Count(c => c == '4');
                int total = zeroNumbers + oneNumbers + twoNumbers + fourNumbers;

                if (total >= 7 && (twoNumbers + oneNumbers + fourNumbers) >= 1)
                {
                    if (inDivision)
                        totalScore = 1;
                    else
                        totalScore = 0;
                }
            }
            return totalScore;
        }

        public TournamentRating<SwissSheetEntry> GenerateTournamentRating(List<SwissSheetEntry> tournament)
            => GenerateTournamentRating(tournament, GetTournamentDivision, GetLichessName, CalculateScore);

        public TournamentRating<SheetEntry> GenerateTournamentRating(List<SheetEntry> tournament)
            => GenerateTournamentRating(tournament, GetTournamentDivision, GetLichessName, CalculateScore);

        public TournamentRating<TValue> GenerateTournamentRating<TValue>(List<TValue> tournament, Func<TValue, DivisionType> getDivision, Func<TValue, string> getLichessName, Func<TValue, bool, int> calculateScore)
        {
            Dictionary<string, Student> students = [];
            foreach (var student in _context.Students)
                if (!string.IsNullOrEmpty(student.LichessId))
                    students.Add(student.LichessId, student);

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
                if (students.TryGetValue(getLichessName(entry), out var founded))
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
            var arenaTournament = await _lichess.GetTournament(id);
            if (arenaTournament != null)
            {
                foreach (var tournament in new List<ArenaTournamentInfo>(TournamentsList))
                    if (tournament.Tournament.ID == id)
                        TournamentsList.Remove(tournament);
                await _lichess.SaveTournamentSheet(GetTournamentPath(id), id, true);
                if (IsSemesterTournament(arenaTournament))
                {
                    using var reader = File.OpenText(GetTournamentPath(arenaTournament.ID));
                    var result = new ArenaTournamentInfo(arenaTournament, GenerateTournamentRating(await _lichess.GetTournamentSheet(reader)));
                    TournamentsList.Add(result);
                    return result;
                }
            }
            return default;
        }

        public async Task<SwissTournamentInfo?> UpdateSwissTournament(string id)
        {
            var swissTournament = await _lichess.GetSwissTournament(id);
            if (swissTournament != null)
            {
                foreach (var tournament in new List<SwissTournamentInfo>(SwissTournamentsList))
                    if (tournament.Tournament.ID == id)
                        SwissTournamentsList.Remove(tournament);
                await _lichess.SaveSwissTournamentSheet(GetSwissTournamentPath(id), id);
                if (IsSemesterTournament(swissTournament))
                {
                    using var reader = File.OpenText(GetSwissTournamentPath(swissTournament.ID));
                    var result = new SwissTournamentInfo(swissTournament, GenerateTournamentRating(await _lichess.GetSwissTournamentSheet(reader)));
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
            var swissTournaments = await _lichess.GetTeamSwissTournaments(teamID);
            var arenaTournaments = await _lichess.GetTeamArenaTournaments(teamID);
            List<string> savedSwissTournaments = [];
            List<string> savedArenaTournaments = [];

            foreach (var filePath in Directory.GetFiles(GetTournamentFolder()))
                savedArenaTournaments.Add(Path.GetFileName(filePath)[..^4]);

            foreach (var filePath in Directory.GetFiles(GetSwissTournamentFolder()))
                savedSwissTournaments.Add(Path.GetFileName(filePath)[..^4]);

            swissTournaments = [.. swissTournaments.Except(swissTournaments.Where(t => savedSwissTournaments.Contains(t.ID) || !IsSemesterTournament(t)))];
            arenaTournaments = [.. arenaTournaments.Except(arenaTournaments.Where(t => savedArenaTournaments.Contains(t.ID) || !IsSemesterTournament(t)))];

            foreach (var tournament in TournamentsList)
            {
                if (File.GetLastWriteTime(GetTournamentPath(tournament.Tournament.ID)) < tournament.Tournament.StartDate && tournament.Tournament.StartDate < DateTime.Now)
                    arenaTournaments.Add(tournament.Tournament);
            }

            foreach (var tournament in SwissTournamentsList)
            {
                if (File.GetLastWriteTime(GetSwissTournamentPath(tournament.Tournament.ID)) < tournament.Tournament.Started && tournament.Tournament.Started < DateTime.Now)
                    swissTournaments.Add(tournament.Tournament);
            }

            foreach (var swissTournament in swissTournaments)
            {
                result.Add((swissTournament.ID, swissTournament.Name));
                await _lichess.SaveSwissTournamentSheet(GetSwissTournamentPath(swissTournament.ID), swissTournament.ID);
                using var reader = File.OpenText(GetSwissTournamentPath(swissTournament.ID));
                if (IsSemesterTournament(swissTournament))
                    swissTournamentsInfos.Add(new(swissTournament, GenerateTournamentRating(await _lichess.GetSwissTournamentSheet(reader))));
            }

            foreach (var arenaTournament in arenaTournaments)
            {
                result.Add((arenaTournament.ID, arenaTournament.FullName));
                await _lichess.SaveTournamentSheet(GetTournamentPath(arenaTournament.ID), arenaTournament.ID, true);
                using var reader = File.OpenText(GetTournamentPath(arenaTournament.ID));
                if (IsSemesterTournament(arenaTournament))
                    arenaTournamentsInfos.Add(new(arenaTournament, GenerateTournamentRating(await _lichess.GetTournamentSheet(reader))));
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
        private bool IsSemesterTournament(ArenaTournament tournament)
           => tournament.StartDate > _mainConfig.SemesterStartDate && !tournament.Description.Contains("баллы за этот турнир не начисляются", StringComparison.CurrentCultureIgnoreCase);

        private bool IsSemesterTournament(SwissTournament tournament)
            => tournament.Started > _mainConfig.SemesterStartDate;
    }
}
