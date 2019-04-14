﻿using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Discord.WebSocket;
using System.IO;
using Newtonsoft.Json;
using System;

namespace localbot
{
    public class AnonModule : ModuleBase<SocketCommandContext>
    {
        // thanks for review:)

        // NOTE: Config file in project for editing is not the file that is read, that is in a different place
        private static ConfigJSON _config = 
            JsonConvert.DeserializeObject<ConfigJSON>(System.IO.File.ReadAllText(@"C:\Users\Owen\Desktop\config.txt"));
        private class ConfigJSON
        {
            public int cooldown;
            public int hist_leng;
            public int max_id;
            public ulong server_id;
        }


        private static Dictionary<ulong, int> _blacklist = 
            JsonConvert.DeserializeObject<Dictionary<ulong, int>>(System.IO.File.ReadAllText(@"C:\Users\Owen\Desktop\blacklist.txt"));

        // The number of IDs that are tracked to a user's profile
        public static int historyLength = (int) _config.hist_leng;
        // The max number a user's ID can be
        public static int maxID = (int) _config.max_id;
        // The ammount of time that users must wait before using newID again
        private TimeSpan cooldown = new TimeSpan(0, (int) _config.cooldown, 00);
        // The Server ID that this instance is talkin to
        private ulong serverID = (ulong) _config.server_id;

        private class AnonUser
        {
            public ulong user; // Unique ID assigned by discord
            private List<int> ids; // History of anon IDs this user has aliased as
            public DateTime lastNewID; // Last time this user ran >newID
            public bool timeout; // If this user is timed out
            public DateTime timeoutEnd; // The time they are "back in"

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
                if(_blacklist == null)
                {
                    _blacklist = new Dictionary<ulong, int>();
                }
                return _blacklist.ContainsKey(this.user);
            }

            // Returns true if this user is timed out
            public bool IsTimedout()
            {
                return timeout != false ? timeoutEnd - DateTime.Now > new TimeSpan(0, 0, 0) : false;
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
                ids.Insert(0, alias);
            }

        }

        private static Random random = new Random();

        // This list contains the current active users AnonUser
        private static List<AnonUser> activeUsers = new List<AnonUser>();

