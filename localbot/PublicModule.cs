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

        [Command(">localhelp")]
        public async Task LocalHelp()
        {

            await (Context.User as SocketGuildUser).SendMessageAsync("`localBot Commands:`\n"
                );
        }
        
    }
}