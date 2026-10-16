using System;
using System.Collections.Generic;
using Game;
using Game.UI;
using Unity.Entities;
using UnityEngine.Scripting;
using CS2MCP.Core;

namespace CS2MCP
{
    /// <summary>Deferred naming retains the creation world's fence, including its response closure.
    /// SetCustomName acknowledgment is not proof of end-frame playback or full tool cleanup.</summary>
    public sealed partial class BridgeTransitRenameSystem : GameSystemBase
    {
        private struct PendingRename
        {
            public Entity Line;
            public string Name;
            public BridgeRequest Request;
            public Func<string, BridgeResponse> BuildResponse;
            public DeferredWorldWork Work;
        }
        private readonly Queue<PendingRename> m_Pending = new Queue<PendingRename>();
        private NameSystem m_NameSystem;

        public void Enqueue(Entity line, string name, BridgeRequest request,
            Func<string, BridgeResponse> buildResponse, DeferredWorldWork work)
        {
            if (work == null) throw new ArgumentNullException(nameof(work));
            m_Pending.Enqueue(new PendingRename { Line = line, Name = name, Request = request,
                BuildResponse = buildResponse, Work = work });
        }

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            m_NameSystem = base.World.GetOrCreateSystemManaged<NameSystem>();
        }

        [Preserve]
        protected override void OnUpdate()
        {
            while (m_Pending.Count > 0)
            {
                PendingRename pending = m_Pending.Dequeue();
                if (!pending.Work.TryRun(() => m_NameSystem.SetCustomName(pending.Line, pending.Name), out var error) ||
                    !pending.Work.TryRun(() => base.World.GetExistingSystemManaged<Game.UI.InGame.TransportationOverviewUISystem>()?.RequestUpdate(), out error))
                {
                    Fail(pending.Request, error);
                    continue;
                }
                // The builder reads SafeLabel. Guard it separately after the native setter/UI callback.
                if (!pending.Work.TryRun(() => pending.BuildResponse(null) ?? throw new InvalidOperationException("line response builder returned no response"),
                    out BridgeResponse response, out error))
                {
                    Fail(pending.Request, error);
                    continue;
                }
                pending.Request?.Complete(response);
            }
        }

        private static void Fail(BridgeRequest request, BridgeErrorData error)
        {
            Mod.Log.Warn($"deferred transit naming stopped: {error.message}; the original line may already exist");
            request?.Complete(BridgeResponse.Error(error.httpStatus ?? 500, error.message + "; the original line may already exist; do not blindly create it again",
                error.code, retryable: false, outcome: error.outcome, operationId: error.operationId));
        }

        [Preserve]
        protected override void OnDestroy()
        {
            while (m_Pending.Count > 0)
            {
                var pending = m_Pending.Dequeue();
                Fail(pending.Request, new BridgeErrorData(ErrorCodes.WorldMismatch,
                    "line naming system was destroyed before completion", outcome: "partial", operationId: pending.Request?.OperationId));
            }
            base.OnDestroy();
        }
    }
}
