// Native boundary types/effects only. RequestHandlers.Save.cs is the actual production method.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Entities;

internal static class SaveNativeBoundary
{
    internal static readonly List<string> Calls = new List<string>();
    internal static int MainThread;
    internal static bool CityAvailable, MenuAvailable;
    internal static string ThrowAt;
    internal static Task<bool> NextTask;
    internal static string SavedName;
    internal static object SavedInfo, SavedDatabase, SavedPreview;
    internal static UnityEngine.RenderTexture Preview;
    internal static Game.UI.InGame.MenuUISystem Menu;
    internal static Action OnNativeSave;

    internal static void Reset()
    {
        MainThread = Thread.CurrentThread.ManagedThreadId;
        Calls.Clear();
        CityAvailable = true;
        MenuAvailable = true;
        ThrowAt = null;
        NextTask = null;
        SavedName = null;
        SavedInfo = null;
        SavedDatabase = null;
        SavedPreview = null;
        Preview = null;
        Menu = new Game.UI.InGame.MenuUISystem();
        OnNativeSave = null;
    }

    internal static void Call(string name)
    {
        if (Thread.CurrentThread.ManagedThreadId != MainThread)
            throw new InvalidOperationException("native access from continuation");
        Calls.Add(name);
        if (ThrowAt == name) throw new InvalidOperationException("private-native-failure");
    }
}
namespace UnityEngine
{
    public class Texture
    {
    }

    public sealed class RenderTexture : Texture
    {
    }

    public sealed class Camera
    {
        private static readonly Camera value = new Camera();
        public static Camera main
        {
            get
            {
                SaveNativeBoundary.Call("camera");
                return value;
            }
        }
    }
}
namespace Game.Assets
{
    public sealed class SaveInfo
    {
    }
}
namespace Colossal.IO.AssetDatabase
{
    public interface ILocalAssetDatabase
    {
    }

    internal sealed class SaveTestDatabase : ILocalAssetDatabase
    {
    }

    public static class AssetDatabase
    {
        private static readonly ILocalAssetDatabase database = new SaveTestDatabase();
        public static ILocalAssetDatabase user
        {
            get
            {
                SaveNativeBoundary.Call("database");
                return database;
            }
        }
    }
}
namespace Game.UI.Menu
{
    public static class MenuHelpers
    {
        public struct SaveGamePreviewSettings
        {
        }
    }
}
namespace Game.UI.InGame
{
    public sealed class MenuUISystem
    {
        internal readonly Game.Assets.SaveInfo Info = new Game.Assets.SaveInfo();

        public Game.Assets.SaveInfo GetSaveInfo(bool autoSave)
        {
            SaveNativeBoundary.Call("saveInfo:" + autoSave);
            return Info;
        }
    }
}
namespace Game.UI
{
    public static class ScreenCaptureHelper
    {
        public static UnityEngine.RenderTexture CreateRenderTarget(string name, int width, int height)
        {
            SaveNativeBoundary.Call("preview:" + name + ":" + width + ":" + height);
            return SaveNativeBoundary.Preview = new UnityEngine.RenderTexture();
        }

        public static void CaptureScreenshot(UnityEngine.Camera camera, UnityEngine.RenderTexture destination,
            Game.UI.Menu.MenuHelpers.SaveGamePreviewSettings settings)
        {
            SaveNativeBoundary.Call("capture");
            if (!ReferenceEquals(destination, SaveNativeBoundary.Preview)) throw new Exception("preview changed");
        }
    }
}
namespace Game.SceneFlow
{
    public sealed class GameManager
    {
        private static readonly GameManager manager = new GameManager();
        public static GameManager instance
        {
            get
            {
                SaveNativeBoundary.Call("manager");
                return manager;
            }
        }

        public Task<bool> Save(string name, Game.Assets.SaveInfo info,
            Colossal.IO.AssetDatabase.ILocalAssetDatabase database, UnityEngine.Texture preview)
        {
            SaveNativeBoundary.Call("save");
            SaveNativeBoundary.SavedName = name;
            SaveNativeBoundary.SavedInfo = info;
            SaveNativeBoundary.SavedDatabase = database;
            SaveNativeBoundary.SavedPreview = preview;
            SaveNativeBoundary.OnNativeSave?.Invoke();
            return SaveNativeBoundary.NextTask;
        }
    }
}
namespace CS2MCP
{
    public sealed partial class RequestHandlers
    {
        private sealed class SaveTestWorld
        {
            internal T GetExistingSystemManaged<T>() where T : class
            {
                SaveNativeBoundary.Call("menu");
                return SaveNativeBoundary.MenuAvailable ? SaveNativeBoundary.Menu as T : null;
            }
        }

        private readonly SaveTestWorld World = new SaveTestWorld();

        private bool TryGetCity(out Entity city, out BridgeResponse error)
        {
            SaveNativeBoundary.Call("city");
            city = default;
            error = SaveNativeBoundary.CityAvailable ? null : BridgeResponse.Error(503, "city unavailable");
            return SaveNativeBoundary.CityAvailable;
        }

        internal BridgeResponse InvokeSave(BridgeRequest request) => SaveGame(request);
    }
}
