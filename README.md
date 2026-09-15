## Configuration
```json
{
   ...
  "JoinGroupChat": true,
  "LeaveGroupChat": true,
}
```

### `JoinGroupChat`
`bool` type with the default value of `true`.
This property defines whether to subsequently join group chat on successful group join issued by `joingroup` command.

### `LeaveGroupChat`
`bool` type with the default value of `true`.
This property defines whether to subsequently leave group chat on successful group leave issued by `leavegroup` or `leaveallgroups` command.

---

## Commands
Command | Alias | Access | Description
--- | ---| --- | ---
`joingroup [Bots] <GroupIDs>` | `jg` | `Master`| Joins specified Steam groups on given bot instances.
`leavegroup [Bots] <GroupIDs>` | `lg` | `Master` | Leaves specified Steam groups on given bot instances.
`leaveallgroups [Bots]` | `lag` | `Master` | Leaves all Steam groups of given bot instances.
`joingroupchat [Bots] <GroupIDs>` | `jgc` | `Master`| Joins specified Steam group chats on given bot instances.
`leavegroupchat [Bots] <GroupIDs>` | `lgc` | `Master` | Leaves specified Steam group chats on given bot instances.
`leaveallgroupchats [Bots]` | `lagc` | `Master` | Leaves all Steam group chats of given bot instances.
`groupmanagerversion` | `gmv` | `FamilySharing` | Prints an actual version of plugin.
