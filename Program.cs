using Discord;
using Discord.Gateway;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Litio
{
    class Program
    {
        public static Dictionary<ulong, List<GuildMember>> Raiders = new Dictionary<ulong, List<GuildMember>>();
        public static Dictionary<ulong, List<DiscordThread>> Threads = new Dictionary<ulong, List<DiscordThread>>();
        public static Dictionary<ulong, List<DiscordMessage>> Messages = new Dictionary<ulong, List<DiscordMessage>>();
        private static DiscordSocketClient client;

        public static Random Random = new Random();
        public static readonly int MaxJoins = 7;
        public static readonly int MaxMessages = 14;
        public static readonly int MaxThreads = 5;
        public static readonly TimeSpan MessageExpiration = new TimeSpan(0, 0, 2);
        public static readonly TimeSpan ThreadExpiration = new TimeSpan(0, 0, 2);

        static void Main(string[] args)
        {
            Console.WriteLine(" [Litio] Starting and setting up, please wait.");
            Console.Title = "Litio Anti-Raid";

            client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                Intents = GatewayIntentBundles.Guilds | GatewayIntentBundles.GuildMessages | GatewayIntentBundles.GuildAdministration | DiscordGatewayIntent.GuildMembers,
                RetryOnRateLimit = true,
                ApiVersion = 9
            });
            BanQueue.Start();

            client.OnLoggedIn += Client_OnLoggedIn;
            client.OnJoinedGuild += Client_OnJoinedGuild;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnUserJoinedGuild += Client_OnUserJoinedGuild;
            client.OnThreadCreated += Client_OnThreadCreated;

            Console.Write(" [Litio] Insert your bot token: ");
            string Token = Console.ReadLine();
            client.Login(Token);

            Thread.Sleep(-1);
        }

        private static void Client_OnThreadCreated(DiscordSocketClient client, ThreadEventArgs args)
        {
            try
            {
                var threadRaiders = Threads[args.Thread.Channel.Id];
                if (threadRaiders.Count > 0)
                {
                    threadRaiders.RemoveAll(m => m.Metadata.CreatedAt < (DateTime.Now - new TimeSpan(0, 0, 3)) - ThreadExpiration);
                }
                if (GetAntiRaidToggle(args.Thread.Guild) == true)
                {
                    threadRaiders.Add(args.Thread);
                    if (threadRaiders.Count() >= MaxThreads)
                    {
                        Console.WriteLine(" [Anti-Raid] Thread spam detected in " + args.Thread.Guild.Id);

                        var Copy = new List<DiscordThread>(threadRaiders);

                        foreach (var thread in Copy)
                        {
                            try
                            {
                                new Thread(() => CheckForPermsAndAddInQueue2(thread.Guild.Id, thread.OwnerId)).Start();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(" [Anti-Raid] Error adding in queue: " + ex.Message);
                            }
                        }

                        foreach (DiscordThread thread in Copy)
                        {
                            Thread.Sleep(100);
                            client.DeleteThread(thread.Id);
                        }
                    }
                }
            }
            catch (DiscordHttpException ex)
            {
                Console.WriteLine(" [Anti-Raid] Thread error occurred: " + ex.Message);
            }
        }

        private static void Client_OnJoinedGuild(DiscordSocketClient client, SocketGuildEventArgs args)
        {
            try
            {
                Console.WriteLine($" [Litio] Joined new guild! Name: {args.Guild.Name} | Id: {args.Guild.Id} | Members: {args.Guild.MemberCount}");

                Raiders[args.Guild.Id] = new List<GuildMember>();
                foreach (GuildChannel channel in args.Guild.GetChannels())
                {
                    if (channel.IsText)
                    {
                        Messages[channel.Id] = new List<DiscordMessage>();
                        Threads[channel.Id] = new List<DiscordThread>();
                    }
                }

                if (!Utils.Guilds.Select(x => x.GuildId).Contains(args.Guild.Id.ToString()))
                {
                    Console.WriteLine(" [Litio] Saving guild...");
                    Utils.Guilds.Add(new LitioGuild() { TimeoutDuration = new TimeSpan(0, 0, 0), PunishmentType = LitioPunishment.Ban, GuildId = args.Guild.Id.ToString(), Toggled = true });
                    File.WriteAllText("Database.txt", JsonConvert.SerializeObject(Utils.Guilds, Formatting.Indented));

                    Console.WriteLine(" [Litio] Saved guild.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(" [Litio] Error occurred: " + ex.Message);
            }
        }



        private static bool GetAntiRaidToggle(MinimalGuild guild)
        {
            try
            {
                return Utils.Guilds.Find(x => x.GuildId == guild.Id.ToString()).Toggled;
            }
            catch
            {
                return false;
            }
        }

        private static void Client_OnUserJoinedGuild(DiscordSocketClient client, GuildMemberEventArgs args)
        {
            try
            {
                var raiders = Raiders[args.Member.Guild.Id];
                if (raiders.Count > 0)
                {
                    raiders.RemoveAll(m => m.JoinedAt < (DateTime.Now - new TimeSpan(0, 0, 3)) - MessageExpiration);
                }
                if (GetAntiRaidToggle(args.Member.Guild) == true)
                {
                    raiders.Add(args.Member);
                    if (raiders.Count >= MaxJoins)
                    {
                        Console.WriteLine(" [Anti-Raid] Botting detected in " + args.Member.Guild.Id);

                        var Copy = new List<GuildMember>(raiders);

                        foreach (var member in Copy)
                        {
                            try
                            {
                                BanQueue.Enqueue(member);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(" [Anti-Raid] Error adding in queue: " + ex.Message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(" [Anti-Raid] Join error occurred: " + ex.Message);
            }
        }

        private static void CheckForPermsAndAddInQueue2(ulong guildId, ulong memberId)
        {
            try
            {
                var member = client.GetGuildMember(guildId, memberId);
                if (member != null)
                {
                    if (!member.GetPermissions().Has(DiscordPermission.ManageGuild | DiscordPermission.BanMembers | DiscordPermission.ManageMessages | DiscordPermission.ManageChannels | DiscordPermission.ManageGuild | DiscordPermission.Administrator))
                    {
                        BanQueue.Enqueue(member);
                    }
                }
            }
            catch
            {

            }
        }

        private static void CheckForPermsAndAddInQueue(GuildMember member)
        {
            try
            {
                if (member != null)
                {
                    if (!member.GetPermissions().Has(DiscordPermission.ManageGuild | DiscordPermission.BanMembers | DiscordPermission.ManageMessages | DiscordPermission.ManageChannels | DiscordPermission.ManageGuild | DiscordPermission.Administrator))
                    {
                        BanQueue.Enqueue(member);
                    }
                }
            }
            catch
            {

            }
        }

        private static void Client_OnMessageReceived(DiscordSocketClient client, MessageEventArgs args)
        {
            try
            {
                if (!string.IsNullOrEmpty(args.Message.Content))
                {
                    if (!Messages.ContainsKey(args.Message.Channel.Id))
                        Messages[args.Message.Channel.Id] = new List<DiscordMessage>();
                    var messages = Messages[args.Message.Channel.Id];
                    if (messages.Count > 0)
                    {
                        messages.RemoveAll(m => m.SentAt < DateTime.Now - new TimeSpan(0, 0, 3) - MessageExpiration);
                    }

                    if (GetAntiRaidToggle(args.Message.Guild) == true)
                    {
                        messages.Add(args.Message);
                        if (messages.Count >= MaxMessages)
                        {
                            Console.WriteLine(" [Anti-Raid] Raid detected in " + args.Message.Guild.Id);

                            var msgsCopy = new List<DiscordMessage>(messages);

                            foreach (var msg in msgsCopy)
                            {
                                try
                                {
                                    new Thread(() => CheckForPermsAndAddInQueue(msg.Author.Member)).Start();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(" [Anti-Raid] Error adding in queue: " + ex.Message);
                                }
                            }

                            client.DeleteMessages(msgsCopy[0].Channel.Id, msgsCopy.Select(x => x.Id).ToList());
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(" [Anti-Raid] Spam error occurred: " + ex.Message);
            }
        }

        private static void Client_OnLoggedIn(DiscordSocketClient client, LoginEventArgs args)
        {
            Console.Title = "Logged in as " + client.User;
            Console.WriteLine(" [Litio] Logged in");
            Utils.Guilds = Utils.GetGuilds();

            foreach (var cmd in client.GetGlobalCommands(client.User.Id))
                cmd.Delete();

            client.RegisterSlashCommands();
        }
    }
}