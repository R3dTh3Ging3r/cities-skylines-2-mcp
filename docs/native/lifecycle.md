# Lifecycle investigation and fixture preparation

O1 prepares G1 evidence and refuses native dispatch. Every lifecycle probe is
`notRun`; this is not native compatibility or writer support. O2/O3/O5 must supply
real world fencing, completion instrumentation and lease admission before probes
can run. Startup validation metadata grants no authority.

## Runner contract

Explicit collection: `powershell -NoProfile -File scripts/Test-NativeFixture.ps1
-Fixture lifecycle -EvidenceDir .local/v04`. This contacts the configured local
bridge for `/capabilities` and `/state` only. Ordinary build/test commands do not
invoke the collector against the game. Tests pass an ephemeral fake loopback URL
and `-Offline`; that flag labels provenance and **still performs HTTP reads**.
Only `-ValidateEvidence .local/v04/report.json` guarantees no network.

`-AllowMutation` is Collect-only and records operator opt-in. It does not enable
dispatch in O1. For unverified capabilities, future guarded collection requires
both this switch and `CS2MCP_NATIVE_VALIDATION=true` observed in the running mod's
manifest. The mod reads that exact lowercase value once at startup. The fixed
`nativeValidation.probes` are `validationOnly:true`, `support:unverified` and
`dispatchAvailable:false`, even when `enabled:true`. Unsupported operations
remain unsupported. No machine configuration is changed by the runner.

`-EvidenceRoot` is optional, Collect-only and defaults to repository `.local`.
The root and EvidenceDir must both be canonically confined to `.local`, with
EvidenceDir beneath the supplied root. Traversal, existing reparse ancestors and
raw/resolved paths over 240 characters are rejected before HTTP or output writes.
Offline validation confines the report and each artifact to `.local`, with
artifacts also beneath the report directory. Contracts schema-1 reports and all
F6 catalog/provenance/binding/semantic equality checks remain supported.

Lifecycle EvidenceDir must be new or contain **only** operator input
`fixture-context.json`. Prior results, receipts, unknown outcomes or any other
files/directories cause refusal before HTTP/writes. An exclusive `run-start.json`
creation fences concurrent collectors. Original context is retained and copied
verbatim to `fixture-context-snapshot.json`; malformed input is retained too.
Use a new directory for another run. The runner never deletes evidence, restores
a checkpoint or loads a city. The operator performs any manual reset and supplies
fresh context.

Context schema (these values illustrate claims, not observed native identities):

```json
{
  "schemaVersion": 1,
  "fixture": "lifecycle",
  "disposableCityConfirmed": true,
  "world": { "bootId": "observed-boot", "epoch": 1 },
  "city": { "identity": "observed-city-identity" },
  "permittedFixtureScope": ["lifecycle"],
  "checkpoint": {
    "name": "separately-named-baseline",
    "assetIdentity": "verified-destination-package-identity",
    "verified": true,
    "separatelyNamed": true,
    "cityIdentity": "observed-city-identity",
    "world": { "bootId": "observed-boot", "epoch": 1 }
  }
}
```

Missing context, wrong fixture/scope, absent disposable confirmation, wrong
world/city, unverified or mismatched checkpoint produce refusal evidence. Optional
identity claims in a `/state` response detect contradictions only. Neither these
claims nor context assertions establish a fresh producer WorldRef, checkpoint
readback or lease. Report `world.bootId/epoch` stay null until actual producer
evidence exists. Before each future mutation group, compare fresh producer
world/city/checkpoint and lease against context; stop on change or unavailable
evidence. Do not fabricate authority to pass this gate.

`report.json` records common build/source/hash distinctions, checks and artifacts,
plus `nativeOutcome`, `preparation`, `fixtureContext`, `contextArtifact` and
`receipts`. O1 emits `nativeOutcome:notRun`, empty receipts and failed preparation
with `operationGuardsNotImplemented` and
`freshWorldLeaseCheckpointEvidenceUnavailable` refusals. Probe checks stay
`notRun`; successful read collection does not pass a fixture. `refusal.json`
independently preserves preparation/refusal/read-failure evidence and is checked
against the report by the existing validator. A failed or incomplete read stops
collection without application retry. Transport implementations may reconnect
GET reads internally; this is never mutation retry or cancellation evidence.
Any future submitted command with unknown outcome must retain its receipt and
stop the group; an operator reset cannot erase it.

