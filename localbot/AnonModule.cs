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

        // Max ID number to be generated or chosen with >newID
        public const int maxID = 1000;
        // Length of the history of IDs that each AnonUser records
        public const int historyLength = 5;

        private class AnonUser
        {
            public ulong user;
            private List<int> ids;
            public DateTime lastNewID;
            public bool blacklisted;

            public bool timeout;
            public DateTime timeoutEnd;

            public AnonUser(ulong user)
            {
                this.user = user;
                ids = new List<int>();
            }

            public int getID()
            {
                return ids[0];
            }

            public bool IsBlacklisted()
            {
                return blacklisted;
            }

            public bool IsTimedout()
            {
                if (timeout == false)
                {
                    return true;
                } else
                {
                    if(timeoutEnd - DateTime.Now < new TimeSpan(0, 0, 0))
                    {
                        return true;
                    } else
                    {
                        return false;
                    }
                }
            }

            public bool AliasAs(int id)
            {
                return ids.Contains(id);
            }

            public void NewAlias(int alias)
            {
                if (ids.Count > historyLength)
                {
                    ids.RemoveAt(ids.Count - 1);
                }
                ids.Add(alias);
            }

        }

        private static Random random = new Random();
        private static SocketTextChannel anon_channel;
        private TimeSpan cooldown = new TimeSpan(0, 00, 10);

        private static List<AnonUser> activeUsers = new List<AnonUser>();

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
                    u.blacklisted = true;
                    await ReplyAsync($"user {num} was blacklisted");
                    return;
                }
            }
        }

        [Command(">timeout")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Timeout(int num, int minutes)
        {
            foreach (AnonUser u in activeUsers)
            {
                if (u.AliasAs(num))
                {
                    u.timeout = true;
                    u.timeoutEnd = DateTime.Now + new TimeSpan(0, minutes, 0);
                    await ReplyAsync($"user {num} for {minutes} minutes");
                    return;
                }
            }
        }

        [Command(">unblacklist")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task UnBlacklist(IGuildUser user)
        {
            if (GetUser(Context.User.Id).IsBlacklisted())
            {
                GetUser(Context.User.Id).blacklisted = false;
                await ReplyAsync($"user unblacklisted");
            } else
            {
                await ReplyAsync($"user was not blacklisted to begin with");
            }

        }

        [Command(">newid")]
        public async Task NewID([Remainder] int num)
        {
            if (GetUser(Context.User.Id) == null)
            {
                activeUsers.Add(new AnonUser(Context.User.Id));
                GetUser(Context.User.Id).lastNewID = DateTime.Now;
            }
            if (GetUser(Context.User.Id).IsBlacklisted())
            {
                await (Context.User).SendMessageAsync($"you are blacklisted");
                return;
            }
            if (GetUser(Context.User.Id).IsTimedout())
            {
                await (Context.User).SendMessageAsync($"you are timed out");
                return;
            }
            if (DateTime.Now - GetUser(Context.User.Id).lastNewID < cooldown)
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

            await (Context.User).SendMessageAsync($"you are now speaking under id: `{num}`");
        }

        [Command(">newid")]
        public async Task NewID()
        {
            if (GetUser(Context.User.Id) == null)
            {
                activeUsers.Add(new AnonUser(Context.User.Id));
                GetUser(Context.User.Id).lastNewID = DateTime.Now;
            }
            if (GetUser(Context.User.Id).IsBlacklisted())
            {
                await (Context.User).SendMessageAsync($"you are blacklisted");
                return;
            }
            if (GetUser(Context.User.Id).IsTimedout())
            {
                await (Context.User).SendMessageAsync($"you are timed out");
                return;
            }
            if (DateTime.Now - GetUser(Context.User.Id).lastNewID < cooldown)
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

            await (Context.User).SendMessageAsync($"you are now speaking under id: `{num}`");
        }

        [Command(">anon")]
        public async Task Anon([Remainder] string text)
        {

            if (GetUser(Context.User.Id) == null)
            {
                await (Context.User).SendMessageAsync("please generate an id with `newID`");
                return;
            }
            if (GetUser(Context.User.Id).IsBlacklisted())
            {
                await (Context.User).SendMessageAsync($"you are blacklisted");
                return;
            }
            if (GetUser(Context.User.Id).IsTimedout())
            {
                await (Context.User).SendMessageAsync($"you are timed out");
                return;
            }

            int current_id = GetUser(Context.User.Id).getID();
            await (anon_channel).SendMessageAsync($"`{current_id}:` {text}");
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

    }
}
