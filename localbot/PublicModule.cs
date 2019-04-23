﻿using System.IO;
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

        [Command(">help")]
        public async Task LocalHelp()
        {
            var helpEmbed = new EmbedBuilder
            {
                Title = "Commands:",
                Color = Color.Red
            };
            helpEmbed.AddField(">ping", "pong");
            helpEmbed.AddField(">newid <id>", "generate or manually select an id");
            helpEmbed.AddField(">message <id>", "send a message to another anon user under your current id");
            helpEmbed.AddField(">anon <message>", "send a message to the anonymous channel under your current id");
            helpEmbed.AddField(">rel <message>", "send a message to the relationships channel under your current id");
            helpEmbed.AddField(">relationships <message>", "alternate command to >relationships");
            helpEmbed.AddField(">set_color <r> <g> <b>", "set the embed color you will send anon messages with");
            helpEmbed.AddField(">timeout <id> <time>", "[requires kick permissions] mute anon user <id> for <time> minutes");
            helpEmbed.AddField(">blacklist <id>", "[requires kick permissions] add user <id> to the blacklist preventing them from sending messages through the bot");
            helpEmbed.AddField(">unblacklist <id>", "[requires kick permissions] remove a user from the blacklist and or clear any timeout they have incured");

            await Context.User.SendMessageAsync(embed: helpEmbed.Build());
        }

        [Command(">ping")]
        public async Task Ping()
        {
            await ReplyAsync("pong");
        }
        
    }
}