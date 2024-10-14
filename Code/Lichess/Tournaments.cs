using Newtonsoft.Json;
using PolyChessTGBot.Lichess.Types.Arena;

namespace PolyChessTGBot.Lichess
{
    public partial class LichessApiClient
    {
        public async Task<ArenaTournament?> GetTournament(string id, int page = 1)
        {
            return await GetJsonObject<ArenaTournament>("tournament", id);
        }

        public async Task<List<SheetEntry>?> GetTournamentSheet(string id, bool full = false)
            => GetTournamentSheet(await GetFileStringAsync("tournament", id, "results?sheet=" + full));

        public async Task<List<SheetEntry>> GetTournamentSheet(Stream stream)
        {
            using var reader = new StreamReader(stream);
            return await GetTournamentSheet(reader);
        }

        public async Task<List<SheetEntry>> GetTournamentSheet(StreamReader reader)
            => GetTournamentSheet(await reader.ReadToEndAsync());

        public List<SheetEntry> GetTournamentSheet(string str)
        {
            List<SheetEntry> result = [];
            foreach (var entry in str.Split('\n'))
            {
                var deserializedObject = JsonConvert.DeserializeObject<SheetEntry>(entry);
                if (deserializedObject != null)
                    result.Add(deserializedObject);
            }
            return result;
        }

        public async Task SaveTournamentSheet(string id, string path, bool full = false)
        {
            var stream = await GetFileAsync("tournament", id, "results?sheet=" + full);
            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(fileStream);
            }
            stream.Close();
        }
    }
}
