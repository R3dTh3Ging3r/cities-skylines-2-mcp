using System;
using System.Threading.Tasks;
using Game.SceneFlow;
using Game.UI.InGame;
using Game.UI.Menu;

namespace CS2MCP
{
    // Actual save control flow, separated from unrelated native meta endpoints for linked-source tests.
    public sealed partial class RequestHandlers
    {
        private BridgeResponse SaveGame(BridgeRequest request)
        {
            if (!TryGetCity(out _, out BridgeResponse error))
            {
                return error;
            }
            string name = request.Query.TryGetValue("name", out string rawName) && !string.IsNullOrEmpty(rawName)
                ? rawName
                : $"CS2MCP {DateTime.Now:yyyy-MM-dd HH-mm-ss}";

            MenuUISystem menu = World.GetExistingSystemManaged<MenuUISystem>();
            if (menu == null)
            {
                return BridgeResponse.Error(503, "menu system unavailable");
            }
            var saveInfo = menu.GetSaveInfo(autoSave: false);
            // The save pipeline requires a preview texture (null crashes it) —
            // capture one exactly like AutoSaveSystem does.
            UnityEngine.RenderTexture preview = Game.UI.ScreenCaptureHelper.CreateRenderTarget("PreviewSaveGame-CS2MCP", 680, 383);
            Game.UI.ScreenCaptureHelper.CaptureScreenshot(UnityEngine.Camera.main, preview, new MenuHelpers.SaveGamePreviewSettings());
            var observation = BridgeSaveTaskObservation.Begin(request);
            Task<bool> task;
            try
            {
                task = GameManager.instance.Save(name, saveInfo, Colossal.IO.AssetDatabase.AssetDatabase.user, preview);
            }
            catch
            {
                observation?.CallThrew();
                throw;
            }
            observation?.Returned(task);

            return BridgeResponse.Json(new
            {
                saving = true,
                name,
                note = "save runs asynchronously; it appears in the game's load menu when finished",
            });
        }

    }
}
