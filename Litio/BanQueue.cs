using Discord;
using Discord.Gateway;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Litio
{
    class BanQueue
    {
        static List<GuildMember> raiders = new List<GuildMember>();

        public static void Enqueue(GuildMember member)
        {
            try
            {
                if (member != null)
                {
                    if (!raiders.Select(x => x.User.Id).Contains(member.User.Id))
                    {
                        raiders.Add(member);
                    }
                }
            }
            catch
            {

            }
        }

        public static async Task StartAsync(DiscordSocketClient client)
        {
            try
            {
                await Task.Run(() =>
                {
                    while (true)
                    {
                        if (raiders.Count > 0)
                        {
                            Thread.Sleep(5);
                            try
                            {
                                GuildMember member = raiders[0];
                                switch (Utils.GetPunishmentType(raiders[0].Guild))
                                {
                                    case LitioPunishment.Ban:
                                        member.Ban("[Anti-Raid] Raid detected", 1);
                                        break;
                                    case LitioPunishment.Kick:
                                        member.Kick();
                                        break;
                                    case LitioPunishment.Timeout:
                                        client.TimeoutUser(member.Guild.Id, member.User.Id, Utils.Guilds.FirstOrDefault(x => x.GuildId == member.Guild.Id.ToString()).TimeoutDuration);
                                        break;
                                }

                                raiders.RemoveAt(0);
                            }
                            catch (DiscordHttpException ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                        else Thread.Sleep(10);
                    }
                });
            }
            catch
            {

            }
        }
    }
}
