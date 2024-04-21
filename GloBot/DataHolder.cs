using System.Collections.Generic;
using System.Linq;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace DiscordBotTest {
    internal class DataHolder {
        public static List<string> AdminCommands = new List<string>() {
            "forceskip"
        };

        public static List<string> GeneralCommands = new List<string>() {
            "join", "leave", "play", "queue", "skip", "stop", "remove", "pause", "resume", "clearqueue"
        };

        public static List<string> FullCommandList = AdminCommands.Concat(GeneralCommands).ToList();
        public static Dictionary<string, CommandDescription> CommandDescriptions = new Dictionary<string, CommandDescription>() {
            { "forceskip", new CommandDescription(null, new[] { "Must be in a voice channel", "A song must be currently playing", "Must be in the same voice channel as me" }) },
            { "join", new CommandDescription(null, new[] { "Must be in a voice channel" }) },
            { "leave", new CommandDescription(null, new[] { "Must be in a voice channel", "Must be in the same voice channel as me" }) },
            { "play", new CommandDescription(new Dictionary<string, string>() { { "query", "The track you want to play. It can either be the name or a song/playlist link!" } }, new[] { "Must be in a voice channel", "Must be in the same voice channel as me" }) },
            { "queue", new CommandDescription(null, new[] { "Must be in a voice channel", "Must be in the same voice channel as me" }) },
            { "skip", new CommandDescription(null, new[] { "Must be in a voice channel", "A song must be currently playing", "Must be in the same voice channel as me" }) },
            { "stop", new CommandDescription(null, new[] { "Must be in a voice channel", "A song must be currently playing", "Must be in the same voice channel as me" }) },
            { "remove", new CommandDescription(new Dictionary<string, string>() { { "track", "The position of the track you want to remove" } }, new[] { "Must be in a voice channel", "The queue must not be empty", "Must be in the same voice channel as me", "Chosen index must be within queue" }) },
            { "pause", new CommandDescription(null, new[] { "Must be in a voice channel", "A song must be currently playing", "Must be in the same voice channel as me" }) },
            { "resume", new CommandDescription(null, new[] { "Must be in a voice channel", "A song must be currently paused", "Must be in the same voice channel as me" }) },
            { "clearqueue", new CommandDescription(null, new[] { "Must be in a voice channel", "The queue must not be empty", "Must be in the same voice channel as me" }) },
        };

        public static Dictionary<DiscordGuild, DiscordChannel> lastSpokenChannels = new Dictionary<DiscordGuild, DiscordChannel>();

        public static List<ulong> AuthorizedServers = new List<ulong>() {
            910992098537402429,
            1214175327597891594,
            1175188756681212017
        };

        public static bool DevModeEnabled = true;

        public static void UpdateLastSpokenChannel(DiscordChannel channel) {
            var guild = channel.Guild;

            if (lastSpokenChannels.ContainsKey(guild))
                lastSpokenChannels.Remove(guild);

            lastSpokenChannels.Add(guild, channel);
        }

        public static DiscordChannel GetLastSpokenChannel(DiscordGuild guild) {
            if (lastSpokenChannels.ContainsKey(guild))
                return lastSpokenChannels[guild];
            return null;
        }

        public static async void ShowDevMessage(InteractionContext ctx) {
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .WithContent("Hey, thanks for adding me! Unfortunately I'm still in development, but don't worry, we'll be up and running soon!\nLearn more here -> https://www.glo-bot.site/"));
        }
    }

    internal struct CommandDescription {
        public Dictionary<string, string> CommandArguments { get; private set; }
        public string[] CommandRequirements { get; private set; }

        public CommandDescription(Dictionary<string, string> commandArguments, string[] commandRequirements) {
            CommandArguments = commandArguments;
            CommandRequirements = commandRequirements;
        }

        public string GetArgumentFormatted(string argument) {
            return "> " + CommandArguments[argument];
        }

        public string GetRequirementFormatted(int index) {
            return "- " + CommandRequirements[index];
        }
    }
}
