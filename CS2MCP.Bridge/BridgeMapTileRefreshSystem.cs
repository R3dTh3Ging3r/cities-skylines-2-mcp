using System;
using System.Collections.Generic;
using Game;
using Game.Common;
using Unity.Entities;
using UnityEngine.Scripting;
using CS2MCP.Core;

namespace CS2MCP
{
    /// <summary>Re-flags purchased tiles in ToolUpdate, preserving the purchase world's fence.
    /// A stale refresh is dropped without inspecting another world's ECS or undoing a purchase.</summary>
    public sealed partial class BridgeMapTileRefreshSystem : GameSystemBase
    {
        private struct PendingRefresh { internal Entity[] Tiles; internal DeferredWorldWork Work; }
        private readonly Queue<PendingRefresh> m_Pending = new Queue<PendingRefresh>();

        public void Enqueue(IEnumerable<Entity> tiles, DeferredWorldWork work)
        {
            if (work == null) throw new ArgumentNullException(nameof(work));
            m_Pending.Enqueue(new PendingRefresh { Tiles = new List<Entity>(tiles).ToArray(), Work = work });
        }

        [Preserve]
        protected override void OnUpdate()
        {
            while (m_Pending.Count > 0)
            {
                PendingRefresh pending = m_Pending.Dequeue();
                foreach (Entity tile in pending.Tiles)
                {
                    if (!pending.Work.TryRun(() => EntityManager.Exists(tile), out bool exists, out var error))
                    {
                        Mod.Log.Warn($"deferred tile refresh stopped: {error.message}; original purchase effects were not reversed");
                        break;
                    }
                    if (!exists) continue;
                    // Recheck the component under the same original fence, and guard the write separately.
                    if (!pending.Work.TryRun(() => EntityManager.HasComponent<Updated>(tile), out bool updated, out error) ||
                        !updated && !pending.Work.TryRun(() => EntityManager.AddComponent<Updated>(tile), out error))
                    {
                        Mod.Log.Warn($"deferred tile refresh stopped: {error.message}; original purchase effects were not reversed");
                        break;
                    }
                }
            }
        }

        [Preserve]
        protected override void OnDestroy() { m_Pending.Clear(); base.OnDestroy(); }
    }
}
