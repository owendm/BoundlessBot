using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Collections;
using System.Collections.Generic;
using Discord.WebSocket;
using System;

namespace localbot
{

    public class PublicModule : ModuleBase<SocketCommandContext>
    {

        [Command("localhelp")]
        public async Task LocalHelp()
        {

            await (Context.User as SocketGuildUser).SendMessageAsync("`localBot Commands:`\n" +
                "\n" +
                "`newID <id-number>`\n" +
                "assigns <id-number> to the user for use with `anon`\n" +
                "\n" +
                "`anon <message>" +
                "sends a message using the bot under your <id-number> alias\n" +
                "\n" +
                "`blacklist <id-number>\n" +
                "prevents a discord user with anon-id <id-number> from using the bot\n" +
                "\n" +
                "`unblacklist <discord-tag>\n" +
                "removes a discord user from the blacklist\n" +
                "\n" +
                "`set_color <color>`\n" +
                "assigns the user the role <color>. only works with roles that start with prefix color_\n" +
                "\n" +
                "`remove_color`\n" +
                "removes all _color roles from user"
                );
        }

        [Command("remove_color")]
        public async Task RemoveColor()
        {
            foreach(IRole i in (Context.User as SocketGuildUser).Roles)
            {
                if (i.Name.Substring(0, 6) == "color_")
                    await (Context.User as SocketGuildUser).RemoveRoleAsync(Context.Guild.GetRole(i.Id));
            }
        }

  
        [Command("set_color")]
        public async Task SetColorAsync([Remainder] string color)
        {
            if (color.Length < 8)
                return;
            if (color.Substring(0, 6) != "color_")
            {
                color = "color_" + color;
            }

            foreach (IRole u in (Context.User as SocketGuildUser).Roles)
            {
                if (u.Name.Substring(0, 6) == "color_")
                {
                    await (Context.User as SocketGuildUser).RemoveRoleAsync(Context.Guild.GetRole(u.Id));
                }
            }

            foreach(IRole i in Context.Guild.Roles)
            {
                if(i.Name == color)
                {
                    await (Context.User as SocketGuildUser).AddRoleAsync(Context.Guild.GetRole(i.Id));
                    return;
                }
            }

            await ReplyAsync($"Color {color} not found");
        }

    }
}