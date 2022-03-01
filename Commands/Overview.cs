using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Litio.Commands
{
    [SlashCommand("overview", "View current server settings.")]
    public class OverviewCmd : SlashCommand
    {
        [SlashParameter("ephemeral", "Choose if show the message to everyone or just you.")]
        public bool Ephemeral { get; private set; }

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
                if (!CallerMember.GetPermissions().Has(DiscordPermission.ManageGuild))
                {
                    return new InteractionResponseProperties()
                    {
                        Embed = CreateEmbed(Utils.Error, "Missing permissions.", $"You don't have the required permissions to execute that command."),
                        Ephemeral = true
                    };
                }

                return new InteractionResponseProperties()
                {
                    Embed = CreateEmbed(Utils.Success, Client.GetGuild(Guild.Id).Name, $@"**Anti-raid enabled**: `{Utils.GetAntiRaidToggle(Guild)}`
**Punishment type**: `{Utils.GetPunishmentType(Guild)}`"),
                    Ephemeral = Ephemeral
                };
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
