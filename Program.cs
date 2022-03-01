using Discord;
using Discord.Gateway;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        public static readonly TimeSpan JoinExpiration = new TimeSpan(0, 0, 2);
        public static readonly TimeSpan ThreadExpiration = new TimeSpan(0, 0, 2);

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine(" [Litio] Starting and setting up, please wait.");
                Console.Title = "Litio V2 | Anti-Raid";

                client = new DiscordSocketClient(new DiscordSocketConfig()
                {
                    Intents = GatewayIntentBundles.Guilds | GatewayIntentBundles.GuildMessages | GatewayIntentBundles.GuildAdministration | DiscordGatewayIntent.GuildMembers,
                    RetryOnRateLimit = true,
                    ApiVersion = 9
                });
                BanQueue.Start(client);

                client.OnLoggedIn += Client_OnLoggedIn;
                client.OnJoinedGuild += Client_OnJoinedGuild;
                client.OnMessageReceived += Client_OnMessageReceived;
                client.OnUserJoinedGuild += Client_OnUserJoinedGuild;
                client.OnThreadCreated += Client_OnThreadCreated;
                client.OnInteraction += Client_OnInteraction;

                Console.Write(" [Litio] Bot token? [Y/N] ");
                string botToken = Console.ReadLine();
                Console.Write(" [Litio] Insert token: ");
                string token = Console.ReadLine();

                client.Login(botToken.ToLower() == "y" ? "Bot " + token : token);
                Thread.Sleep(-1);
            }
            catch (InvalidTokenException)
            {
                Console.WriteLine(" [Litio] Invalid token, if bot token make sure you have inserted 'Bot ' as prefix (without the ').");
                Console.ReadLine();
            }
        }

        private static DiscordEmbed CreateEmbed(Color Color, GuildMember member, string AuthorName, string Description)
        {
            return new EmbedMaker()
            {
                Color = Color,
                Author = new EmbedAuthor()
                {
                    Name = AuthorName,
                    IconUrl = member.User.Avatar != null ? member.User.Avatar.Url : client.User.Avatar.Url
                },
                Description = Description,
                Footer = new EmbedFooter()
                {
                    Text = "Litio",
                },
                Timestamp = DateTime.Now
            };
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
                if (Utils.GetAntiRaidToggle(args.Thread.Guild) == true)
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

                if (!Raiders.ContainsKey(args.Guild.Id))
                {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine(" [Litio] Error occurred: " + ex.Message);
            }
        }

        private static void Client_OnUserJoinedGuild(DiscordSocketClient client, GuildMemberEventArgs args)
        {
            try
            {
                var raiders = Raiders[args.Member.Guild.Id];
                if (raiders.Count > 0)
                {
                    raiders.RemoveAll(m => m.JoinedAt < (DateTime.Now - new TimeSpan(0, 0, 3)) - JoinExpiration);
                }
                if (Utils.GetAntiRaidToggle(args.Member.Guild) == true)
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

                    if (Utils.GetAntiRaidToggle(args.Message.Guild) == true)
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

        private static void Client_OnInteraction(DiscordSocketClient client, DiscordInteractionEventArgs args)
        {
            try
            {
                switch (args.Interaction.Data.CommandName)
                {
                    case "setpunishment":
                        try
                        {
                            if (!args.Interaction.Member.GetPermissions().Has(DiscordPermission.ManageGuild))
                            {
                                args.Interaction.Respond(InteractionCallbackType.RespondWithMessage, new InteractionResponseProperties()
                                {
                                    Embed = CreateEmbed(Utils.Error, args.Interaction.Member, "Missing permissions.", $"You don't have the required permissions to execute that command."),
                                    Ephemeral = true
                                });
                            }

                            string punishmentType = "Ban";
                            switch (args.Interaction.Data.CommandArguments[0].Value.ToLower())
                            {
                                case "punishment_ban":
                                    Utils.Guilds.FirstOrDefault(x => x.GuildId == args.Interaction.Guild.Id.ToString()).PunishmentType = LitioPunishment.Ban;
                                    break;
                                case "punishment_kick":
                                    Utils.Guilds.FirstOrDefault(x => x.GuildId == args.Interaction.Guild.Id.ToString()).PunishmentType = LitioPunishment.Kick;
                                    punishmentType = "Kick";
                                    break;
                                case "punishment_timeout":
                                    Utils.Guilds.FirstOrDefault(x => x.GuildId == args.Interaction.Guild.Id.ToString()).PunishmentType = LitioPunishment.Timeout;
                                    punishmentType = "Timeout";
                                    break;
                            }
                            args.Interaction.Respond(InteractionCallbackType.RespondWithMessage, new InteractionResponseProperties()
                            {
                                Embed = CreateEmbed(Utils.Success, args.Interaction.Member, client.GetGuild(args.Interaction.Guild.Id).Name, $"Updated punishment type as `{punishmentType}`"),
                                Ephemeral = true,
                            });
                            File.WriteAllText("Database.txt", JsonConvert.SerializeObject(Utils.Guilds, Formatting.Indented));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            args.Interaction.Respond(InteractionCallbackType.RespondWithMessage, new InteractionResponseProperties()
                            {
                                Embed = CreateEmbed(Utils.Error, args.Interaction.Member, "Error occurred.", "An unknown error has occurred, we apologize for the inconvenience."),
                                Ephemeral = true
                            });
                        }
                        break;
                }
            }
            catch (DiscordHttpException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void Client_OnLoggedIn(DiscordSocketClient client, LoginEventArgs args)
        {
            try
            {
                Console.Title = "Litio V2 | Logged in as " + client.User;
                Console.WriteLine(" [Litio] Logged in");
                Utils.Guilds = Utils.GetGuilds();
                if (args.User.Type == DiscordUserType.User)
                {
                    foreach (var guild in args.Guilds)
                    {
                        if (!Raiders.ContainsKey(guild.Id))
                        {
                            Raiders[guild.Id] = new List<GuildMember>();
                            foreach (GuildChannel channel in guild.GetChannels())
                            {
                                if (channel.IsText)
                                {
                                    Messages[channel.Id] = new List<DiscordMessage>();
                                    Threads[channel.Id] = new List<DiscordThread>();
                                }
                            }

                            if (!Utils.Guilds.Select(x => x.GuildId).Contains(guild.Id.ToString()))
                            {
                                Console.WriteLine(" [Litio] Saving guild...");
                                Utils.Guilds.Add(new LitioGuild() { TimeoutDuration = new TimeSpan(0, 0, 0), PunishmentType = LitioPunishment.Ban, GuildId = guild.Id.ToString(), Toggled = true });
                                File.WriteAllText("Database.txt", JsonConvert.SerializeObject(Utils.Guilds, Formatting.Indented));

                                Console.WriteLine(" [Litio] Saved guild.");
                            }
                        }
                    }
                }

                client.RegisterSlashCommands();
                string data = "{\"name\":\"setpunishment\",\"type\":1,\"description\":\"Set the punishment type for the current guild.\",\"options\":[{\"name\":\"punishment\",\"description\":\"The punishment type.\",\"type\":3,\"required\":true,\"choices\":[{\"name\":\"Ban\",\"value\":\"punishment_ban\"},{\"name\":\"Kick\",\"value\":\"punishment_kick\"},{\"name\":\"Timeout\",\"value\":\"punishment_timeout\"}]}]}";
                client.HttpClient.PostAsync($"https://discord.com/api/v9/applications/{client.User.Id}/commands", data).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}