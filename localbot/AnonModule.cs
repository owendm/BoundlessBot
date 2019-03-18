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
        // TODO:
        // - reduce redundancy of checks when executing commands
        // 
        // - fix so not all commands can be executed in any context
        // 
        // - admin role that can be set instead of actual admin perms
        //
        // - command to set the history length and cooldown time for 
        // newID and anonUser history respectivly

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

            // Takes an unsigned long user and returns a AnonUser with 
            // user set to the user
            public AnonUser(ulong user)
            {
                this.user = user;
                ids = new List<int>();
            }

            // Returns the current ID number that the AnonUser has set
            public int getID()
            {
                return ids[0];
            }

            // Returns true if this AnonUser is blacklisted
            public bool IsBlacklisted()
            {
                return blacklisted;
            }

            // Returns true if this user is timed out
            public bool IsTimedout()
            {
                if (timeout == false)
                {
                    return false;
                } else
                {
                    if(timeoutEnd - DateTime.Now > new TimeSpan(0, 0, 0))
                    {
                        return true;
                    } else
                    {
                        return false;
                    }
                }
            }

            // Returns true if this AnonUser has used int id as an
            // id in historyLength ids
            public bool AliasAs(int id)
            {
                return ids.Contains(id);
            }

            // Changes the active ID for this AnonUser and pushes the
            // old ids to the history
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

        // This is the channel that anonymous messages are sent to
        private static SocketTextChannel anon_channel;

        // The ammount of time that users must wait before using newID again
        private TimeSpan cooldown = new TimeSpan(0, 00, 10);

        // This list contains the current active users AnonUser
        private static List<AnonUser> activeUsers = new List<AnonUser>();
        
        // Designates the channel for anon messages to be sent to
        // Can only be executed by administrators
        [Command(">set_anon_channel")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetChannel()
        {
            anon_channel = (Context.Channel as SocketTextChannel);
            await ReplyAsync($"anon channel set");
        }

        // Disables the anon channel
        [Command(">disable_anon_channel")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DisableChannel()
        {
            anon_channel = null;
            await ReplyAsync($"anon channel disabled");
        }

        // Resets the directory of AnonUsers, blacklist will be emptied
        [Command(">reset_anon")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ResetAnon()
        {
            activeUsers = new List<AnonUser>();
            await ReplyAsync($"anon numbers reset");
        }

        // Blacklists a user using their ID
        // Checks their ID history so they can not "roll away"
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

        // Temporarily mutes a user's anon capabilities. Takes
        // an anon id and the ammount of minutes they should be 
        // timed out for
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

        // Takes a discord user and removes them from the blacklist
        [Command(">unblacklist")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task UnBlacklist([Remainder] IGuildUser user)
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

        // Generates a newID for the anonUser who executes the command.
        // This override allows the user to specify their id
        [Command(">newid")]
        public async Task NewID([Remainder] int num)
        {
            if (GetUser(Context.User.Id) == null) // Does the user have an AnonUser profile?
            {
                activeUsers.Add(new AnonUser(Context.User.Id));
                GetUser(Context.User.Id).lastNewID = DateTime.Now - (cooldown * 2);
            }
            if (GetUser(Context.User.Id).IsBlacklisted()) // Is this profile blacklisted?
            {
                await (Context.User).SendMessageAsync($"you are blacklisted");
                return;
            }
            if (GetUser(Context.User.Id).IsTimedout()) // Is this profile timed out?
            {
                await (Context.User).SendMessageAsync($"you are timed out");
                return;
            }
            if (DateTime.Now - GetUser(Context.User.Id).lastNewID < cooldown) // Is newID on cooldown?
            {
                await (Context.User).SendMessageAsync($"newID is on cooldown, wait {(cooldown - (DateTime.Now - GetUser(Context.User.Id).lastNewID)).ToString()}");
                return;
            }

            if ((RecentlyUsed(num) || num < 0 || num > maxID)) // Is this ID taken or out of bounds?
            {
                await (Context.User).SendMessageAsync($"{num} is either taken or out of acceptable range");
                return;
            }
            
            GetUser(Context.User.Id).NewAlias(num);
            GetUser(Context.User.Id).lastNewID = DateTime.Now;

            await (Context.User).SendMessageAsync($"you are now speaking under id: `{num}`");
        }

        // Generates a newID for the anonUser who executes the command.
        [Command(">newid")]
        public async Task NewID()
        {
            if (GetUser(Context.User.Id) == null) // Does the user have an AnonUser profile?
            {
                activeUsers.Add(new AnonUser(Context.User.Id));
                GetUser(Context.User.Id).lastNewID = DateTime.Now - (cooldown * 2);
            }
            if (GetUser(Context.User.Id).IsBlacklisted()) // Is this profile blacklisted?
            {
                await (Context.User).SendMessageAsync($"you are blacklisted");
                return;
            }
            if (GetUser(Context.User.Id).IsTimedout()) // Is this profile timed out?
            {
                await (Context.User).SendMessageAsync($"you are timed out");
                return;
            }
            if (DateTime.Now - GetUser(Context.User.Id).lastNewID < cooldown) // Is newID on cooldown?
            {
                await (Context.User).SendMessageAsync($"newID is on cooldown, wait {(cooldown - (DateTime.Now - GetUser(Context.User.Id).lastNewID)).ToString()}");
                return;
            }

            if (activeUsers.Count / historyLength > maxID) // Are all possible newIDs used up? (to avoid infinite loop)
            {
                await (Context.User).SendMessageAsync($"all IDs are currently in use, contact your mods to reset IDs");
                return;
            }

            int num = random.Next(maxID);
            while(RecentlyUsed(num)) // This should in theory never infinitly loop because of the previous check
            {
                random.Next(maxID);
            }
            
            GetUser(Context.User.Id).NewAlias(num);
            GetUser(Context.User.Id).lastNewID = DateTime.Now;

            await (Context.User).SendMessageAsync($"you are now speaking under id: `{num}`");
        }

        // Sends a message to the anonChannel from a user's perspective
        [Command(">anon")]
        public async Task Anon([Remainder] string text)
        {

            if (GetUser(Context.User.Id) == null) // Does the user have an AnonUser profile?
            {
                // TODO: Just have this generate an ID?
                await (Context.User).SendMessageAsync("please generate an id with `newID`");
                return;
            }
            if (GetUser(Context.User.Id).IsBlacklisted()) // Is this profile blacklisted?
            {
                await (Context.User).SendMessageAsync($"you are blacklisted");
                return;
            }
            if (GetUser(Context.User.Id).IsTimedout()) // Is this profile timed out?
            {
                await (Context.User).SendMessageAsync($"you are timed out");
                return;
            }

            int current_id = GetUser(Context.User.Id).getID();
            await (anon_channel).SendMessageAsync($"`{current_id}:` {text}");
        }

        // Messages another AnonUser
        // Takes the id number of the user to message and the text you
        // want to send to them
        [Command(">message")]
        public async Task Message(int num, [Remainder] string text)
        {

            if (GetUser(Context.User.Id) == null) // Does the user have an AnonUser profile?
            {
                await (Context.User).SendMessageAsync("please generate an id with `newID`");
                return;
            }
            if (GetUser(Context.User.Id).IsBlacklisted()) // Is this profile blacklisted?
            {
                await (Context.User).SendMessageAsync($"you are blacklisted");
                return;
            }
            if (GetUser(Context.User.Id).IsTimedout()) // Is this profile timed out?
            {
                await (Context.User).SendMessageAsync($"you are timed out");
                return;
            }

            int current_id = GetUser(Context.User.Id).getID();
            IGuildUser recipient = anon_channel.GetUser(GetUser(num).user);
            await (recipient).SendMessageAsync($"`{current_id}:` {text}");
        }

        // Takes an int id and returns if any AnonUsers
        // have used the id recently (with in the historyLength)
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

        // Takes a unsigned long for the user's unique discord id
        // returns their AnonUser profile or null if it is not in
        // the current list of active users
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

        // Takes the int id and returns the id of the current user of 
        // the id or null if nobody is using it
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