        // Doxes an anon user (principly for moderation)
        [Command(">dox")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task doxUser([Remainder] int num) {
            await ReplyAsync($"user `{num}` is {Context.Client.GetUser(_blacklist.First(x => x.Value == num).Key).Username}");
        }

        // Changes the cooldown on newID
        [Command(">newid_cooldown")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task cooldownChange([Remainder] int num)
        {
            cooldown = new TimeSpan(0, num, 0);
            await ReplyAsync($"newID cooldown set to {num} minutes");
        }

        // Blacklists a user using their ID
        // Checks their ID history so they can not "roll away"
        [Command(">blacklist")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Blacklist(int num)
        {
            if(_blacklist == null)
            {
                _blacklist = new Dictionary<ulong, int>();
            }

            foreach(AnonUser u in activeUsers)
            {
                if(u.AliasAs(num))
                {
                    _blacklist.Add(u.user, num);
                    System.IO.File.WriteAllText(@"C:\Users\Owen\Desktop\blacklist.txt", JsonConvert.SerializeObject(_blacklist));
                    u.NewAlias(num);
                    await ReplyAsync($"user {num} was blacklisted.");
                    return;
                }
            }
            return;
        }

        // Temporarily mutes a user's anon capabilities. Takes
        // an anon id and the ammount of minutes they should be 
        // timed out for
        [Command(">timeout")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Timeout(int num, int minutes)
        {
            foreach (AnonUser u in activeUsers)
            {
                if (u.AliasAs(num))
                {
                    u.timeout = true;
                    u.timeoutEnd = DateTime.Now + new TimeSpan(0, minutes, 0);
                    u.NewAlias(num);
                    await ReplyAsync($"user {num} for {minutes} minute(s)");
                    return;
                }
            }
            return;
        }

        // Takes a discord user and removes them from the blacklist
        // Will also end a user's timeout
        [Command(">unblacklist")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task UnBlacklist(int num)
        {
            if(GetUser(num).IsTimedout())
            {
                GetUser(num).timeout = false;
                GetUser(num).timeoutEnd = DateTime.Now;
            }
            if (GetUser(num).IsBlacklisted())
            {
                _blacklist.Remove(GetUser(num).user);
                System.IO.File.WriteAllText(@"C:\Users\Owen\Desktop\blacklist.txt", JsonConvert.SerializeObject(_blacklist));
                await ReplyAsync($"user unblacklisted");
            } else
            {
                await ReplyAsync($"user was not blacklisted to begin with");
            }
            return;
        }

        // Generates a newID for the anonUser who executes the command.
        // This override allows the user to specify their id
        [Command(">newid")]
        public async Task NewID([Remainder] int num)
        {
            AnonUser cur_user;
            if (GetUser(Context.User.Id) == null) // Does the user have an AnonUser profile?
            {
                activeUsers.Add(new AnonUser(Context.User.Id));
                GetUser(Context.User.Id).lastNewID = DateTime.Now - (cooldown * 2);
            }
            cur_user = GetUser(Context.User.Id);
            if (cur_user.IsBlacklisted()) // Is this profile blacklisted?
            {
                await (Context.User).SendMessageAsync($"you are blacklisted");
                return;
            }
            if (cur_user.IsTimedout()) // Is this profile timed out?
            {
                await (Context.User).SendMessageAsync($"you are timed out");
                return;
            }
            if (DateTime.Now - cur_user.lastNewID < cooldown) // Is newID on cooldown?
            {
                await (Context.User).SendMessageAsync($"newID is on cooldown, wait {(cooldown - (DateTime.Now - GetUser(Context.User.Id).lastNewID)).ToString()}");
                return;
            }
            if ((RecentlyUsed(num) || num < 0 || num > maxID)) // Is this ID taken or out of bounds?
            {
                await (Context.User).SendMessageAsync($"{num} is either taken or out of acceptable range");
                return;
            }

            cur_user.NewAlias(num);
            cur_user.lastNewID = DateTime.Now;

            await (Context.User).SendMessageAsync($"you are now speaking under id: `{num}`");
            return;
        }

        // Generates a newID for the anonUser who executes the command.
        [Command(">newid")]
        public async Task NewID()
        {
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

            await NewID(num);
            return;
        }

        // Sends a message to the anonChannel from a user's perspective
        [Command(">anon")]
        public async Task Anon([Remainder] string text)
        {
            await SendMessage(text, "anon");
            return;
        }

        // Sends a message to the relChannel from a user's perspective
        [Command(">relationships")]
        public async Task Relationships([Remainder] string text)
        {
            await SendMessage(text, "relationships");
            return;
        }

        // Messages another AnonUser
        // Takes the id number of the user to message and the text you
        // want to send to them
        [Command(">message")]
        public async Task Message(int num, [Remainder] string text)
        {
            await SendMessage(text, "message", num);
            return;
        }

        // Takes int num (the sender's AnonID), string text (the context of the message), 
        // string where ("anon", or "message") and optional parameter recipient id (used 
        // for sending messages);
        private async Task SendMessage(string text, string where, int recipient = 0)
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

            text = text.Replace("@everyone", "@\u200beveryone");
            text = text.Replace("@here", "@\u200bhere");

            // Keeping this here incase we decide to switch to embeds
            var message = new EmbedBuilder
            {
                Title = current_id.ToString(),
                Description = text
            };

            switch (where)
            {
                case "message":
                    await (Context.Client.GetUser(GetUser(recipient).user))
                        .SendMessageAsync($"`{current_id}:` {text}");
                    break;
                case "anon":
                    await (Context.Client.GetGuild(serverID).TextChannels.FirstOrDefault<SocketTextChannel>(textchannel => textchannel.Name == "anonymous"))
                        .SendMessageAsync($"`{current_id}:` {text}");
                    break;
                case "relationships":
                    await (Context.Client.GetGuild(serverID).TextChannels.FirstOrDefault<SocketTextChannel>(textchannel => textchannel.Name == "relationships"))
                        .SendMessageAsync($"`{current_id}:` {text}");
                    break;
                default:
                    break;
            }

        }

        // Takes an int id and returns if any AnonUsers
        // have used the id recently (with in the historyLength)
        private bool RecentlyUsed(int id)
        {
            var u = activeUsers.Find(i => i.AliasAs(id) == true);
            return u != null;
        }
        
        // Takes a unsigned long for the user's unique discord id
        // returns their AnonUser profile or null if it is not in
        // the current list of active users
        private AnonUser GetUser(ulong user)
        {
            var u = activeUsers.Find(i => i.user == user);
            return u;
        }

        // Takes the int id and returns the id of the current user of 
        // the id or null if nobody is using it
        private AnonUser GetUser(int id)
        {
            var u = activeUsers.Find(i => i.getID() == id);
            return u;
        }
    }
}