Exit codes: 0 complete evidence, 2 structurally valid but incomplete evidence,
1 malformed evidence or unsafe collector input. Native outcome is independent of
script exit. O1 cannot emit a complete lifecycle result. The named admission
regressions `validation_mode_does_not_bypass_world_or_lease` and
`release_defaults_reject_probe_commands` are explicit pending tests for real
O2/O5 admission, not simulated authorization tests.

## Installed metadata observations

Metadata was inspected with Cecil without invoking/loading Game/Unity runtime
APIs. Selected signatures/IL were reused from the bounded O1 inventory; hashes
refreshed on 2026-10-11 still match:

| Assembly | SHA-256 |
| --- | --- |
| Game.dll | `AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A` |
| Colossal.IO.AssetDatabase.dll | `8D5571FECE816C4E19237A3D8A0A6F477B36B812D3C86EA05B41CD7EFAA65D54` |

Raw reader, selected IL and local paths remain ignored under `.local/o1inv`.
These are source/metadata observations, not runtime callback results.

### Loading hooks

```csharp
protected virtual void Game.GameSystemBase.OnGamePreload(Colossal.Serialization.Entities.Purpose purpose, Game.GameMode mode)
protected virtual void Game.GameSystemBase.OnGameLoaded(Colossal.Serialization.Entities.Context serializationContext)
protected virtual void Game.GameSystemBase.OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, Game.GameMode mode)
bool Game.SceneFlow.GameManager.get_isGameLoading()
void Game.SceneFlow.GameManager.EventGamePreload.Invoke(Colossal.Serialization.Entities.Purpose purpose, Game.GameMode mode)
```

GameSystemBase subscribes wrappers to manager preload/loading-complete events and
the LoadGameSystem save-loaded event. Selected LoadSimulationData IL invokes
preload before cleanup; selected Load state-machine IL invokes loading-complete
before assigning manager ready state. Loading level is not an epoch. Runtime
subscriber order, callback thread, same-save reload cardinality, failure/disposal
overlap and first safe update remain unproved.

### Tools and completion

```csharp
Game.Tools.ToolBaseSystem Game.Tools.ToolSystem.get_activeTool()
void Game.Tools.ToolSystem.set_activeTool(Game.Tools.ToolBaseSystem value)
System.Action<Game.Tools.ToolBaseSystem> Game.Tools.ToolSystem.EventToolChanged // field
protected virtual void Game.Tools.ToolBaseSystem.OnStopRunning()
void Game.UI.InGame.ToolUISystem.SelectTool(Game.Tools.ToolBaseSystem tool)
```

The setter assigns the new tool and synchronously notifies EventToolChanged;
selected IL does not call OnStopRunning. ToolUpdate later disables/updates the
previous tool. StopRunning is an interruption candidate, not rollback proof.
The bounded search found no dedicated stop/cancel method in the selected tool/UI
types; that does not prove its absence elsewhere.

Current bridge completion mechanisms differ: ordinary placement acknowledges
Apply before later finish; roads acknowledge finish after Apply; transit finish
checks committed stop/line entities; line naming hands off to the rename system,
which acknowledges SetCustomName before deferred playback is demonstrated.
Actual setter/stop/apply/finish ordering, manual takeover effects, deferred name
playback, cleanup and cancellation of queued/entity/pathfinding work remain
unproved. No probe instrumentation or cleanup change is added in O1.

### Save and intended identity

