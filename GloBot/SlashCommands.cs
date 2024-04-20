using AngleSharp.Dom;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.EventHandling;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;

namespace DiscordBotTest {
    internal class SlashCommands : ApplicationCommandModule {
        [SlashCommand("ping", "Responds with pong!")]
        public async Task PingCommand(InteractionContext ctx) => await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Pong!"));

        [SlashCommand("help", "Get a list of commands")]
        public async Task HelpCommand(InteractionContext ctx, [Option("command", "The specific command you want help with")] string command = "") {
            if (DataHolder.DevModeEnabled && !DataHolder.AuthorizedServers.Contains(ctx.Guild.Id)) {
                DataHolder.ShowDevMessage(ctx);
                return;
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder {
                Color = DiscordColor.Lilac,
                Author = new DiscordEmbedBuilder.EmbedAuthor() {
                    Name = "Help with Glo",
                    IconUrl = Program.Client.CurrentUser.AvatarUrl,
                    Url = Program.Client.CurrentUser.AvatarUrl
                }
            };

            DiscordEmbedBuilder allCommandsEmbed = new DiscordEmbedBuilder {
                Color = DiscordColor.Lilac,
                Author = new DiscordEmbedBuilder.EmbedAuthor() {
                    Name = "All Glo Commands",
                    IconUrl = Program.Client.CurrentUser.AvatarUrl,
                    Url = Program.Client.CurrentUser.AvatarUrl
                }
            };

            embed.AddField("Configuring Glo", "Coming soon!");
            embed.AddField("Getting Started", "To get started, simply use the ``/play`` command with the name or link of your chosen song!");
            embed.AddField("All Commands", "Still need a full list? click the \"View All Commands\" button below.");

            allCommandsEmbed.AddField("Admin Commands", "``" + string.Join("`` ``", DataHolder.AdminCommands.ToArray()) + "``");
            allCommandsEmbed.AddField("General Commands", "``" + string.Join("`` ``", DataHolder.GeneralCommands.ToArray()) + "``");

            if (command != "" && command != null) {
                if (!DataHolder.FullCommandList.Contains(command)) {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent("That command does not exist!")
                        .AsEphemeral(true));
                    return;
                }

                CommandDescription commandDescription = DataHolder.CommandDescriptions[command];
                string arguments = "";
                if (commandDescription.CommandArguments != null) {
                    foreach (KeyValuePair<string, string> arg in commandDescription.CommandArguments) {
                        arguments += arg.Key + " ❗\n> " + arg.Value;
                    }
                } else {
                    arguments = "This command has no arguments!";
                }
                string requirements = "";
                foreach (string req in commandDescription.CommandRequirements) {
                    requirements += "- " + req + "\n";
                }

                var allCommandsId = "view_all_commands_" + Guid.NewGuid().ToString();

                ctx.Client.ComponentInteractionCreated += async (client, e) => {
                    if (e.Id == allCommandsId) {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                            .AddEmbed(allCommandsEmbed)
                            .AsEphemeral(true));
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                    }
                };

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .AddEmbed(new DiscordEmbedBuilder {
                        Color = DiscordColor.Lilac,
                        Author = new DiscordEmbedBuilder.EmbedAuthor() {
                            Name = $"Glo",
                            IconUrl = Program.Client.CurrentUser.AvatarUrl,
                            Url = Program.Client.CurrentUser.AvatarUrl
                        },
                        Title = $"**{command}**"
                    }
                    .AddField("Arguments", arguments)
                    .AddField("Requirements", requirements)
                    ).AddComponents(new DiscordComponent[] {
                        new DiscordButtonComponent(ButtonStyle.Success, allCommandsId, "View All Commands"),
                        new DiscordLinkButtonComponent("https://glo-bot-site.vercel.app/", "Dashboard"),
                        new DiscordLinkButtonComponent("https://discord.com/oauth2/authorize?client_id=1229730016359878727&permissions=2721115200&scope=bot", "Invite Me")
                    }));

                return;
            }

