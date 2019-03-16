using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Discord.WebSocket;
using System.Net.Http;
using System;

namespace localbot
{
    class VeriFiModule : ModuleBase<SocketCommandContext>
    {
        private class VeriFiUser
        {
            public ulong user { get; set; }
            public bool veriFi;

            public VeriFiUser (ulong userID)
            {
                this.user = userID;
            }
            public VeriFiUser() {  }
        }

        private readonly List<VeriFiUser> users = new List<VeriFiUser>();
        private static readonly HttpClient client = new HttpClient();
        
        [Command(">isVeriFi")]
        public async Task Check([Remainder] IGuildUser user)
        {
            foreach(VeriFiUser u in users)
            {
                if (u.user == user.Id)
                {
                    await ReplyAsync($"verification: {u.veriFi}");
                }
            }
        }

        [Command(">verifi")]
        public async Task Verify([Remainder] String credentials)
        {
            VeriFiUser current = new VeriFiUser();
            bool inSystem = false;
            foreach(VeriFiUser u in users)
            {
                if (u.user == Context.User.Id)
                {
                    current = u;
                    inSystem = true;
                }
            }
            if (!inSystem)
            {
                current = new VeriFiUser(Context.User.Id);
                users.Add(current);
            }

            var requestCred = new Dictionary<String, String>();
            String[] cred = credentials.Split();
            requestCred["user"] = cred[0];
            requestCred["password"] = cred[1];
            var content = new FormUrlEncodedContent(requestCred);
            var response = await client.PostAsync("https://uwnetid.washington.edu/session/", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (responseString.Contains("Session key established:")) {
                current.veriFi = true;
                await ReplyAsync($"user verified");
                return;
            } else
            {
                current.veriFi = false;
                await ReplyAsync($"user not verified");
            }

        }
    }
}
