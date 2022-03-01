using Discord;
using Discord.Commands;
using System;
using System.Drawing;
using System.Linq;

namespace Litio.Commands
{
    [SlashCommand("purge", "Purge a customizable amount of messages.")]
    public class PurgeCmd : SlashCommand
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
                if (!CallerMember.GetPermissions().Has(DiscordPermission.ManageMessages))
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

                var messages = Client.GetChannelMessages(Channel.Id, new MessageFilters() { Limit = Amount == 100 ? Amount - 1 : Amount});
                if (messages.Count > 0)
                {
                    Client.DeleteMessages(Channel.Id, messages.Select(x => x.Id).ToList());
                    return new InteractionResponseProperties()
                    {
                        Embed = CreateEmbed(Utils.Success, Client.GetGuild(Guild.Id).Name, $"Succesfully deleted `{Amount}` messages."),
                        Ephemeral = true
                    };
                }
                else
                {
                    return new InteractionResponseProperties()
                    {
                        Embed = CreateEmbed(Utils.Error, "Error occurred.", "Looks that this channel has no messages to purge."),
                        Ephemeral = true
                    };
                }
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
