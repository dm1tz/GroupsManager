using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam.Integration;
using ArchiSteamFarm.Steam;
using GMStrings = GroupsManager.Localization.Strings;
using SteamKit2;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System;
using static ArchiSteamFarm.Steam.Integration.ArchiWebHandler;
using SteamKit2.Internal;

namespace GroupsManager.Handlers;

internal static class GroupHandler {
	private static bool JoinGroupChat => GroupsManagerPlugin.Config?.JoinGroupChat ?? GroupsManagerConfig.DefaultJoinGroupChat;
	private static bool LeaveGroupChat => GroupsManagerPlugin.Config?.LeaveGroupChat ?? GroupsManagerConfig.DefaultLeaveGroupChat;
	private const ushort MaxGroupsPerAccount = 1000; // https://help.steampowered.com/en/faqs/view/1299-2CF6-A354-9370

	internal static async Task<string> JoinGroup(Bot bot, IReadOnlyCollection<ulong> groupIDs) {
		List<SteamID> myGroupIDs = GetMyGroupIDs(bot.SteamFriends);

		if (myGroupIDs.Count + groupIDs.Count > MaxGroupsPerAccount) {
			return GMStrings.FormatBotGroupLimitExceeded(groupIDs.Count);
		}

		ushort successCount = 0;

		foreach (var groupID in groupIDs) {
			if (myGroupIDs.Contains(groupID)) {
				string? groupName = bot.SteamFriends.GetClanName((SteamID) groupID);
				bot.ArchiLogger.LogGenericWarning(GMStrings.FormatWarningAlreadyJoinedGroup(groupName ?? groupID.ToString(CultureInfo.InvariantCulture)));

				continue;
			}

			bool success = await bot.ArchiWebHandler.JoinGroup(groupID).ConfigureAwait(false);

			if (success) {
				successCount++;

				if (JoinGroupChat) {
					ulong? chatRoomGroupID = await GetChatRoomGroupID(bot.ArchiHandler, groupID).ConfigureAwait(false);

					if (chatRoomGroupID != null) {
						await bot.ArchiHandler.JoinChatRoomGroup(chatRoomGroupID.Value).ConfigureAwait(false);
					}
				}
			}
		}

		return successCount > 0 ? GMStrings.FormatBotJoinGroup(successCount, groupIDs.Count) : Strings.WarningFailed;
	}

	internal static async Task<string> LeaveGroup(Bot bot, IReadOnlyCollection<ulong> groupIDs) {
		List<SteamID> myGroupIDs = GetMyGroupIDs(bot.SteamFriends);

		if (myGroupIDs.Count == 0) {
			return string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(myGroupIDs));
		}

		ushort successCount = 0;

		foreach (var groupID in groupIDs) {
			if (!myGroupIDs.Contains(groupID)) {
				string? groupName = bot.SteamFriends.GetClanName((SteamID) groupID);
				bot.ArchiLogger.LogGenericWarning(GMStrings.FormatWarningAlreadyLeftGroup(groupName ?? groupID.ToString(CultureInfo.InvariantCulture)));

				continue;
			}

			bool success = await LeaveGroup(bot.ArchiWebHandler, groupID).ConfigureAwait(false);

			if (success) {
				successCount++;

				if (LeaveGroupChat) {
					ulong? chatRoomGroupID = await GetChatRoomGroupID(bot.ArchiHandler, groupID).ConfigureAwait(false);

					if (chatRoomGroupID != null) {
						await bot.ArchiHandler.LeaveChatRoomGroup(chatRoomGroupID.Value).ConfigureAwait(false);
					}
				}
			}
		}

