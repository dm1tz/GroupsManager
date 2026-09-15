using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm;
using GroupsManager.Handlers;
using Interaction = ArchiSteamFarm.Steam.Interaction;
using SteamKit2;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace GroupsManager;

internal static class Commands {
	internal static bool JoinGroupChat => GroupsManagerPlugin.Config?.JoinGroupChat ?? GroupsManagerConfig.DefaultJoinGroupChat;
	internal static bool LeaveGroupChat => GroupsManagerPlugin.Config?.LeaveGroupChat ?? GroupsManagerConfig.DefaultLeaveGroupChat;

	internal static async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0) {
		switch (args.Length) {
			case 1:
				switch (args[0].ToUpperInvariant()) {
					case "LEAVEALLGROUPS" or "LAG":
						return await ResponseLeaveAllGroups(access, bot).ConfigureAwait(false);
					case "LEAVEALLGROUPCHATS" or "LAGC":
						return await ResponseLeaveAllGroupChats(access, bot).ConfigureAwait(false);
					case "GROUPMANAGERVERSION" or "GMV":
						return ResponseVersion(access);
					default:
						return null;
				}
			default:
				switch (args[0].ToUpperInvariant()) {
					case "JOINGROUP" or "JG" when args.Length > 2:
						return await ResponseJoinGroup(access, args[1], Utilities.GetArgsAsText(message, 2), steamID).ConfigureAwait(false);
					case "LEAVEGROUP" or "LG" when args.Length > 2:
						return await ResponseLeaveGroup(access, args[1], Utilities.GetArgsAsText(message, 2), steamID).ConfigureAwait(false);
					case "LEAVEALLGROUPS" or "LAG":
						return await ResponseLeaveAllGroups(access, Utilities.GetArgsAsText(args, 1, ","), steamID).ConfigureAwait(false);
					case "JOINGROUP" or "JG":
						return await ResponseJoinGroup(access, bot, args[1]).ConfigureAwait(false);
					case "LEAVEGROUP" or "LG":
						return await ResponseLeaveGroup(access, bot, args[1]).ConfigureAwait(false);
					case "JOINGROUPCHAT" or "JGC" when args.Length > 2:
						return await ResponseJoinGroupChat(access, args[1], Utilities.GetArgsAsText(message, 2), steamID).ConfigureAwait(false);
					case "LEAVEGROUPCHAT" or "LGC" when args.Length > 2:
						return await ResponseLeaveGroupChat(access, args[1], Utilities.GetArgsAsText(message, 2), steamID).ConfigureAwait(false);
					case "LEAVEALLGROUPCHATS" or "LAGC":
						return await ResponseLeaveAllGroupChats(access, Utilities.GetArgsAsText(args, 1, ","), steamID).ConfigureAwait(false);
					case "JOINGROUPCHAT" or "JGC":
						return await ResponseJoinGroupChat(access, bot, args[1]).ConfigureAwait(false);
					case "LEAVEGROUPCHAT" or "LGC":
						return await ResponseLeaveGroupChat(access, bot, args[1]).ConfigureAwait(false);
					default:
						return null;
				}
		}
	}

	private async static Task<string?> ResponseJoinGroup(EAccess access, Bot bot, string groupIDsText) {
		ArgumentException.ThrowIfNullOrEmpty(groupIDsText);

		if (access < EAccess.Master) {
			return access > EAccess.None ? bot.Commands.FormatBotResponse(Strings.ErrorAccessDenied) : null;
		}

		HashSet<ulong>? groupIDs = ParseGroupIDs(groupIDsText);

		if ((groupIDs == null) || (groupIDs.Count == 0)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(groupIDs)));
		}

		string result = await GroupHandler.JoinGroup(bot, groupIDs).ConfigureAwait(false);

		return bot.Commands.FormatBotResponse(result);
	}

	private static async Task<string?> ResponseJoinGroup(EAccess access, string botNames, string groupIDsText, ulong steamID = 0) {
		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Owner ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseJoinGroup(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot, groupIDsText))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}

	private async static Task<string?> ResponseLeaveGroup(EAccess access, Bot bot, string groupIDsText) {
		ArgumentException.ThrowIfNullOrEmpty(groupIDsText);

		if (access < EAccess.Master) {
			return access > EAccess.None ? bot.Commands.FormatBotResponse(Strings.ErrorAccessDenied) : null;
		}

		HashSet<ulong>? groupIDs = ParseGroupIDs(groupIDsText);

		if ((groupIDs == null) || (groupIDs.Count == 0)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(groupIDs)));
		}

		string result = await GroupHandler.LeaveGroup(bot, groupIDs).ConfigureAwait(false);

		return bot.Commands.FormatBotResponse(result);
	}

	private static async Task<string?> ResponseLeaveGroup(EAccess access, string botNames, string groupIDsText, ulong steamID = 0) {
		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Owner ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => Task.Run(() => ResponseLeaveGroup(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot, groupIDsText)))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}

	private async static Task<string?> ResponseLeaveAllGroups(EAccess access, Bot bot) {
		if (access < EAccess.Master) {
			return access > EAccess.None ? bot.Commands.FormatBotResponse(Strings.ErrorAccessDenied) : null;
		}

		string result = await GroupHandler.LeaveAllGroups(bot).ConfigureAwait(false);

		return bot.Commands.FormatBotResponse(result);
	}

	private static async Task<string?> ResponseLeaveAllGroups(EAccess access, string botNames, ulong steamID = 0) {
		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Owner ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => Task.Run(() => ResponseLeaveAllGroups(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot)))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}

	private async static Task<string?> ResponseJoinGroupChat(EAccess access, Bot bot, string groupIDsText) {
		ArgumentException.ThrowIfNullOrEmpty(groupIDsText);

		if (access < EAccess.Master) {
			return access > EAccess.None ? bot.Commands.FormatBotResponse(Strings.ErrorAccessDenied) : null;
		}

		HashSet<ulong>? groupIDs = ParseGroupIDs(groupIDsText);

		if ((groupIDs == null) || (groupIDs.Count == 0)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(groupIDs)));
		}

		string result = await GroupHandler.JoinChatRoomGroup(bot, groupIDs).ConfigureAwait(false);

		return bot.Commands.FormatBotResponse(result);
	}

	private static async Task<string?> ResponseJoinGroupChat(EAccess access, string botNames, string groupIDsText, ulong steamID = 0) {
		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Owner ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => Task.Run(() => ResponseJoinGroupChat(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot, groupIDsText)))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}

	private async static Task<string?> ResponseLeaveGroupChat(EAccess access, Bot bot, string groupIDsText) {
		ArgumentException.ThrowIfNullOrEmpty(groupIDsText);

		if (access < EAccess.Master) {
			return access > EAccess.None ? bot.Commands.FormatBotResponse(Strings.ErrorAccessDenied) : null;
		}

		HashSet<ulong>? groupIDs = ParseGroupIDs(groupIDsText);

		if ((groupIDs == null) || (groupIDs.Count == 0)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(groupIDs)));
		}

		string result = await GroupHandler.LeaveChatRoomGroup(bot, groupIDs).ConfigureAwait(false);

		return bot.Commands.FormatBotResponse(result);
	}

	private static async Task<string?> ResponseLeaveGroupChat(EAccess access, string botNames, string groupIDsText, ulong steamID = 0) {
		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Owner ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => Task.Run(() => ResponseLeaveGroupChat(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot, groupIDsText)))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}

	private async static Task<string?> ResponseLeaveAllGroupChats(EAccess access, Bot bot) {
		if (access < EAccess.Master) {
			return access > EAccess.None ? bot.Commands.FormatBotResponse(Strings.ErrorAccessDenied) : null;
		}

		string result = await GroupHandler.LeaveAllChatRoomGroups(bot).ConfigureAwait(false);

		return bot.Commands.FormatBotResponse(result);
	}

	private static async Task<string?> ResponseLeaveAllGroupChats(EAccess access, string botNames, ulong steamID = 0) {
		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Owner ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => Task.Run(() => ResponseLeaveAllGroupChats(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot)))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}

	private static string? ResponseVersion(EAccess access) {
		return access >= EAccess.FamilySharing ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotVersion, nameof(GroupsManagerPlugin), typeof(GroupsManagerPlugin).Assembly.GetName().Version)) : null;
	}

	private static HashSet<ulong>? ParseGroupIDs(string groupIDsText) {
		string[] groupIDsArgs = groupIDsText.Split(SharedInfo.ListElementSeparators, StringSplitOptions.RemoveEmptyEntries);

		HashSet<ulong> groupIDs = [];

		foreach (string groupIDArg in groupIDsArgs) {
			if (!ulong.TryParse(groupIDArg, out ulong groupID) || (groupID == 0)) {
				return null;
			}

			groupIDs.Add(groupID);
		}

		return groupIDs;
	}
}
