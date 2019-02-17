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
            await (Context.User as SocketGuildUser).SendMessageAsync(
                "`Color Commands:`\n\n" +
                "`set_color <color>`" +
                "\nsets user color to the role with the color_ prefix\n\n" +
                "`remove_color" +
                "\nremoves any roles with the color_ prefix from user\n\n" +
                "Anon Commands:\n" +
                "DM these commands to the bot\n\n" +
                "`newID` <id>" +
                "\nchoose your id number (between 0 and 1000)\n\n" +
                "`anon <message>`" +
                "\nsends a message to the relevant anon channel using your id\n\n" +
                "`blacklist <anon-id>`" +
                "\nprevents discord user behind <anon-id> from using the bot\n\n" +
                "`unblacklist <discord-tag>`" +
                "\nremoves a user from the blacklist"
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