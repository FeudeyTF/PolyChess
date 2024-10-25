﻿using LichessAPI.Converters;
using System.Text.Json.Serialization;

namespace LichessAPI.Types
{
    public record class User
    {
        public string ID = string.Empty;

        public string Username = string.Empty;

        [JsonPropertyName("perfs")]
        public Dictionary<string, Perfomance> Perfomance = [];

        [JsonPropertyName("createdAt")]
        [JsonConverter(typeof(LichessDateTimeConverter))]
        public DateTime RegisterDate;

        [JsonPropertyName("seenAt")]
        [JsonConverter(typeof(LichessDateTimeConverter))]
        public DateTime LastSeenDate;

        public Dictionary<string, int> Playtime  = [];

        public Profile? Profile;

        public string? Flair;

        public string URL = string.Empty;

        public Dictionary<string, int> Count  = [];

        public bool Followable;

        public bool Following;

        public bool Blocking;

        public bool FollowsYou;
    }
}