            var viewAllCommandsId = "view_all_commands_" + Guid.NewGuid().ToString();

            var viewAllCommandsButton = new DiscordButtonComponent(ButtonStyle.Success, viewAllCommandsId, "View All Commands");
            var dashboardLinkButton = new DiscordLinkButtonComponent("https://glo-bot-site.vercel.app/", "Dashboard");
            var inviteMeLinkButton = new DiscordLinkButtonComponent("https://discord.com/oauth2/authorize?client_id=1229730016359878727&permissions=2721115200&scope=bot", "Invite Me");

            ctx.Client.ComponentInteractionCreated += async (client, e) => {
                if (e.Id == viewAllCommandsId) {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .AddEmbed(allCommandsEmbed)
                        .AsEphemeral(true));
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                }
            };

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .AddEmbed(embed)
                .AddComponents(new DiscordComponent[] {
                    viewAllCommandsButton,
                    dashboardLinkButton,
                    inviteMeLinkButton
                }));
        }

        [SlashCommand("join", "Joins your current voice channel")]
        public async Task JoinCommand(InteractionContext ctx) {
            if (DataHolder.DevModeEnabled && !DataHolder.AuthorizedServers.Contains(ctx.Guild.Id)) {
                DataHolder.ShowDevMessage(ctx);
                return;
            }

            // Get the user's current voice channel
            DiscordChannel targetChannel;
            try {
                targetChannel = ctx.Member.VoiceState.Channel;
            }
            catch (Exception e) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You are not currently in a voice channel."));
                return;
            }
            DataHolder.UpdateLastSpokenChannel(ctx.Channel);
            // Connect to the voice channel
            await targetChannel.ConnectAsync();
            // Send a message stating the bot has joined the voice channel
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"Joined ``{targetChannel.Name}``!"));
        }

        [SlashCommand("leave", "Leaves the current voice channel")]
        public async Task LeaveCommand(InteractionContext ctx) {
            if (DataHolder.DevModeEnabled && !DataHolder.AuthorizedServers.Contains(ctx.Guild.Id)) {
                DataHolder.ShowDevMessage(ctx);
                return;
            }

            if (Program.Client.GetVoiceNext().GetConnection(ctx.Guild) == null) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("I am not currently in a voice channel."));
                return;
            }

            DiscordChannel channel = null;
            try {
                channel = ctx.Member.VoiceState.Channel;
            }
            catch (Exception e) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                   new DiscordInteractionResponseBuilder().WithContent("You are not currently in my voice channel."));
                return;
            }
            if (channel != Program.Client.GetVoiceNext().GetConnection(ctx.Guild).TargetChannel) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You are not currently in a voice channel."));
                return;
            }
            DataHolder.UpdateLastSpokenChannel(ctx.Channel);

            Program.Client.GetVoiceNext().GetConnection(ctx.Guild).Dispose();
            QueueManager.ClearQueue(ctx.Guild);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"Left ``{channel.Name}``."));
        }

        [SlashCommand("play", "Play a song!")]
        public async Task PlayCommand(InteractionContext ctx, [Option("query", "Query to search")] string query) {
            if (DataHolder.DevModeEnabled && !DataHolder.AuthorizedServers.Contains(ctx.Guild.Id)) {
                DataHolder.ShowDevMessage(ctx);
                return;
            }

            DiscordChannel channel = null;
            try {
                channel = ctx.Member.VoiceState.Channel;
            }
            catch (Exception e) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                   new DiscordInteractionResponseBuilder().WithContent("You are not currently in a voice channel."));
                return;
            }

            if (Program.Client.GetVoiceNext().GetConnection(ctx.Guild) == null) {
                // Connect to the voice channel
                await channel.ConnectAsync();
            }

            if (channel != Program.Client.GetVoiceNext().GetConnection(ctx.Guild).TargetChannel) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You are not currently in my voice channel."));
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"Fetching, this may take a moment..."));
            DataHolder.UpdateLastSpokenChannel(ctx.Channel);

            try {
                if (query.StartsWith("https://www.youtube.com/watch?v=")
                || query.StartsWith("http://www.youtube.com/watch?v=")
                || query.StartsWith("https://youtube.com/watch?v=")
                || query.StartsWith("http://youtube.com/watch?v=")) {

                    ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Added {query} to the queue."));
                    await QueueManager.AddToQueue(ctx, query);
                    return;

                }
                else if (query.StartsWith("https://www.youtube.com/playlist")
                    || query.StartsWith("http://www.youtube.com/playlist")
                    || query.StartsWith("https://youtube.com/playlist")
                    || query.StartsWith("http://youtube.com/playlist")) {

                    bool shouldPlayAfter = QueueManager.GetQueue(ctx.Guild).Count == 0;

                    var youtube = new YoutubeClient();
                    var videos = await youtube.Playlists.GetVideosAsync(query);
                    int amt = 0;
                    foreach (PlaylistVideo video in videos) {
                        QueueManager.AddToQueue(ctx, video.Url.Split('&')[0], true);
                        amt++;
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Added {amt}/{videos.Count} songs..."));
                    }

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Added all videos from {query} to the queue."));
                    if (shouldPlayAfter) await QueueManager.PlayQueue(ctx.Guild, ctx.Channel);
                    return;

                }
                else {

                    YouTubeSearch.VideoSearch items = new YouTubeSearch.VideoSearch();
                    List<YouTubeSearch.VideoSearchComponents> list = await items.GetVideos(query, 1);
                    string url = list.First().getUrl();

                    ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Added {url} to the queue"));
                    await QueueManager.AddToQueue(ctx, url);
                    return;

                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong!"));
                return;
            }
        }

        [SlashCommand("stop", "Stop the songs.")]
        public async Task StopCommand(InteractionContext ctx) {
            if (DataHolder.DevModeEnabled && !DataHolder.AuthorizedServers.Contains(ctx.Guild.Id)) {
                DataHolder.ShowDevMessage(ctx);
                return;
            }

            if (Program.Client.GetVoiceNext().GetConnection(ctx.Guild) == null) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("I am not currently in a voice channel."));
                return;
            }

            DiscordChannel channel = null;
            try {
                channel = ctx.Member.VoiceState.Channel;
            }
            catch (Exception e) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                   new DiscordInteractionResponseBuilder().WithContent("You are not currently in my voice channel."));
                return;
            }
            if (channel != Program.Client.GetVoiceNext().GetConnection(ctx.Guild).TargetChannel) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You are not currently in a voice channel."));
                return;
            }
            DataHolder.UpdateLastSpokenChannel(ctx.Channel);

            AudioManager.CancelStream(ctx.Guild);
            QueueManager.ClearQueue(ctx.Guild); await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("🔴 **Stopped the music!**"));
        }

        [SlashCommand("queue", "List the current queue")]
        public async Task QueueCommand(InteractionContext ctx) {
            if (DataHolder.DevModeEnabled && !DataHolder.AuthorizedServers.Contains(ctx.Guild.Id)) {
                DataHolder.ShowDevMessage(ctx);
                return;
            }

            if (Program.Client.GetVoiceNext().GetConnection(ctx.Guild) == null) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("I am not currently in a voice channel."));
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"Fetching, this may take a moment..."));

            string final = "";
            List<VideoData> queue = QueueManager.GetQueue(ctx.Guild);
            List<List<VideoData>> paginatedQueue = QueueManager.GetPaginatedQueue(ctx.Guild);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder {
                Color = DiscordColor.Lilac,
                Author = new DiscordEmbedBuilder.EmbedAuthor() {
                    Name = $"Queue for {ctx.Guild.Name} ({queue.Count} songs)",
                    IconUrl = Program.Client.CurrentUser.AvatarUrl,
                    Url = Program.Client.CurrentUser.AvatarUrl
                }
            };

            var tempMsgBuilder = new DiscordMessageBuilder();

            if (queue.Count == 0) {
                embed.AddField("Now Playing", "Nothing is playing.", false);
                embed.AddField("Up Next", "Nothing is in the queue. Use ``/play`` to get the party started!");

                await ctx.DeleteResponseAsync();
                tempMsgBuilder.AddEmbed(embed);
                await tempMsgBuilder.SendAsync(ctx.Channel);
                return;
            } else if (queue.Count == 1) {
                embed.AddField("Now Playing", $"[{queue[0].Title}]({queue[0].Url}) - {AbstractFunctions.FormatSeconds((float)queue[0].Duration)}", false);
                embed.AddField("Up Next", "Nothing is in the queue. Use ``/play`` to get the party started!");

                await ctx.DeleteResponseAsync();
                tempMsgBuilder.AddEmbed(embed);
                await tempMsgBuilder.SendAsync(ctx.Channel);
                return;
            }

            var interactivity = ctx.Client.GetInteractivity();

            var ytdl = new YoutubeDL();
            for (int i = 0; i < paginatedQueue[0].Count; i++) {
                final += $"{i+1}. [{paginatedQueue[0][i].Title}]({paginatedQueue[0][i].Url}) - {AbstractFunctions.FormatSeconds((float)paginatedQueue[0][i].Duration)}\n";
            }

            embed.AddField("• Now Playing", $"[{queue[0].Title}]({queue[0].Url}) - {AbstractFunctions.FormatSeconds((float)queue[0].Duration)}", false);
            embed.AddField("• Up Next", $"{final}");

            var backButtonGuid = Guid.NewGuid().ToString();
            var forwardButtonGuid = Guid.NewGuid().ToString();
            var backButtonId = "back_button_" + backButtonGuid;
            var forwardButtonId = "forward_button_" + forwardButtonGuid;

            var backButton = new DiscordButtonComponent(ButtonStyle.Primary, backButtonId, "◀️");
            var forwardButton = new DiscordButtonComponent(ButtonStyle.Primary, forwardButtonId, "▶️");

            await ctx.DeleteResponseAsync();
            var builder = new DiscordMessageBuilder()
                .AddEmbed(embed)
                .AddComponents(new DiscordComponent[] {
                    backButton,
                    forwardButton
                });
            var msg = await builder.SendAsync(ctx.Channel);

            int page = 0;
            int waiter = 120;

            ctx.Client.ComponentInteractionCreated += async (client, e) => {
                if (waiter == 0) return;
                if (e.Id == backButtonId) {
                    if (page != 0) {
                        page -= 1;
                        final = "";
                        for (int i = 0; i < paginatedQueue[page].Count; i++) {
                            final += $"{i + 1 + (page * 5)}. [{paginatedQueue[page][i].Title}]({paginatedQueue[page][i].Url}) - {AbstractFunctions.FormatSeconds((float)paginatedQueue[page][i].Duration)}\n";
                        }

                        embed.ClearFields();
                        embed.AddField("• Now Playing", $"[{queue[0].Title}]({queue[0].Url}) - {AbstractFunctions.FormatSeconds((float)queue[0].Duration)}", false);
                        embed.AddField("• Up Next", $"{final}");

                        builder = new DiscordMessageBuilder()
                            .AddEmbed(embed)
                            .AddComponents(new DiscordComponent[] {
                                backButton,
                                forwardButton
                            });
                        await msg.ModifyAsync(builder);
                    }
                } else if (e.Id == forwardButtonId) {
                    if (page != paginatedQueue.Count-1) {
                        page += 1;
                        final = "";
                        for (int i = 0; i < paginatedQueue[page].Count; i++) {
                            final += $"{i + 1 + (page*5)}. [{paginatedQueue[page][i].Title}]({paginatedQueue[page][i].Url}) - {AbstractFunctions.FormatSeconds((float)paginatedQueue[page][i].Duration)}\n";
                        }

                        embed.ClearFields();
                        embed.AddField("• Now Playing", $"[{queue[0].Title}]({queue[0].Url}) - {AbstractFunctions.FormatSeconds((float)queue[0].Duration)}", false);
                        embed.AddField("• Up Next", $"{final}");

                        builder = new DiscordMessageBuilder()
                            .AddEmbed(embed)
                            .AddComponents(new DiscordComponent[] {
                                backButton,
                                forwardButton
                            });
                        await msg.ModifyAsync(builder);
                    }
                }
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            };

            while (waiter > 0) {
                await Task.Delay(1000);
                waiter -= 1;
            }
        }

        [SlashCommand("skip", "Skip the current song")]
        public async Task SkipCommand(InteractionContext ctx) {
            if (DataHolder.DevModeEnabled && !DataHolder.AuthorizedServers.Contains(ctx.Guild.Id)) {
                DataHolder.ShowDevMessage(ctx);
                return;
            }

            if (Program.Client.GetVoiceNext().GetConnection(ctx.Guild) == null) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("I am not currently in a voice channel."));
                return;
            }

            DiscordChannel channel;
            try {
                channel = ctx.Member.VoiceState.Channel;
            }
            catch (Exception e) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                   new DiscordInteractionResponseBuilder().WithContent("You are not currently in my voice channel."));
                return;
            }
            if (channel != Program.Client.GetVoiceNext().GetConnection(ctx.Guild).TargetChannel) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You are not currently in a voice channel."));
                return;
            }
            DataHolder.UpdateLastSpokenChannel(ctx.Channel);

            if (QueueManager.GetQueue(ctx.Guild).Count == 0) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("There is nothing in the queue!"));
                if (QueueManager.voteSkips.ContainsKey(ctx.Guild))
                    QueueManager.voteSkips.Remove(ctx.Guild);
                return;
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder {
                Color = DiscordColor.Lilac,
                Title = $"Skip Vote"
            };

            if (!QueueManager.voteSkips.ContainsKey(ctx.Guild)) {
                QueueManager.voteSkips.Add(ctx.Guild, 0);
            }
            QueueManager.voteSkips[ctx.Guild] += 1;

            if (QueueManager.voteSkips[ctx.Guild] == channel.Users.Count-1) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("⏩ **Skipping song**"));
                AudioManager.CancelStream(ctx.Guild);
            }
            else {
                embed.AddField("Votes", $"{QueueManager.voteSkips[ctx.Guild]}/{channel.Users.Count-1}", false);
                embed.AddField("Wanna vote to skip?", "Use ``/skip`` to cast your vote!", false);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                   new DiscordInteractionResponseBuilder().AddEmbed(embed));
            }
        }

        [SlashCommand("forceskip", "Force skips the current song")]
        public async Task ForceSkipCommand(InteractionContext ctx) {
            if (DataHolder.DevModeEnabled && !DataHolder.AuthorizedServers.Contains(ctx.Guild.Id)) {
                DataHolder.ShowDevMessage(ctx);
                return;
            }

            if (Program.Client.GetVoiceNext().GetConnection(ctx.Guild) == null) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("I am not currently in a voice channel."));
                return;
            }
            if (!ctx.Member.Permissions.HasPermission(Permissions.MuteMembers)) {
                bool allowed = false;
                foreach (DiscordRole role in ctx.Member.Roles) {
                    if (role.Name.ToUpper() == "DJ") {
                        allowed = true;
                        break;
                    }
                }
                if (!allowed) {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent("You can't do this! You either need the ``Mute Members`` permission or a role named \"DJ\"!")
                        .AsEphemeral(true));
                    return;
                }
            }

            DataHolder.UpdateLastSpokenChannel(ctx.Channel);
            if (QueueManager.GetQueue(ctx.Guild).Count > 0) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("⏩ **Skipping song**"));
                AudioManager.CancelStream(ctx.Guild);
            }
            else {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("There is nothing in the queue!"));
            }
        }

        [SlashCommand("remove", "Remove a song from the queue")]
        public async Task RemoveCommand(InteractionContext ctx, [Option("index", "Index of the song you wish to remove")] long index) {
            if (DataHolder.DevModeEnabled && !DataHolder.AuthorizedServers.Contains(ctx.Guild.Id)) {
                DataHolder.ShowDevMessage(ctx);
                return;
            }

            if (Program.Client.GetVoiceNext().GetConnection(ctx.Guild) == null) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("I am not currently in a voice channel."));
                return;
            }

            DataHolder.UpdateLastSpokenChannel(ctx.Channel);
            if ((int)index == 0) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"Song at position {index} does not exist."));
                return;
            }

            if (QueueManager.GetQueue(ctx.Guild).Count > 0 && QueueManager.GetQueue(ctx.Guild).Count >= index) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"Removed {QueueManager.GetQueue(ctx.Guild)[(int)index].Title} from the queue."));
                QueueManager.RemoveFromQueue(ctx.Guild, (int)index);
            }
            else if (QueueManager.GetQueue(ctx.Guild).Count > 0) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"Song at position {index} does not exist."));
            }
            else {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("There is nothing in the queue!"));
            }
        }

        [SlashCommand("pause", "Pause the song")]
        public async Task PauseCommand(InteractionContext ctx) {
            if (DataHolder.DevModeEnabled && !DataHolder.AuthorizedServers.Contains(ctx.Guild.Id)) {
                DataHolder.ShowDevMessage(ctx);
                return;
            }

            if (Program.Client.GetVoiceNext().GetConnection(ctx.Guild) == null) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("I am not currently in a voice channel."));
                return;
            }
            DataHolder.UpdateLastSpokenChannel(ctx.Channel);
            VoiceNextConnection sink = Program.Client.GetVoiceNext().GetConnection(ctx.Guild);
            if (!QueueManager.paused.Contains(ctx.Guild)) {
                sink.Pause();
                QueueManager.paused.Add(ctx.Guild);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("⏸️ Paused the song!"));
            } else {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("There is nothing playing!"));
            }
        }

        [SlashCommand("resume", "Pause the song")]
        public async Task ResumeCommand(InteractionContext ctx) {
            if (DataHolder.DevModeEnabled && !DataHolder.AuthorizedServers.Contains(ctx.Guild.Id)) {
                DataHolder.ShowDevMessage(ctx);
                return;
            }

            if (Program.Client.GetVoiceNext().GetConnection(ctx.Guild) == null) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("I am not currently in a voice channel."));
                return;
            }
            DataHolder.UpdateLastSpokenChannel(ctx.Channel);
            VoiceNextConnection sink = Program.Client.GetVoiceNext().GetConnection(ctx.Guild);
            if (QueueManager.paused.Contains(ctx.Guild)) {
                await sink.ResumeAsync();
                QueueManager.paused.Remove(ctx.Guild);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("▶️ Resumed the song!"));
            }
            else {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("I'm already playing!"));
            }
        }

        [SlashCommand("clearqueue", "Clear the queue")]
        public async Task ClearQueueCommand(InteractionContext ctx) {
            if (DataHolder.DevModeEnabled && !DataHolder.AuthorizedServers.Contains(ctx.Guild.Id)) {
                DataHolder.ShowDevMessage(ctx);
                return;
            }

            if (Program.Client.GetVoiceNext().GetConnection(ctx.Guild) == null) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("I am not currently in a voice channel."));
                return;
            }

            DiscordChannel channel;
            try {
                channel = ctx.Member.VoiceState.Channel;
            }
            catch (Exception e) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                   new DiscordInteractionResponseBuilder().WithContent("You are not currently in my voice channel."));
                return;
            }
            if (channel != Program.Client.GetVoiceNext().GetConnection(ctx.Guild).TargetChannel) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You are not currently in a voice channel."));
                return;
            }
            DataHolder.UpdateLastSpokenChannel(ctx.Channel);
            if (QueueManager.GetQueue(ctx.Guild).Count == 0) {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("There is nothing in the queue!"));
                if (QueueManager.voteSkips.ContainsKey(ctx.Guild))
                    QueueManager.voteSkips.Remove(ctx.Guild);
                return;
            }

            AudioManager.CancelStream(ctx.Guild);
            QueueManager.ClearQueue(ctx.Guild);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Cleared the queue."));
        }
    }
}
