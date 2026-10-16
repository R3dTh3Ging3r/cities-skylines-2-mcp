using System;
using System.Collections;
using UnityEngine;
using CS2MCP.Core;

namespace CS2MCP
{
    /// <summary>
    /// MonoBehaviour helper that captures the screen at WaitForEndOfFrame —
    /// the only point in the frame where Unity reliably allows reading the
    /// back buffer (calling ScreenCapture from a system update returns null).
    /// Completes the BridgeRequest itself, so the handler returns no response.
    /// </summary>
    public sealed class ScreenshotRunner : MonoBehaviour
    {
        private static ScreenshotRunner s_Instance;

        public static ScreenshotRunner Ensure()
        {
            if (s_Instance == null)
            {
                var host = new GameObject("CS2MCP.ScreenshotRunner")
                {
                    hideFlags = HideFlags.HideAndDontSave,
                };
                DontDestroyOnLoad(host);
                s_Instance = host.AddComponent<ScreenshotRunner>();
            }
            return s_Instance;
        }

        public void Capture(BridgeRequest request)
        {
            // Retain the HTTP ingress world. Readiness selects diagnostic vs city capture, never a new identity.
            WorldSnapshot snapshot = Mod.Worlds.Snapshot;
            var work = new DeferredWorldWork(Mod.Worlds, request.IngressWorld, snapshot.ready, "notApplied", request.OperationId);
            StartCoroutine(CaptureRoutine(request, work));
        }

        private IEnumerator CaptureRoutine(BridgeRequest request, DeferredWorldWork work)
        {
            if (!work.TryRun(() => { }, out var error))
            {
                Fail(request, error);
                yield break;
            }
            yield return new WaitForEndOfFrame();
            CaptureAfterEndOfFrame(request, work);
        }

        private static void CaptureAfterEndOfFrame(BridgeRequest request, DeferredWorldWork work)
        {
            Texture2D captured = null;
            Texture2D output = null;
            try
            {
                // TryRun checks after the yield, before capture, and after capture/encoding. The body also
                // fences its native stages so a synchronous invalidation cannot advance to another stage.
                if (!work.TryRun(() => {
                    captured = ScreenCapture.CaptureScreenshotAsTexture();
                    work.ThrowIfInvalid();
                    if (captured == null)
                    {
                        captured = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                        work.ThrowIfInvalid();
                        captured.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                        work.ThrowIfInvalid();
                        captured.Apply();
                    }
                    work.ThrowIfInvalid();
                    output = captured;
                    if (request.TryGetInt("width", out int width) && width > 0 && width < captured.width)
                        output = Downscale(captured, width);
                    work.ThrowIfInvalid();
                    byte[] bytes = ImageConversion.EncodeToPNG(output);
                    if (bytes == null || bytes.Length == 0) throw new InvalidOperationException("PNG encode failed");
                    return bytes;
                }, out byte[] png, out var error))
                {
                    Fail(request, error);
                    return;
                }
                // Publication is separate from capture: stale bytes never become a successful image.
                if (!work.TryRun(() => BridgeResponse.Png(png, work.World, work.Ready), out BridgeResponse response, out error))
                {
                    Fail(request, error);
                    return;
                }
                request.Complete(response);
            }
            finally
            {
                // Only resources allocated by this capture are released; no old-world ECS reconciliation.
                if (output != null && !ReferenceEquals(output, captured)) Destroy(output);
                if (captured != null) Destroy(captured);
            }
        }

        private static void Fail(BridgeRequest request, BridgeErrorData error)
        {
            request.Complete(BridgeResponse.Error(error.httpStatus ?? 500, error.message, error.code,
                retryable: false, outcome: error.outcome, operationId: error.operationId));
        }

        private static Texture2D Downscale(Texture2D source, int targetWidth)
        {
            int targetHeight = Mathf.Max(1, Mathf.RoundToInt((float)source.height * targetWidth / source.width));
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;
                var result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
                result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
                result.Apply();
                return result;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
