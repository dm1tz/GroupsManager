using ArchiSteamFarm.Core;
using ArchiSteamFarm.Helpers.Json;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using GroupsManager.Handlers;
using SteamKit2;
using System.Collections.Generic;
using System.ComponentModel;
using System.Composition;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace GroupsManager;

[Export(typeof(IPlugin))]
internal sealed class GroupsManagerPlugin : IPlugin, IASF, IBotSteamClient, IBotCommand2 {
	public string Name => nameof(GroupsManagerPlugin);
	public Version Version => typeof(GroupsManagerPlugin).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	internal static GroupsManagerConfig? Config { get; private set; }

	public Task OnASFInit(IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null) {
		if (additionalConfigProperties == null) {
			return Task.CompletedTask;
		}

		GroupsManagerConfig? config = null;

		foreach ((string configProperty, JsonElement configValue) in additionalConfigProperties) {
			try {
				if (configProperty == nameof(GroupsManagerConfig)) {
					config = configValue.ToJsonObject<GroupsManagerConfig>();
				}
			} catch (Exception e) {
				ASF.ArchiLogger.LogGenericException(e);
				return Task.CompletedTask;
			}
		}

		Config = config;

		return Task.CompletedTask;
	}

	public Task OnBotSteamCallbacksInit(Bot bot, CallbackManager callbackManager) {
		ArgumentNullException.ThrowIfNull(bot);
		ArgumentNullException.ThrowIfNull(callbackManager);

		return Task.CompletedTask;
	}

	public Task<IReadOnlyCollection<ClientMsgHandler>?> OnBotSteamHandlersInit(Bot bot) {
		ArgumentNullException.ThrowIfNull(bot);

		SteamUnifiedMessages? steamUnifiedMessages = bot.GetHandler<SteamUnifiedMessages>();

		if (steamUnifiedMessages == null) {
			throw new InvalidOperationException(nameof(steamUnifiedMessages));
		}

		return Task.FromResult<IReadOnlyCollection<ClientMsgHandler>?>(new HashSet<ClientMsgHandler>(1) {
				new ChatRoomHandler(bot.ArchiLogger, steamUnifiedMessages)});
	}

	public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0) {
		ArgumentNullException.ThrowIfNull(bot);

		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(message);

		if ((args == null) || (args.Length == 0)) {
			throw new ArgumentNullException(nameof(args));
		}

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		return await Commands.OnBotCommand(bot, access, message, args, steamID).ConfigureAwait(false);
	}

	public Task OnLoaded() => Task.CompletedTask;
}
