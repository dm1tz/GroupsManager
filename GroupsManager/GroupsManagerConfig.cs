using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GroupsManager;

[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "The class is used during json deserialization")]
internal sealed class GroupsManagerConfig {
	internal const bool DefaultJoinGroupChat = true;
	internal const bool DefaultLeaveGroupChat = true;

	[JsonInclude]
	internal bool JoinGroupChat { get; private init; } = DefaultJoinGroupChat;

	[JsonInclude]
	internal bool LeaveGroupChat { get; private init; } = DefaultLeaveGroupChat;

	[JsonConstructor]
	private GroupsManagerConfig() { }
}
