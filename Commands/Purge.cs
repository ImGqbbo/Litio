using Discord;
using Discord.Commands;
using System;
using System.Drawing;
using System.Linq;

namespace Litio.Commands
{
    [SlashCommand("purge", "Purge a customizable amount of messages.")]
    public class PurgeCommand : SlashCommand
    {
        [SlashParameter("amount", "Amount of messages to purge.", true)]
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

                if (Amount > 100)
                {
                    return new InteractionResponseProperties()
                    {
                        Embed = CreateEmbed(Utils.Error, "Malformed input.", "Due to a Discord API limitation, the maximum amount of messages to purge is 100."),
                        Ephemeral = true
                    };
                }

                if (Amount == 100) Amount = Amount - 1;
                var messages = Client.GetChannelMessages(Channel.Id, new MessageFilters() { Limit = Amount });
                if (messages != null && messages.Count > 0)
                {
                    Client.DeleteMessages(Channel.Id, messages.Select(x => x.Id).ToList());
                    return new InteractionResponseProperties()
                    {
                        Embed = CreateEmbed(Utils.Success, "Task completed.", $"Succesfully deleted `{Amount}` messages."),
                        Ephemeral = false
                    };
                }

                return new InteractionResponseProperties()
                {
                    Embed = CreateEmbed(Utils.Error, "Error occurred.", "Failed to fetch the messages, we apologize for the inconvenience."),
                    Ephemeral = true
                };
            }
            catch
            {
                return new InteractionResponseProperties()
                {
                    Embed = CreateEmbed(Utils.Error, "Error occurred.", "An unknown error has occurred, we apologize for the inconvenience."),
                    Ephemeral = true
                };
            }
        }
    }
}
