# Bridge testing and release gates

Run from the repository root in PowerShell. Node >=18, TypeScript ES2022/Node16,
C# 9 and .NET Framework 4.7.2 remain the runtime targets. Use existing packages;
the pure core runner needs no game assemblies.

```powershell
# Select the verified official Node18 runtime for this process only, if available.
$env:PATH = (Resolve-Path .local/f1/node18/node-v18.20.8-win-x64).Path + ';' + $env:PATH
npm --prefix mcp-server test
dotnet build tests/CS2MCP.CoreTests/CS2MCP.CoreTests.csproj -c Release
& tests/CS2MCP.CoreTests/bin/Release/net472/CS2MCP.CoreTests.exe --suite contracts
& tests/CS2MCP.CoreTests/bin/Release/net472/CS2MCP.CoreTests.exe --suite endpoints
& tests/CS2MCP.CoreTests/bin/Release/net472/CS2MCP.CoreTests.exe --suite metrics
& tests/CS2MCP.CoreTests/bin/Release/net472/CS2MCP.CoreTests.exe --suite values
& tests/CS2MCP.CoreTests/bin/Release/net472/CS2MCP.CoreTests.exe --suite operations
# Serializer integration runner: links production BridgeResponse, using only the
# already-installed Newtonsoft runtime assembly (no Game/Unity/ECS references).
dotnet build tests/CS2MCP.ResponseTests/CS2MCP.ResponseTests.csproj -c Release '-p:GamePath=E:/SteamLibrary/steamapps/common/Cities Skylines II'
& tests/CS2MCP.ResponseTests/bin/Release/net472/CS2MCP.ResponseTests.exe
dotnet build CS2MCP.Bridge/CS2MCP.Bridge.csproj -c Release -p:SkipDeploy=true '-p:GamePath=E:/SteamLibrary/steamapps/common/Cities Skylines II'
```

Check each exit code. The npm script uses an explicit file list because Node18
on Windows does not expand test globs. Append new files without removing prior
suites. Unknown core suites and zero assertions fail. Node tests supply ephemeral
fake loopback URLs; they never fall back to the real bridge or import its executable
entry point. The evidence suite invokes the actual PowerShell script with an
asynchronous child process so its fake server can answer. Child processes and
listeners are closed, and temporary evidence stays below ignored `.local/v01/`.
The separate response runner preserves the BCL-only core runner and existing
reference-assembly version 1.0.3. Supply `GamePath`, `CS2_PATH` or
`CSII_INSTALLATIONPATH`, or pass `-p:NewtonsoftPath=<existing DLL path>` directly;
a missing serializer path fails before compilation. Its tests exercise actual
production serialization of ambiguous errors, explicit no-effect validation
rejections, queued timeouts and null success properties without native calls.

Evidence tests use `powershell -NoProfile -ExecutionPolicy Bypass -File` for this
locally authored script. This affects only that child process. Check
`Get-ExecutionPolicy -List` first on a new host: do not change or work around
MachinePolicy/UserPolicy. Where local policy permits scripts, ordinary
`powershell -NoProfile -File` is sufficient.

Before the exact native compile, record installed DLL existence, size, timestamp
and SHA256; repeat afterward. The installed path is
`%USERPROFILE%/AppData/LocalLow/Colossal Order/Cities Skylines II/Mods/CS2MCP/CS2MCP.dll`.
Equal known before/after hashes prove this compile did not replace that DLL.
Absent before/after is recorded as absent, not as a known equal hash. Hash
`CS2MCP.Bridge/bin/Release/CS2MCP.dll` separately as the candidate. Record source
revision and dirty state separately; neither a source revision nor an installed
file hash identifies the assembly already loaded by the game. Keep raw build,
hash and test output in `.local/v01/`.

Offline passing suites and a SkipDeploy build make the candidate reviewable.
They do not establish native parity. Release requires the appropriate closed-game
installation/readback, disposable fixture session, independently observed panels,
known build/world identity and tool exposure. Unknown date/count fields and cargo
units remain explicitly qualified. F6 introduces no mutation capability.
See the [contracts evidence procedure](validation/contracts.md) for native gates,
report validation, manual probes and rollback.
