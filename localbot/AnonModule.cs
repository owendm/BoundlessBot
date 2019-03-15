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
        public const int maxID = 1000;
        public const int historyLength = 5;

        private class AnonUser
        {
            public ulong user { get; set; }
            private Stack<int> ids;
            public DateTime lastNewID { get; set; }

            public AnonUser(ulong user)
            {
                this.user = user;
                ids = new Stack<int>();
            }

            public int getID()
            {
                return ids.Peek();
            }

            public bool AliasAs(int id)
            {
                bool foundID = false;
                Stack<int> temp = new Stack<int>();
                while(ids.Count != 0)
                {
                    temp.Push(ids.Pop());
                }
                while (temp.Count != 0)
                {
                    int cur = temp.Pop();
                    if (cur == id)
                    {
                        foundID = true;
                    }
                    ids.Push(cur);
                }                
                return foundID;
            }

            public void NewAlias(int alias)
            {
                ids.Push(alias);
            }

        }


        private static Random random = new Random();
        private static SocketTextChannel anon_channel;
        private TimeSpan cooldown = new TimeSpan(0, 00, 10);

        private static List<AnonUser> activeUsers = new List<AnonUser>();
        private static List<AnonUser> blacklist = new List<AnonUser>();

        [Command(">set_anon_channel")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetChannel()
        {
            anon_channel = (Context.Channel as SocketTextChannel);
            await ReplyAsync($"anon channel set");
        }

        [Command(">disable_anon_channel")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DisableChannel()
        {
            anon_channel = null;
            await ReplyAsync($"anon channel disabled");
        }

        [Command(">reset_anon")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ResetAnon()
        {
            activeUsers = new List<AnonUser>();
            await ReplyAsync($"anon numbers reset");
        }

        [Command(">blacklist")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Blacklist(int num)
        {
            foreach(AnonUser u in activeUsers)
            {
                if(u.AliasAs(num))
                {
                    AnonUser suspect = u;
                    activeUsers.Remove(u);
                    blacklist.Add(u);
                    await ReplyAsync($"user was blacklisted");
                } else
                {
                    await ReplyAsync($"user ID not found");
                }
            }
        }

        [Command(">unblacklist")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task UnBlacklist(IGuildUser user)
        {
            if (isBlacklisted(user.Id))
            {
                blacklist.Remove(GetBlUser(user.Id));
                await ReplyAsync($"user unblacklisted");
            } else
            {
                await ReplyAsync($"user was not blacklisted to begin with");
            }

        }

        [Command(">newid")]
        public async Task NewID([Remainder] int num)
        {
            if (isBlacklisted(Context.User.Id))
            {
                await (Context.User).SendMessageAsync($"you are blacklisted");
                return;
            }

            if (GetUser(Context.User.Id) == null)
            {
                activeUsers.Add(new AnonUser(Context.User.Id));
                GetUser(Context.User.Id).lastNewID = DateTime.Now;
            } else if (DateTime.Now - GetUser(Context.User.Id).lastNewID < cooldown)
            {
                await (Context.User).SendMessageAsync($"newID is on cooldown, wait {(cooldown - (DateTime.Now - GetUser(Context.User.Id).lastNewID)).ToString()}");
                return;
            }
            
            if (activeUsers.Count / historyLength > maxID)
            {
                await (Context.User).SendMessageAsync($"all IDs are currently in use, contact your mods to reset IDs");
                return;
            }

            if ((RecentlyUsed(num) || num < 0 || num > maxID))
            {
                await (Context.User).SendMessageAsync($"{num} is either taken or out of acceptable range");
                return;
            }
            
            GetUser(Context.User.Id).NewAlias(num);
            GetUser(Context.User.Id).lastNewID = DateTime.Now;

            await (Context.User).SendMessageAsync($"you are now speaking under id: '{num}'");
        }

        [Command(">newid")]
        public async Task NewID()
        {
            if (isBlacklisted(Context.User.Id))
            {
                await (Context.User).SendMessageAsync($"you are blacklisted");
                return;
            }
            if (GetUser(Context.User.Id) == null)
            {
                activeUsers.Add(new AnonUser(Context.User.Id));
                GetUser(Context.User.Id).lastNewID = DateTime.Now;
            } else if (DateTime.Now - GetUser(Context.User.Id).lastNewID < cooldown)
            {
                await (Context.User).SendMessageAsync($"newID is on cooldown, wait {(cooldown - (DateTime.Now - GetUser(Context.User.Id).lastNewID)).ToString()}");
                return;
            }

            if (activeUsers.Count / historyLength > maxID)
            {
                await (Context.User).SendMessageAsync($"all IDs are currently in use, contact your mods to reset IDs");
                return;
            }

            int num = random.Next(maxID);
            while(RecentlyUsed(num)) // yeah, yeah ree i don't predict max id capacity anytime soon
            {
                random.Next(maxID);
            }
            
            GetUser(Context.User.Id).NewAlias(num);
            GetUser(Context.User.Id).lastNewID = DateTime.Now;

            await (Context.User).SendMessageAsync($"you are now speaking under id: '{num}'");
        }

        [Command(">anon")]
        public async Task Anon([Remainder] string text)
        {
            if (isBlacklisted(Context.User.Id))
            {
                await (Context.User).SendMessageAsync($"you are blacklisted");
                return;
            }

            if (!IsActiveUser(Context.User.Id))
            {
                await (Context.User).SendMessageAsync("please generate an id with `newID`");
                return;
            }

            int current_id = GetUser(Context.User.Id).getID();
            await (anon_channel).SendMessageAsync($"`{current_id}:` {text}");
        }

        // To Implement: Check context to polish responses when commands executed in wrong context
        private bool ContextCheck(SocketCommandContext current, String opt)
        {
            return true;            
        }

        private bool RecentlyUsed(int id)
        {
            foreach(AnonUser user in activeUsers)
            {
                if(user.AliasAs(id) == true)
                {
                    return true;
                }
            }
            return false;
        }

        private AnonUser GetUser(ulong user)
        {
            foreach(AnonUser u in activeUsers)
            {
                if (u.user == user)
                {
                    return u;
                }
            }
            return null;
        }
        private AnonUser GetUser(int id)
        {
            foreach (AnonUser u in activeUsers)
            {
                if (u.getID() == id)
                {
                    return u;
                }
            }
            return null;
        }

        private AnonUser GetBlUser(ulong user)
        {
            foreach (AnonUser u in blacklist)
            {
                if (u.user == user)
                {
                    return u;
                }
            }
            return null;
        }

        private bool IsActiveUser(ulong user)
        {
            foreach (AnonUser u in activeUsers)
            {
                if (u.user == user)
                {
                    return true;
                }
            }
            return false;
        }

        private bool isBlacklisted(ulong user)
        {
            foreach(AnonUser u in blacklist)
            {
                if (u.user == user)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
