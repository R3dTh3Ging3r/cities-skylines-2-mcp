# Connect Cities Skylines II to Codex on Windows

CS2MCP exposes game tools through a local Node process. Codex communicates with that process over standard input/output; the process calls the game mod at `http://127.0.0.1:8642`. The HTTP address is the game bridge, not an MCP server URL.

## Build

Install the Steam game completely before building the C# mod. Use a current supported Node release and a .NET SDK compatible with the upstream build requirements. The setup was checked with Node 24.15.0 and .NET SDK 10.0.401 present. No separate OpenAI API key is required by this bridge.

From the repository root in PowerShell:

```powershell
Push-Location mcp-server
npm ci --ignore-scripts
npm run build
Pop-Location

# Replace the example path with your Steam installation directory.
dotnet build CS2MCP.Bridge/CS2MCP.Bridge.csproj -c Release -p:SkipDeploy=true '-p:GamePath=E:\SteamLibrary\steamapps\common\Cities Skylines II'
```

Check each command succeeds before continuing. `SkipDeploy=true` builds without copying files into the game's Mods directory. The C# build requires `Cities2_Data\Managed\Game.dll` and the other referenced game libraries. A partially downloaded installation cannot supply a valid build environment.

The C# project targets .NET Framework 4.7.2 and restores its reference assemblies through NuGet. It does not need the game's libraries copied into this repository. Keep those libraries out of source control.

## Install the mod

Close the game. If `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\CS2MCP` already exists, copy it to a backup outside the game's Mods directory first. From the successful Release output (normally `CS2MCP.Bridge\bin\Release`), copy `CS2MCP.dll` and its optional PDB to that Mods folder. Verify the copied files match the build outputs. Launch the game afterward.

For rollback, close the game, remove only the installed CS2MCP files, and restore the previous folder from the backup if one existed. Leave other mods alone.

## Register the tools

Use [the configuration example](examples/codex-mcp.toml). Replace both checkout paths and the Node executable path with values on your PC. Merge the `mcp_servers.cs2` tables into a trusted project's `.codex/config.toml`, or into your user Codex configuration when project scope is unavailable. Preserve unrelated entries. Do not commit machine configuration.

The working directory matters because the upstream server loads `.env` relative to that directory. The explicit loopback URL avoids relying on an unrelated environment file. The initial allowlist includes inspection and simulation controls; pause and speed tools change the running game.

Check the registration from that project:

```powershell
codex mcp get cs2 --json
```

Restart the MCP connection or reopen the Codex session if newly added tools do not appear. A configuration listing confirms registration, not a live game connection. Verify `cs2_ping` from the actual Codex session after starting the game.

The supported configuration options are described in the [official Codex MCP documentation](https://learn.chatgpt.com/docs/extend/mcp).

## Verify a test city

1. Call `cs2_ping`. At the main menu, it can report liveness without a loaded city. Check `cs2_game_state` before attempting city actions.
2. Load a disposable city. Read `cs2_city_overview` and compare the city name and population with a screenshot from `cs2_screenshot`.
3. Call `cs2_set_simulation` with `paused=true` and read state back. Set `speed=1`, verify it, then call `cs2_run_simulation` with `hours=0.1` and `speed=1`. It returns a target immediately; read state at reasonable intervals until it reports paused again.
4. For construction testing, add the required tools to the same `enabled_tools` list: `cs2_save_game`, `cs2_find_prefabs`, `cs2_prefab_info`, `cs2_terrain`, `cs2_tiles_info`, `cs2_list_map_tiles`, `cs2_road_graph`, `cs2_list_roads`, `cs2_connect_road`, `cs2_list_zones`, `cs2_zone_area`, and `cs2_zoning`. Refresh the connection and inspect their live schemas.
5. Create a separately named checkpoint and verify save completion. Discover available unlocked roads and zones, inspect terrain and owned tiles, connect one short road to the existing graph, and zone an adjacent area. Verify the graph connection, resulting entities, and screenshot. Run another short simulation interval and inspect the outcome.

Send game changes sequentially. A request timeout does not prove that an action was cancelled: the upstream queue may still process it. Inspect the city before retrying a road, zoning operation, or save. A save request acknowledgement alone is not proof that the save finished writing.

To stop a session, interrupt Codex and pause the game manually. A dispatched action may still finish. This setup has no dedicated emergency-stop button or guaranteed cancellation mechanism.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| C# build cannot find the game | Verify Steam finished installing and the supplied path contains `Cities2_Data\Managed\Game.dll`. |
| Codex does not list the tools | Verify the built entry point and Node executable exist, the project configuration is trusted, and the connection has restarted. |
| Ping says the bridge cannot be reached | Start the game with the compiled mod installed; check `Logs\CS2MCP.log` under the game's LocalLow data directory. |
| Ping works but city queries fail | Wait for loading to finish, load a city, and check game mode. |
| Listener cannot start | Check port 8642 for an existing listener. If changing ports, set `CS2MCP_PORT` for the game and match `CS2_BRIDGE_URL` in the server configuration. |
| A mutation times out | Treat the outcome as uncertain and inspect game state before another mutation. |

To disable the connection, set `enabled=false` in the `mcp_servers.cs2` table and restart the connection. To remove it, remove only that table and its `env` subtable. This stops future calls after the connection closes; it does not cancel an action already queued inside the game.

## Validation status on October 8 2026

Node compilation, MCP initialization, discovery of 65 tools, and the structured error for an unavailable game bridge were verified on Node 24.15.0. All 22 tool names used in the setup and construction checklist were present. A clean locked install and build passed after refreshing dependencies; `npm audit` reported zero vulnerabilities. NuGet dependency restore also passed.

The Steam game libraries were not yet installed during setup. C# compilation, mod deployment, live Codex-to-game calls, saving, simulation control, and construction remain unverified. Update this status after performing those checks; the offline MCP check is not evidence of gameplay compatibility.
