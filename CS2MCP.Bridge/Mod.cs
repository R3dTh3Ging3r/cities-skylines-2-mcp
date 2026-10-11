using Colossal.Logging;
using Game;
using Game.Modding;
using System.Reflection;
using CS2MCP.Core;

namespace CS2MCP
{
    public sealed class Mod : IMod
    {
        public const string Name = "CS2MCP";
        public const string Version = "0.9.0";
        public static readonly RuntimeSnapshotStore RuntimeSnapshots = new RuntimeSnapshotStore();
        public static readonly BuildIdentity Identity = ReadBuildIdentity();
        // Read once with mod startup; later environment changes cannot enable probe dispatch.
        public static readonly NativeValidationMetadata NativeValidation = new NativeValidationMetadata(
            System.Environment.GetEnvironmentVariable("CS2MCP_NATIVE_VALIDATION"));

        // Force initialization when the loader constructs the mod, rather than beforefieldinit timing.
        static Mod() { }

        private static BuildIdentity ReadBuildIdentity()
        {
            Assembly assembly = typeof(Mod).Assembly;
            string information = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            string revision = null;
            foreach (AssemblyMetadataAttribute metadata in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
                if (metadata.Key == "SourceRevisionId") revision = metadata.Value;
            return new BuildIdentity(assembly.GetName().Version?.ToString(), Version, information, revision);
        }

        public static readonly ILog Log = LogManager.GetLogger(Name).SetShowsErrorsInUI(false);

        public void OnLoad(UpdateSystem updateSystem)
        {
            Log.Info($"{Name} {Version} loading, registering bridge systems");
            updateSystem.UpdateAt<BridgeSystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<BridgeToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<BridgeTransitToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<BridgeTransitRenameSystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<BridgeMapTileRefreshSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<BridgeRoadToolSystem>(SystemUpdatePhase.ToolUpdate);
        }

        public void OnDispose()
        {
            Log.Info($"{Name} disposing");
            BridgeSystem.Instance?.StopServer();
        }
    }
}
