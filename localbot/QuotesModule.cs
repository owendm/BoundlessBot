using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Discord.WebSocket;
using System.IO;
using System;

namespace localbot
{
    public class QuotesModule : ModuleBase<SocketCommandContext>
    {
        //string path = @"c:\users\Owen\desktop\quotes.txt";

        //[Command(">AddQuote")]
        //public async Task AddQuote(IGuildUser attributed, [Remainder] string quote)
        //{
        //    if(File.Exists(path))
        //    {
        //        using (StreamWriter sw = File.CreateText(path))
        //        {
        //            // Write quote to file:
        //            // - Generate unique quote ID
        //            // - Store person who added quote?
        //        }
        //        await ReplyAsync($"Quote \"{quote}\" was added, attributed to {attributed.AvatarId}");
        //        return;
        //    } else
        //    {
        //        await ReplyAsync($"Quote Directory File Not Found");
        //        return;
        //    }
        //}

        //[Command(">Quote")]
        //public async Task Quote(IGuildUser attributed)
        //{
        //    if (File.Exists(path))
        //    {
                
        //    }
        //    else
        //    {
        //        await ReplyAsync($"Quote Directory File Not Found");
        //        return;
        //    }
        //}

        //[Command(">Quote")]
        //public async Task Quote(int id)
        //{
        //    if (File.Exists(path))
        //    {
                
        //    }
        //    else
        //    {
        //        await ReplyAsync($"Quote Directory File Not Found");
        //        return;
        //    }
        //}
    }
}
