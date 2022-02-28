using Newtonsoft.Json;
using System;

namespace Litio
{
    internal class LitioGuild
    {
        [JsonProperty("punishment_type")]
        public LitioPunishment PunishmentType { get; set; }

        [JsonProperty("activated")]
        public bool Toggled { get; set; }

        [JsonProperty("guild_id")]
        public string GuildId { get; set; }

        [JsonProperty("timeout_duration")]
        public TimeSpan TimeoutDuration { get; set; }
    }
}
