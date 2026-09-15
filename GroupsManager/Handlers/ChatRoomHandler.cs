using ArchiSteamFarm.Core;
using ArchiSteamFarm.NLog;
using SteamKit2.Internal;
using SteamKit2;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GroupsManager.Handlers;

internal sealed class ChatRoomHandler : ClientMsgHandler {
	private readonly ArchiLogger ArchiLogger;
	private readonly ChatRoom UnifiedChatRoomService;

	internal ChatRoomHandler(ArchiLogger archiLogger, SteamUnifiedMessages steamUnifiedMessages) {
		ArgumentNullException.ThrowIfNull(archiLogger);
		ArgumentNullException.ThrowIfNull(steamUnifiedMessages);

		ArchiLogger = archiLogger;
		UnifiedChatRoomService = steamUnifiedMessages.CreateService<ChatRoom>();
	}

	public override void HandleMsg(IPacketMsg packetMsg) => ArgumentNullException.ThrowIfNull(packetMsg);

	internal async Task<HashSet<ulong>?> GetMyChatRoomGroupIDs() {
		if (Client == null) {
			throw new InvalidOperationException(nameof(Client));
		}

		if (!Client.IsConnected) {
			return null;
		}

		CChatRoom_GetMyChatRoomGroups_Request request = new();

		SteamUnifiedMessages.ServiceMethodResponse<CChatRoom_GetMyChatRoomGroups_Response> response;

		try {
			response = await UnifiedChatRoomService.GetMyChatRoomGroups(request).ToLongRunningTask().ConfigureAwait(false);
		} catch (Exception e) {
			ArchiLogger.LogGenericWarningException(e);

			return null;
		}

		if (response.Result != EResult.OK) {
			return null;
		}

		return response.Body.chat_room_groups.Select(static chatRoom => chatRoom.group_summary.chat_group_id).ToHashSet();
	}
}
