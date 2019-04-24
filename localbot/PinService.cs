using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;

namespace localbot
{
    public class PinService
    {
        public const string PinEmote = "\U0001f4cc";
        
        public PinService(IServiceProvider services)
        {
            var discord = services.GetRequiredService<DiscordSocketClient>();
            discord.ReactionAdded += ReactionAddedAsync;
            // discord.ReactionRemoved += ReactionRemovedAsync;
        }
        
        public async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!(channel is SocketGuildChannel)) return;
            if (reaction.Emote.Name == PinEmote)
            {
                var message = await cachedMessage.GetOrDownloadAsync();
                if (!message.IsPinned)
                {
                    await message.PinAsync();
                }
            }
        }

        // Currently is not enabled because there is no way to check if the message was actually pinned by the bot and not by a moderator or something. 
        public async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!(channel is SocketGuildChannel)) return;
            var message = await cachedMessage.GetOrDownloadAsync();
            if (message.IsPinned && reaction.Emote.Name == PinEmote)
            {
                await message.UnpinAsync();
            }
        }
    }
}