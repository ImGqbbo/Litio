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

        public static void Start()
        {
            try
            {
                Task.Run(() =>
                {
                    while (true)
                    {
                        if (raiders.Count > 0)
                        {
                            Thread.Sleep(5);
                            try
                            {
                                /*switch (Utils.GetPunishmentType(raiders[0].Guild))
                                {
                                    case LitioPunishment.Ban:
                                        raiders[0].Ban("[Anti-Raid] Raid detected", 1);
                                        break;
                                    case LitioPunishment.Kick:
                                        raiders[0].Kick();
                                        break;
                                    case LitioPunishment.Timeout:
                                        client.TimeoutUser(raiders[0].Guild.Id, raiders[0].User.Id, Utils.Guilds.FirstOrDefault(x => x.GuildId == raiders[0].Guild.Id.ToString()).TimeoutDuration);
                                        break;
                                }*/
                                raiders[0].Ban("[Litio] Raid detected", 1);

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