```csharp
System.Threading.Tasks.Task<bool> Game.SceneFlow.GameManager.Save(string saveName, Game.Assets.SaveInfo meta, Colossal.IO.AssetDatabase.ILocalAssetDatabase database, UnityEngine.Texture savePreview)
System.Threading.Tasks.Task<bool> Game.SceneFlow.GameManager.Save(string saveName, Game.Assets.SaveInfo meta, Colossal.IO.AssetDatabase.ILocalAssetDatabase database, Game.UI.ScreenCaptureHelper.AsyncRequest previewRequest)
void Game.SceneFlow.GameManager.EventGameSaveLoad.Invoke(string saveName, string previewUri, bool start, bool success)
Colossal.IO.AssetDatabase.AssetDataPath Game.SceneFlow.SaveHelpers.GetAssetDataPath<T>(Colossal.IO.AssetDatabase.ILocalAssetDatabase database, string saveName)
bool Colossal.IO.AssetDatabase.ILocalAssetDatabase.Exists<TAssetData>(Colossal.IO.AssetDatabase.AssetDataPath path, out TAssetData asset)
Colossal.IO.AssetDatabase.Identifier Colossal.IO.AssetDatabase.AssetData.get_id()
string Colossal.IO.AssetDatabase.AssetData.get_uri()
Colossal.Hash128 Colossal.IO.AssetDatabase.Identifier.guid // field
Colossal.IO.AssetDatabase.SaveGameData Game.Assets.SaveInfo.get_saveGameData()
System.Guid Game.Assets.SaveInfo.get_sessionGuid()
Game.Assets.SaveGameMetadata Game.Settings.UserState.get_lastSaveGameMetadata()
```

Selected Save IL can return Task<bool> true independently of its separate
saveSuccess flag. Completion's URI identifies the **preview texture**, not the
city save. The packaging closure uses native GetAssetDataPath<SaveGameMetadata>
and Exists<PackageAsset> to set saveSuccess. Those are destination lookup
candidates requiring actual fixture evidence; do not construct a path from a
display name or identify the destination from transient metadata/GUIDs. Native
event/task order under failure, durable package readback, contained metadata/data
identities and concurrent-save attribution remain unproved. Neither Save
signature takes a cancellation token; save cancellation is unsupported until
demonstrated. Current legacy saving=true acknowledges start only.

### Preview ownership

```csharp
UnityEngine.RenderTexture Game.UI.ScreenCaptureHelper.CreateRenderTarget(string name, int width, int height, UnityEngine.Experimental.Rendering.GraphicsFormat format = 8)
void Game.UI.ScreenCaptureHelper.CaptureScreenshot(UnityEngine.Camera camera, UnityEngine.RenderTexture destination, Game.UI.Menu.MenuHelpers.SaveGamePreviewSettings settings)
Game.UI.ScreenCaptureHelper.AsyncRequest(UnityEngine.Texture previewTexture)
System.Threading.Tasks.Task Game.UI.ScreenCaptureHelper.AsyncRequest.Complete()
System.Threading.Tasks.Task Game.UI.ScreenCaptureHelper.AsyncRequest.Dispose()
ref Unity.Collections.NativeArray<byte> Game.UI.ScreenCaptureHelper.AsyncRequest.get_result()
```

CreateRenderTarget allocates/creates a render texture. CaptureScreenshot cleans
up temporary resources, not the destination. AsyncRequest allocates persistent
bytes and requests GPU readback; its inspected Dispose awaits completion and
disposes bytes, without destroying the supplied texture. The completion callback
can log GPU errors and still resolve its task. Selected Save IL does not call
previewRequest.Dispose or destroy the input texture. Ownership transfer and a
safe destruction point remain unproved; no speculative cleanup is justified.

## Later native G1 sequence

In a prepared disposable city with verified separately named checkpoint and
real guarded admission: record preload/loaded/load-complete/safe-update order
across a same-save reload; observe native Apply/Finish and manual takeover;
observe deferred transit name playback; perform one separately named save and
correlate task/event/destination package identities; observe GPU/byte/texture
lifetimes before deciding ownership. Keep each group bounded by the registered
manifest and recheck world/lease before it. No disk-full, permission damage,
deliberate hang or developed-city experiment is authorized. Unavailable identity
or cancellation blocks dependent capabilities, not unrelated work.