		return successCount > 0 ? GMStrings.FormatBotLeaveGroup(successCount, groupIDs.Count) : Strings.WarningFailed;
	}

	internal static async Task<string> LeaveAllGroups(Bot bot) {
		List<SteamID> myGroupIDs = GetMyGroupIDs(bot.SteamFriends);

		if (myGroupIDs.Count == 0) {
			return string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(myGroupIDs));
		}

		ushort successCount = 0;

		foreach (var myGroupID in myGroupIDs) {
			bool success = await LeaveGroup(bot.ArchiWebHandler, myGroupID).ConfigureAwait(false);

			if (success) {
				successCount++;

				if (LeaveGroupChat) {
					ulong? chatRoomGroupID = await GetChatRoomGroupID(bot.ArchiHandler, myGroupID).ConfigureAwait(false);

					if (chatRoomGroupID != null) {
						await bot.ArchiHandler.LeaveChatRoomGroup(chatRoomGroupID.Value).ConfigureAwait(false);
					}
				}
			}
		}

		return successCount > 0 ? GMStrings.FormatBotLeaveGroup(successCount, myGroupIDs.Count) : Strings.WarningFailed;
	}

	internal static async Task<string> JoinChatRoomGroup(Bot bot, IReadOnlyCollection<ulong> groupIDs) {
		ChatRoomHandler? chatRoomHandler = bot.GetHandler<ChatRoomHandler>();

		if (chatRoomHandler == null) {
			throw new InvalidOperationException(nameof(chatRoomHandler));
		}

		HashSet<ulong>? myChatRoomGroupIDs = await chatRoomHandler.GetMyChatRoomGroupIDs().ConfigureAwait(false);

		if (myChatRoomGroupIDs == null) {
			return string.Format(CultureInfo.CurrentCulture, Strings.ErrorObjectIsNull, nameof(myChatRoomGroupIDs));
		}

		ushort successCount = 0;

		foreach (var groupID in groupIDs) {
			ulong? chatRoomGroupID = await GetChatRoomGroupID(bot.ArchiHandler, groupID).ConfigureAwait(false);

			if (chatRoomGroupID == null) {
				bot.ArchiLogger.LogGenericError(string.Format(CultureInfo.CurrentCulture, Strings.ErrorObjectIsNull, $"{nameof(chatRoomGroupID)} {(groupID)}"));

				continue;
			}

			if (myChatRoomGroupIDs.Contains(chatRoomGroupID.Value)) {
				string? groupName = bot.SteamFriends.GetClanName((SteamID) groupID);
				bot.ArchiLogger.LogGenericWarning(GMStrings.FormatWarningAlreadyJoinedGroupChat(groupName ?? groupID.ToString(CultureInfo.InvariantCulture)));

				continue;
			}

			bool success = await bot.ArchiHandler.JoinChatRoomGroup(chatRoomGroupID.Value).ConfigureAwait(false);

			if (success) {
				successCount++;
			}
		}

		return successCount > 0 ? GMStrings.FormatBotJoinGroupChat(successCount, groupIDs.Count) : Strings.WarningFailed;
	}

	internal static async Task<string> LeaveChatRoomGroup(Bot bot, IReadOnlyCollection<ulong> groupIDs) {
		ChatRoomHandler? chatRoomHandler = bot.GetHandler<ChatRoomHandler>();

		if (chatRoomHandler == null) {
			throw new InvalidOperationException(nameof(chatRoomHandler));
		}

		HashSet<ulong>? myChatRoomGroupIDs = await chatRoomHandler.GetMyChatRoomGroupIDs().ConfigureAwait(false);

		if (myChatRoomGroupIDs == null) {
			return string.Format(CultureInfo.CurrentCulture, Strings.ErrorObjectIsNull, nameof(myChatRoomGroupIDs));
		}

		if (myChatRoomGroupIDs.Count == 0) {
			return string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(myChatRoomGroupIDs));
		}

		ushort successCount = 0;

		foreach (var groupID in groupIDs) {
			ulong? chatRoomGroupID = await GetChatRoomGroupID(bot.ArchiHandler, groupID).ConfigureAwait(false);

			if (chatRoomGroupID == null) {
				bot.ArchiLogger.LogGenericError(string.Format(CultureInfo.CurrentCulture, Strings.ErrorObjectIsNull, $"{nameof(chatRoomGroupID)} {(groupID)}"));

				continue;
			}

			if (!myChatRoomGroupIDs.Contains(chatRoomGroupID.Value)) {
				string? groupName = bot.SteamFriends.GetClanName((SteamID) groupID);
				bot.ArchiLogger.LogGenericWarning(GMStrings.FormatWarningAlreadyLeftGroupChat(groupName ?? groupID.ToString(CultureInfo.InvariantCulture)));

				continue;
			}

			bool success = await bot.ArchiHandler.LeaveChatRoomGroup(chatRoomGroupID.Value).ConfigureAwait(false);

			if (success) {
				successCount++;
			}
		}

		return successCount > 0 ? GMStrings.FormatBotLeaveGroupChat(successCount, groupIDs.Count) : Strings.WarningFailed;
	}

	internal static async Task<string> LeaveAllChatRoomGroups(Bot bot) {
		ChatRoomHandler? chatRoomHandler = bot.GetHandler<ChatRoomHandler>();

		if (chatRoomHandler == null) {
			throw new InvalidOperationException(nameof(chatRoomHandler));
		}

		HashSet<ulong>? myChatGroupIDs = await chatRoomHandler.GetMyChatRoomGroupIDs().ConfigureAwait(false);

		if (myChatGroupIDs == null) {
			return string.Format(CultureInfo.CurrentCulture, Strings.ErrorObjectIsNull, nameof(myChatGroupIDs));
		}

		if (myChatGroupIDs.Count == 0) {
			return string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(myChatGroupIDs));
		}

		ushort successCount = 0;

		foreach (ulong groupID in myChatGroupIDs) {
			ulong? chatRoomGroupID = await GetChatRoomGroupID(bot.ArchiHandler, groupID).ConfigureAwait(false);

			if (chatRoomGroupID == null) {
				bot.ArchiLogger.LogGenericTrace(string.Format(CultureInfo.InvariantCulture, Strings.WarningFailedWithError, $"{nameof(chatRoomGroupID)} {(groupID)}: {chatRoomGroupID}"));

				continue;
			}

			bool success = await bot.ArchiHandler.LeaveChatRoomGroup(chatRoomGroupID.Value).ConfigureAwait(false);

			if (success) {
				successCount++;
			}
		}

		return successCount > 0 ? GMStrings.FormatBotLeaveGroupChat(successCount, myChatGroupIDs.Count) : Strings.WarningFailed;
	}

	private static async Task<bool> LeaveGroup(ArchiWebHandler archiWebHandler, ulong groupID) {
		if (!new SteamID(groupID).IsClanAccount) {
			throw new ArgumentOutOfRangeException(nameof(groupID));
		}

		string? profileURL = await archiWebHandler.GetAbsoluteProfileURL().ConfigureAwait(false);

		if (string.IsNullOrEmpty(profileURL)) {
			return false;
		}

		Uri request = new(SteamCommunityURL, $"{profileURL}/friends/action");
		Uri referer = new(SteamCommunityURL, $"{profileURL}/groups/");

		Dictionary<string, string> headers = new(1, StringComparer.Ordinal) {
			{ "X-Requested-With", "XMLHttpRequest" }
		};

		// Extra entry for sessionID
		Dictionary<string, string> data = new(4, StringComparer.Ordinal) {
			{ "ajax", "1" },
			{ "action", "leave_group" },
			{ "steamids[]", groupID.ToString(CultureInfo.InvariantCulture) }
		};

		return await archiWebHandler.UrlPostWithSession(request, headers, data, referer, session: ESession.Lowercase).ConfigureAwait(false);
	}

	// https://github.com/SteamRE/SteamKit/issues/87
	private static List<SteamID> GetMyGroupIDs(SteamFriends steamFriends) {
		int groupsCount = steamFriends.GetClanCount();

		List<SteamID> groupIDs = new(groupsCount);

		for (int i = 0; i < groupsCount; i++) {
			groupIDs.Add(steamFriends.GetClanByIndex(i));
		}

		return groupIDs;
	}

	private static async Task<ulong?> GetChatRoomGroupID(ArchiHandler archiHandler, ulong groupID) {
		CClanChatRooms_GetClanChatRoomInfo_Response? clanChatRoomInfo = await archiHandler.GetClanChatRoomInfo(groupID).ConfigureAwait(false);

		if ((clanChatRoomInfo == null) || (clanChatRoomInfo.chat_group_summary.chat_group_id == 0)) {
			return null;
		}

		return clanChatRoomInfo.chat_group_summary.chat_group_id;
	}
}
