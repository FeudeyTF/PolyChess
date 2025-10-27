using System.Text.Json.Serialization;

namespace PolyChess.LichessAPI.Types
{
    public class Team
    {
        public string ID = string.Empty;

        public string Name = string.Empty;

        public string Description = string.Empty;

        public bool Open;

        public LightUser Leader = new();

        [JsonPropertyName("nbMembers")]
        public int MembersCount;

        public string Flair = string.Empty;

        public List<LightUser> Leaders = [];

        public bool Joined;

        public bool Requested;
    }
}