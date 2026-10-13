using Game.SceneFlow;
using Game.Tools;

namespace CS2MCP
{
    public sealed partial class RequestHandlers
    {
        private BridgeResponse ClearTool()
        {
            // TryGetCity checks the game mode and city entity, but does not
            // reject a transition while the next save is still loading.
            GameManager manager = GameManager.instance;
            if (manager != null && manager.isGameLoading)
            {
                return BridgeResponse.Error(409, "game is loading, retry after the city has loaded");
            }
            if (!TryGetCity(out _, out BridgeResponse error))
            {
                return error;
            }

            ToolSystem toolSystem = World.GetExistingSystemManaged<ToolSystem>();
            DefaultToolSystem defaultTool = World.GetExistingSystemManaged<DefaultToolSystem>();
            if (toolSystem == null || defaultTool == null)
            {
                return BridgeResponse.Error(503, "game tool systems unavailable, retry shortly");
            }

            // Bridge build tools finish over multiple frames. Switching away
            // would interfere with their pending operation and completion.
            if (TransitToolsBusy(out error))
            {
                return error;
            }

            ToolBaseSystem previousTool = toolSystem.activeTool;
            bool changed = previousTool != defaultTool;
            if (changed)
            {
                // Use the same native setter as bridge build-tool cleanup.
                // Avoid invoking it again when selection mode is already active.
                toolSystem.activeTool = defaultTool;
            }

            return BridgeResponse.Json(new
            {
                previousTool = previousTool?.GetType().Name,
                activeTool = toolSystem.activeTool?.GetType().Name,
                changed,
                warning = "switching to default selection mode discards unfinished player tool previews",
            });
        }
    }
}
