using LichessAPI.Types.Arena;
using LichessAPI.Types.Swiss;

namespace LichessAPI.Clients
{
    public partial class LichessClient : LichessApiClient
    {
        public async Task<List<ArenaTournament>> GetTeamArenaTournaments(string teamId)
            => await GetNDJsonObject<ArenaTournament>(new StreamReader(await GetFileAsync("team", teamId, "arena")));

        public async Task<List<SwissTournament>> GetTeamSwissTournaments(string teamId)
            => await GetNDJsonObject<SwissTournament>(new StreamReader(await GetFileAsync("team", teamId, "swiss")));

        public async Task<ArenaTournament?> GetTournament(string id)
            => await GetJsonObject<ArenaTournament>(HttpMethod.Get, "tournament", id);

        public async Task<List<SheetEntry>> GetTournamentSheet(string id, bool full = false)
            => await GetNDJsonObject<SheetEntry>("tournament", id, "results?sheet=" + full);

        public async Task<List<SheetEntry>> GetTournamentSheet(StreamReader reader)
            => await GetNDJsonObject<SheetEntry>(reader);

        public async Task<SwissTournament?> GetSwissTournament(string id)
            => await GetJsonObject<SwissTournament>(HttpMethod.Get, "swiss", id);

        public async Task<List<SwissSheetEntry>> GetSwissTournamentSheet(StreamReader reader)
            => await GetNDJsonObject<SwissSheetEntry>(reader);

        public async Task<List<SwissSheetEntry>> GetSwissTournamentSheet(string id, int number = -1)
            => number == -1 ?
                await GetNDJsonObject<SwissSheetEntry>("swiss", id, "results") :
                await GetNDJsonObject<SwissSheetEntry>("swiss", id, "results?nb=" + number);

        public async Task SaveTournamentSheet(string path, string id, bool full = false)
        {
            var stream = await GetFileAsync("tournament", id, "results?sheet=" + full);
            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(fileStream);
            }
            stream.Close();
        }

        public async Task SaveSwissTournamentSheet(string path, string id, int number = -1)
        {
            var stream = number == -1 ? await GetFileAsync("swiss", id, "results") :
                                        await GetFileAsync("swiss", id, "results?nb=" + number); ;
            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(fileStream);
            }
            stream.Close();
        }
    }
}