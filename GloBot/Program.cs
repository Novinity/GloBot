using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using GloBot.config;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;

namespace GloBot {
    internal class Program {
        public static DiscordClient Client { get; private set; }

        static async Task Main(string[] args) {
            JSONReader jsonReader = new JSONReader();
            await jsonReader.ReadJSON();

            var discordConfig = new DiscordConfiguration() {
                Intents = DiscordIntents.All,
                Token = jsonReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            Client = new DiscordClient(discordConfig);
            var slash = Client.UseSlashCommands();
            slash.RegisterCommands<SlashCommands>();
            Client.UseVoiceNext();

            slash.SlashCommandErrored += Slash_SlashCommandErrored;
            Client.GuildDownloadCompleted += Client_GuildDownloadCompleted;
            Client.GuildCreated += Client_GuildCreated;
            Client.VoiceStateUpdated += Client_VoiceStateUpdated;
            Client.Ready += Client_Ready;

            await Client.ConnectAsync();

            await Task.Delay(-1);
        }

        private static async Task Client_VoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs args) {
            DiscordChannel channel = args.Before?.Channel;
            DiscordGuild guild = args.Guild;

            if (Client.GetVoiceNext().GetConnection(guild) == null) return;
            if (Client.GetVoiceNext().GetConnection(guild).TargetChannel != channel) return;

            if (channel.Users.Count == 1) {
                int counter = 300;
                while (counter > 0 && channel.Users.Count == 1) {
                    await Task.Delay(1000);
                    counter--;
                }

                if (channel.Users.Count == 1) {
                    Client.GetVoiceNext().GetConnection(guild).Dispose();
                    QueueManager.ClearQueue(guild);

                    if (DataHolder.GetLastSpokenChannel(guild) != null) {
                        await new DiscordMessageBuilder()
                            .WithEmbed(new DiscordEmbedBuilder() {
                                Color = DiscordColor.Lilac,
                                Author = new DiscordEmbedBuilder.EmbedAuthor() {
                                    Name = "Everybody's gone!",
                                    IconUrl = Client.CurrentUser.AvatarUrl,
                                    Url = Client.CurrentUser.AvatarUrl
                                },
                                Description = $"{channel.Name} has been empty for 5 minutes, so I decided to head out."
                            }).SendAsync(DataHolder.GetLastSpokenChannel(guild));
                    }
                }
            }
        }

        private static Task Client_GuildCreated(DiscordClient sender, GuildCreateEventArgs args) {
            QueueManager.CreateQueue(args.Guild);
            return Task.CompletedTask;
        }

        private static Task Client_GuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs args) {
            foreach (KeyValuePair<ulong, DiscordGuild> entry in sender.Guilds) {
                QueueManager.CreateQueue(entry.Value);
            }
            return Task.CompletedTask;
        }

        private static Task Slash_SlashCommandErrored(SlashCommandsExtension sender, DSharpPlus.SlashCommands.EventArgs.SlashCommandErrorEventArgs args) {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }

        private static Task Client_Ready(DiscordClient sender, ReadyEventArgs args) {
            sender.UpdateStatusAsync(new DiscordActivity("some tunes!", ActivityType.ListeningTo));

            Console.WriteLine(Client.CurrentUser.Username + '#' + Client.CurrentUser.Discriminator + " is ready!");
            return Task.CompletedTask;
        }
    }
}
