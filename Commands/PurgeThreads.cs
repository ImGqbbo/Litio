using Discord;
using Discord.Commands;
using System;
using System.Drawing;
using System.Threading;

namespace Litio.Commands
{
    [SlashCommand("purgethreads", "Purge all the channel threads (BETA).")]
    public class PurgeThreadsCommand : SlashCommand
    {
        [SlashParameter("amount", "Amount of threads to delete.", true)]
        public uint Amount { get; private set; }

        private DiscordEmbed CreateEmbed(Color Color, string AuthorName, string Description)
        {
            return new EmbedMaker()
            {
                Color = Color,
                Author = new EmbedAuthor()
                {
                    Name = AuthorName,
                    IconUrl = Caller.Avatar != null ? Caller.Avatar.Url : Client.User.Avatar.Url
                },
                Description = Description,
                Footer = new EmbedFooter()
                {
                    Text = "Litio",
                },
                Timestamp = DateTime.Now
            };
        }

        public override InteractionResponseProperties Handle()
        {
            try
            {
                if (!CallerMember.GetPermissions().Has(DiscordPermission.ManageThreads))
                {
                    return new InteractionResponseProperties()
                    {
                        Embed = CreateEmbed(Utils.Error, "Missing permissions.", $"You don't have the required permissions to execute that command."),
                        Ephemeral = true
                    };
                }

                var threads = Client.GetChannelActiveThreads(Channel.Id);
                if (threads.Count > 0)
                {
                    new Thread(() =>
                    {
                        int threadsDeleted = 0;
                        foreach (DiscordThread thread in threads)
                        {
                            Thread.Sleep(100);
                            if (threadsDeleted == Amount)
                                break;

                            Client.DeleteThread(thread.Id);
                            threadsDeleted++;
                        }
                    }).Start();

                    return new InteractionResponseProperties()
                    {
                        Embed = CreateEmbed(Utils.Success, "Task completed.", $"Succesfully deleted `{Amount}` threads."),
                        Ephemeral = false
                    };
                }
                else
                {
                    return new InteractionResponseProperties()
                    {
                        Embed = CreateEmbed(Utils.Error, "Error occurred.", "This channel has no threads to delete."),
                        Ephemeral = true
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new InteractionResponseProperties()
                {
                    Embed = CreateEmbed(Utils.Error, "Error occurred.", "An unknown error has occurred, we apologize for the inconvenience."),
                    Ephemeral = true
                };
            }
        }
    }
}
