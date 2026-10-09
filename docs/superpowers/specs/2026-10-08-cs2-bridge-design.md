# Cities Skylines II bridge adaptation

Adapt LancerComet's existing bridge so Codex can inspect and play a running Steam copy of Cities: Skylines II on this PC. Keep the work in a personal GitHub fork owned by `R3dTh3Ging3r`, with small, independently reviewable improvements suitable for upstream contribution.

The user approved adapting this project on October 8, 2026. This design covers installation, connection, and a first supervised play session. Compatibility must be demonstrated against the installed game before claiming that gameplay works.

## Scope and success

The first milestone is a working connection that reads a loaded city and controls pause and speed. The second is a small construction exercise in a disposable city: build a connected road, zone an adjacent area, run a bounded simulation interval, and inspect the result. The user can watch the game and interrupt the session.

Use the existing bridge and tool names. New construction systems, terraforming, remote hosting, a separate agent application, and unattended sessions are outside this adaptation. A successful setup may require documentation and configuration only; code changes must address a reproduced compatibility or behavior problem.

## Components and data flow

```text
Codex tool call
    -> local Node MCP server over standard input/output
    -> HTTP at 127.0.0.1:8642
    -> C# mod inside Cities Skylines II
    -> game request queue and handlers
    -> result returned to Codex
```

The existing [HTTP server](https://github.com/LancerComet/cities-skylines-2-mcp/blob/master/CS2MCP.Bridge/HttpBridgeServer.cs) binds to loopback and forwards game requests to a queue. The [bridge system](https://github.com/LancerComet/cities-skylines-2-mcp/blob/master/CS2MCP.Bridge/BridgeSystem.cs) processes that queue during game updates and contains timed auto-pause behavior. Preserve these boundaries when making fixes.

Use structured city data for decisions and occasional screenshots for spatial checks. Send mutations sequentially, observe their results, and then choose the next action. This reduces desktop interactions; it does not promise a particular response time or faster simulation.

## Local installation and Codex connection

The workspace is `D:\Codex-Projects\Mods-For-Games\CitySkylines_2`. Steam's manifest identifies the expected game directory as `E:\SteamLibrary\steamapps\common\Cities Skylines II`. At design time, `Cities2_Data\Managed\Game.dll` is absent. Recheck installation completion and record the game build before compiling.

The machine has .NET SDK 10.0.401 and Node 24.15.0. Upstream's [C# project](https://github.com/LancerComet/cities-skylines-2-mcp/blob/master/CS2MCP.Bridge/CS2MCP.Bridge.csproj) targets .NET Framework 4.7.2 and references assemblies from the game installation. It supports `SkipDeploy=true`; use that for initial compilation. Deploy the resulting mod only after the build passes, with the game closed, preserving any existing local mod installation for rollback. Game libraries stay on this PC.

Build the [Node server](https://github.com/LancerComet/cities-skylines-2-mcp/blob/master/mcp-server/package.json) using the committed dependency lockfile. Register its compiled entry point as a local MCP process with an explicit working directory and loopback bridge URL. Preserve unrelated Codex configuration. Prefer project-scoped configuration when supported by the active trusted workspace; otherwise add only the named server entry to user configuration.

[Codex supports local standard-input/output MCP servers and tool allowlists](https://learn.chatgpt.com/docs/extend/mcp). Initially expose city inspection, screenshots, camera inspection, and simulation control. Add construction tools for the test-city exercise after the connection milestone passes. Registration is complete only when tools are available in a live Codex session; a saved configuration file alone is insufficient. Restart the connection or session if required by the installed client.

## Play session and recovery

Start with a dedicated test city. Read the game mode, city overview, and current simulation state before any change. Verify pause and normal speed by reading state back. Test a short timed run and confirm that the game pauses afterward.

Before construction, create a separately named checkpoint and verify the save exists or is acknowledged as completed by the game. Select unlocked roads and zones from the game's available definitions. Inspect existing roads and terrain, build one short connecting segment, and verify connectivity and zoning by reading the resulting entities and inspecting a screenshot. Use normal game validation and costs.

The initial stop procedure is to interrupt the Codex turn and pause the game manually. An already dispatched action may still complete, and interruption is not a guaranteed cancellation mechanism. After an interruption or timeout, inspect state before sending another mutation. Never blindly retry construction or saving after an uncertain response. Do not claim a dedicated emergency-stop control exists.

Connection failure, no city loaded, placement rejection, and an uncertain action result must remain distinguishable in reports. If compilation fails against current game libraries, investigate and fix the specific mismatch. If a connection problem occurs, inspect the mod log and bridge response before changing configuration.

Keep a concise local session record of actions, outcomes, and the tested game and bridge revisions. Avoid continuous screenshot capture and preserve the loopback-only listener.

## Validation and acceptance

| Stage | Required evidence |
| --- | --- |
| Build | Node compilation passes; C# compilation passes against the installed game without automatic deployment. |
| Tool connection | A live Codex session lists bridge tools and receives a valid ping result. |
| City inspection | City name and population agree with the loaded test city; a screenshot shows its current view. |
| Simulation | Pause and normal speed read back correctly; a short timed run ends paused. |
| Construction | One connected road and an adjacent zoned area are visible and returned by game queries. |
| Recovery | A separately named checkpoint is verified; interruption and uncertain-response procedures are documented. |

Record each stage as passed, failed, or not run, with the reason. A successful build cannot substitute for live game validation. For actual bug fixes, add focused regression coverage where practical and repeat the affected game check. Documentation-only changes need review rather than new automated tests.

## Fork and contribution workflow

Create or reuse `R3dTh3Ging3r/cities-skylines-2-mcp` as a personal fork of [LancerComet/cities-skylines-2-mcp](https://github.com/LancerComet/cities-skylines-2-mcp). Retain upstream history and attribution, with `origin` pointing to the personal fork and `upstream` to the original. Inspect contribution instructions again when importing the source. Commit this design after importing upstream history instead of creating an unrelated repository history for the document.

Preserve upstream's license and existing notices. Keep generated outputs, dependencies, local configuration, logs, screenshots, saves, and proprietary game assemblies out of commits. Use separate branches for independently useful patches. Keep personal play notes and machine-specific settings out of proposed upstream changes.

Potential contributions include a verified Codex setup guide or fixes for problems found during build and gameplay. Each proposal should describe the observed problem, the change, and actual validation results. The maintainer decides whether to merge it. This design does not send an issue, comment, or pull request to the maintainer.
