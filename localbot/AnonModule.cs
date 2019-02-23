using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Discord.WebSocket;
using System;

namespace localbot
{
    public class AnonModule : ModuleBase<SocketCommandContext>
    {
        private static Random random = new Random();
        private static Dictionary<ulong, int> anon_users = new Dictionary<ulong, int>();
        private static List<ulong> blacklist = new List<ulong>();
        private static SocketTextChannel anon_channel;

        [Command("set_anon_channel")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetChannel()
        {
            anon_channel = (Context.Channel as SocketTextChannel);
            await ReplyAsync($"anon channel set");
        }

        [Command("disable_anon_channel")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DisableChannel()
        {
            anon_channel = null;
            await ReplyAsync($"anon channel disabled");
        }

        [Command("reset_anon")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ResetAnon()
        {
            anon_users = new Dictionary<ulong, int>();
            await ReplyAsync($"anon numbers reset");
        }

        [Command("blacklist")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Blacklist(int num)
        {
            if (anon_users.ContainsValue(num))
            {
                ulong user_to_be_blacklisted = 0;
                foreach(ulong u in anon_users.Keys)
                {
                    if (anon_users[u] == num)
                        user_to_be_blacklisted = u;
                }
                if (!blacklist.Contains(user_to_be_blacklisted))
                {
                    blacklist.Add(user_to_be_blacklisted);
                    await ReplyAsync($"user was blacklisted");
                }
                else
                {
                    await ReplyAsync($"user was already blacklisted");
                }
            } else
            {
                await ReplyAsync($"id {num} no longer in use");
            }

        }

        [Command("unblacklist")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task UnBlacklist(IGuildUser user)
        {
            if (blacklist.Contains(user.Id))
            {
                blacklist.Remove(user.Id);
                await ReplyAsync($"user unblacklisted");
            } else
            {
                await ReplyAsync($"user was not blacklisted");
            }

        }

        [Command("newid")]
        public async Task NewID([Remainder] int num)
        {
            if (blacklist.Contains(Context.User.Id))
            {
                await (Context.User).SendMessageAsync($"you are blacklisted");
                return;
            }

            if (num > 1000 || num < 0)
            {
                await (Context.User).SendMessageAsync($"your id must be between 0 and 1000");
                return;
            }

            foreach (int value in anon_users.Values)
            {
                if (num == value)
                {
                    await (Context.User).SendMessageAsync($"{num} is taken please generate a new ID");
                    return;
                }
            }

            if (anon_users.ContainsKey(Context.User.Id))
            {
                anon_users[Context.User.Id] = num;
            } else
            {
                anon_users.Add(Context.User.Id, num);
            }
            
            await (Context.User).SendMessageAsync($"you are now speaking under id: {num}");
        }

        [Command("anon")]
        public async Task Anon([Remainder] string text)
        {
            if (blacklist.Contains(Context.User.Id))
            {
                await (Context.User).SendMessageAsync($"you are blacklisted");
                return;
            }

            if (!anon_users.ContainsKey(Context.User.Id))
            {
                await (Context.User).SendMessageAsync("please generate an id with `newID`");
                return;
            }

            int current_id = anon_users[Context.User.Id];
            await (anon_channel).SendMessageAsync($"`{current_id}:` {text}");
        }


    }
}
