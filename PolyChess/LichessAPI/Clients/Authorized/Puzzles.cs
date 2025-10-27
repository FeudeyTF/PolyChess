using PolyChess.LichessAPI.Types.Puzzles;

namespace PolyChess.LichessAPI.Clients.Authorized
{
    public partial class LichessAuthorizedClient : LichessApiClient
    {
        public async Task<List<PuzzleActivity>> GetPuzzleActivity(int? max = default, DateTime beforeDate = default)
        {
            string queryParams = Utils.GetQueryParametersString(("max", max), ("before", beforeDate));
            return Utils.ParseNDJsonObject<PuzzleActivity>(await GetQAuthGetRequestContent("puzzle", "activity" + queryParams) ?? "");
        }

        public async Task<PuzzleDashboard?> GetPuzzleDashboard(int days)
        {
            return await GetAuthJsonObject<PuzzleDashboard>(HttpMethod.Get, "puzzle", "dashboard", days.ToString());
        }
    }
}
