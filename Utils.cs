using Discord;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;

namespace Litio
{
    class Utils
    {
        public static Color Error = Color.FromArgb(235, 48, 35);
        public static Color Success = Color.FromArgb(35, 235, 98);
        public static List<LitioGuild> Guilds = new List<LitioGuild>();

        public static LitioPunishment GetPunishmentType(MinimalGuild guild)
        {
            try
            {
                return Guilds.FirstOrDefault(x => x.GuildId == guild.Id.ToString()).PunishmentType;
            }
            catch
            {
                return LitioPunishment.Ban;
            }
        }

        public static bool GetAntiRaidToggle(MinimalGuild guild)
        {
            try
            {
                return Guilds.FirstOrDefault(x => x.GuildId == guild.Id.ToString()).Toggled;
            }
            catch
            {
                return false;
            }
        }

        public static List<LitioGuild> GetGuilds()
        {
            try
            {
                if (!File.Exists("Database.txt"))
                    File.WriteAllText("Database.txt", JsonConvert.SerializeObject(Guilds));

                return JsonConvert.DeserializeObject<List<LitioGuild>>(File.ReadAllText("Database.txt"));
            }
            catch
            {
                return new List<LitioGuild>();
            }
        }
    }
}
