using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using CS2MCP;
using CS2MCP.Core;
using Unity.Entities;
using Unity.Mathematics;
using Game.Tools;
using Newtonsoft.Json.Linq;

internal static class LifecycleProgram
{
    static int passed, failed;
    static void Check(string name, Action test)
    {
        try
        {
            Reset();
            test();
            passed++;
            Console.WriteLine("PASS " + name);
        }
        catch (Exception e)
        {
            failed++;
            Console.WriteLine("FAIL " + name + ": " + e);
        }
    }

    static void Equal(object expected, object actual)
    {
        if (!Equals(expected, actual))
            throw new Exception($"expected {expected}, got {actual}");
    }

    static void True(bool value) => Equal(true, value);
    static void Reject(Action action, int status = 400)
    {
        try
        {
            action();
            throw new Exception("accepted invalid input");
        }
        catch (OperationException e)
        {
            Equal(status, e.Error.httpStatus);
        }
    }

    static LifecycleTraceJournal NewJournal(Func<LifecycleTraceEvent, int> measure = null) => new LifecycleTraceJournal("boot", measure ?? (e => ObservationWire.TraceEvent(e).Length));
    static void Reset()
    {
        Mod.Worlds.CompleteLoad();
        ConstructionLifecycleTrace.Journal = NewJournal();
    }

    static BridgeRequest Request() => new BridgeRequest
    {
        Method = "POST",
        IngressWorld = Mod.Worlds.Current,
        Path = "/build/place"
    };
    static LifecycleTracePage Page(string after = null, int limit = 32)
    {
        var q = new Dictionary<string, string>
        {
            {
                "limit",
                limit.ToString()
            }
        };
        if (after != null)
        {
            q.Add("bootId", "boot");
            q.Add("afterSequence", after);
        }

        return ConstructionLifecycleTrace.Read(q);
    }

    static void Phases(params TracePhase[] expected)
    {
        var actual = Page("0").events.Select(e => e.phase).ToArray();
        int last = -1;
        foreach (var phase in expected)
        {
            int i = Array.IndexOf(actual, phase.ToString(), last + 1);
            True(i > last);
            last = i;
        }
    }

    static BridgeToolSystem Generic(BridgeRequest request)
    {
        var tool = new BridgeToolSystem();
        True(tool.TryQueuePlacement(default, null, default, quaternion.identity, request));
        return tool;
    }

    static BridgeRoadToolSystem Road(BridgeRequest request)
    {
        var tool = new BridgeRoadToolSystem();
        True(tool.TryQueueBuild(default, null, default, default, default, request));
        return tool;
    }

    static BridgeTransitToolSystem Transit(BridgeRequest request, bool line = false, string name = null)
    {
        var tool = new BridgeTransitToolSystem();
        True(line ? tool.TryQueueLine(default, null, "private type", new[] { new BridgeTransitToolSystem.LineStop { Stop = new Entity { Index = 3, Version = 2 } } }, false, default, name, 1, request) : tool.TryQueueStop(default, null, "private type", default, request));
        return tool;
    }

    static void Record(LifecycleTraceJournal journal, LifecycleTraceContext context, Func<WorldSnapshot> world = null, Func<long?> frame = null, Func<DateTime> utc = null, Func<bool?> owner = null, LifecycleTraceReference[] refs = null) => journal.Observe(context, TraceAdapter.generic, TraceKind.placement, TracePhase.queued, world ?? (() => Mod.Worlds.Snapshot), frame ?? (() => 5), utc ?? (() => DateTime.UtcNow), owner, references: refs);
    static string Snapshot(Game.Tools.ObjectToolBaseSystem tool, BridgeRequest request, bool busy) => string.Join(",", tool.Calls) + "|" + tool.applyMode + "|" + busy + "|" + (tool.Active == tool) + "|" + request.WaitForResponse(1)?.Status + "|" + Convert.ToBase64String(request.WaitForResponse(1)?.Body ?? Array.Empty<byte>());
    // Run the actual same adapter through Apply, displacement and a different next kind.
    // Recording must neither fix nor conceal the original interruption/native cleanup policy.
    static string TransitReuse(bool firstLine, bool nextWorld, int recorderMode, string wireDirectory)
    {
        Reset();
        ConstructionLifecycleTrace.Journal = NewJournal(e => recorderMode == 0 ? ObservationWire.TraceEvent(e).Length
            : recorderMode == 1 ? 0 : throw new InvalidOperationException("controlled recording failure"));
        var previous = Request();
        previous.Path = firstLine ? "/transit/lines/create" : "/transit/stops/place";
        var tool = Transit(previous, firstLine);
        tool.Step();
        tool.Step();
        var nativeResult = typeof(BridgeTransitToolSystem).GetField("m_ResultEntity", BindingFlags.Instance | BindingFlags.NonPublic);
        var nativeApplied = typeof(BridgeTransitToolSystem).GetField("m_Applied", BindingFlags.Instance | BindingFlags.NonPublic);
        Equal(7, ((Entity)nativeResult.GetValue(tool)).Index);
        Equal(true, nativeApplied.GetValue(tool));
        Equal(ApplyMode.Apply, tool.applyMode);
        tool.Active = new ToolBaseSystem();
        tool.Stop();
        Equal(409, previous.WaitForResponse(1).Status);
        Equal(false, tool.IsBusy);
        // Preserve the legacy stale native state. Selection alone must stop attributing it.
        Equal(7, ((Entity)nativeResult.GetValue(tool)).Index);
        Equal(true, nativeApplied.GetValue(tool));
        string previousOutcome = Snapshot(tool, previous, tool.IsBusy);
        if (nextWorld)
        {
            Mod.Worlds.BeginLoad();
            Mod.Worlds.CompleteLoad();
        }
        var request = Request();
        bool line = !firstLine;
        request.Path = line ? "/transit/lines/create" : "/transit/stops/place";
        tool.NativeResult = new Entity { Index = 19, Version = 3 };
        True(line
            ? tool.TryQueueLine(default, null, "private type", new[] { new BridgeTransitToolSystem.LineStop { Stop = new Entity { Index = 3, Version = 2 } } }, false, default, "current line", 1, request)
            : tool.TryQueueStop(default, null, "private type", default, request));
        Equal(Entity.Null, nativeResult.GetValue(tool));
        Equal(false, nativeApplied.GetValue(tool));
        Equal(ApplyMode.Apply, tool.applyMode);
        if (recorderMode == 0)
        {
            var queued = Page("0").events.Single(e => e.traceId == request.LifecycleContext.traceId && e.phase == "queued");
            Equal(line ? "line" : "stop", queued.kind);
            True(WorldIdentity.Same(request.IngressWorld, queued.ingressWorld));
            // New operation has no assigned result yet; its held line-stop input is distinct.
            True(!queued.references.Any(r => r.index == 7 || r.index == 19));
            Equal(line ? 2 : 1, queued.references.Count);
        }
        tool.Step();
        tool.Step();
        Equal(19, ((Entity)nativeResult.GetValue(tool)).Index);
        Equal(true, nativeApplied.GetValue(tool));
        Equal(null, request.WaitForResponse(1));
        if (recorderMode == 0)
        {
            var current = Page("0").events.Where(e => e.traceId == request.LifecycleContext.traceId).ToArray();
            var applied = current.Single(e => e.phase == "applyObserved");
            var result = applied.references.Single(r => r.index == 19);
            Equal(3, result.version);
            Equal(line ? "line" : "stop", result.role);
            True(WorldIdentity.Same(request.IngressWorld, result.world));
            True(current.Where(e => e.phase != "applyObserved").All(e => !e.references.Any(r => r.index == 7 || r.index == 19)));
        }
        tool.Step();
        if (line)
        {
            Equal(null, request.WaitForResponse(1));
            var rename = tool.World.GetOrCreateSystemManaged<BridgeTransitRenameSystem>();
            typeof(BridgeTransitRenameSystem).GetMethod("OnCreate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rename, null);
            typeof(BridgeTransitRenameSystem).GetMethod("OnUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rename, null);
            Equal(1, tool.World.GetOrCreateSystemManaged<Game.UI.NameSystem>().Calls);
        }
        Equal(200, request.WaitForResponse(1).Status);
        Equal(false, tool.IsBusy);
        Equal(ApplyMode.None, tool.applyMode);
        Equal(Entity.Null, nativeResult.GetValue(tool));
        Equal(false, nativeApplied.GetValue(tool));
        if (recorderMode == 0)
        {
            var page = Page("0");
            var current = page.events.Where(e => e.traceId == request.LifecycleContext.traceId).ToArray();
            True(current.All(e => !e.references.Any(r => r.index == 7)));
            True(current.Single(e => e.phase == "finishObserved").references.Any(r => r.index == 19 && r.version == 3));
            if (line)
                foreach (string phase in new[] { "renameQueued", "renameDequeued", "setterAttempt", "responseBuilderAttempt" })
                    True(current.Single(e => e.phase == phase).references.Any(r => r.role == "line" && r.index == 19 && r.version == 3 && WorldIdentity.Same(r.world, request.IngressWorld)));
            if (wireDirectory != null)
            {
                Directory.CreateDirectory(wireDirectory);
                string name = (firstLine ? "line-stop" : "stop-line") + (nextWorld ? "-new-world" : "-same-world");
                var input = ObservationCatalog.Parse("{\"items\":[{\"id\":\"reuse\",\"name\":\"construction.lifecycleTrace\",\"args\":{\"bootId\":\"boot\",\"afterSequence\":0,\"limit\":32}}]}");
                var catalog = new ObservationCatalog(new RequestHandlers(), () => 10, () => DateTime.UtcNow);
                var scheduler = new ObservationScheduler(Mod.Worlds, catalog, () => 10, () => 0, new ObservationWire());
                byte[] bytes = null;
                var group = scheduler.Admit(input, value => bytes = value);
                scheduler.Pump();
                Equal(null, group.Result.results[0].error);
                True(bytes.Length <= 262144);
                File.WriteAllBytes(Path.Combine(wireDirectory, "lifecycle-reuse-" + name + ".json"), bytes);
                group.Detach();
            }
        }
        else
            Equal(0, Page("0").events.Length);
        return previousOutcome + "|" + Snapshot(tool, request, tool.IsBusy);
    }

    static int Main(string[] args)
    {
        foreach (bool firstLine in new[] { true, false })
            foreach (bool nextWorld in new[] { false, true })
                Check("same transit adapter " + (firstLine ? "line to stop" : "stop to line") + (nextWorld ? " after world transition" : " in same world") + " selects only current trace results", () => {
                    string success = TransitReuse(firstLine, nextWorld, 0, args.Length > 0 ? args[0] : null);
                    Equal(success, TransitReuse(firstLine, nextWorld, 1, null));
                    Equal(success, TransitReuse(firstLine, nextWorld, 2, null));
                });
        Check("actual dispatch envelope guards old writers and preserves direct replies", () =>
        {
            var request = Request();
            RequestHandlers.NativeHandle = r => BridgeResponse.Json(new { direct = true });
            BridgeSystem.Instance.Dispatch(request, true);
            Equal(200, request.WaitForResponse(1).Status);
            Phases(TracePhase.dispatch, TracePhase.replyAttempt);
            Equal(2, Page("0").events.Length);
            Equal("buildPlace", Page("0").events[0].route);
            foreach (var endpoint in EndpointCatalog.All.Where(e => e.access != "read" && !e.path.StartsWith("/v2/")))
                True(LifecycleTraceRoutes.ForPath(endpoint.path) != TraceRoute.unknown);
            Equal(TraceRoute.unknown, LifecycleTraceRoutes.ForPath("/arbitrary/private/path"));
            Reset();
            request = Request();
            Mod.Worlds.BeginLoad();
            BridgeSystem.Instance.Dispatch(request, true);
            Equal(409, request.WaitForResponse(1).Status);
            Phases(TracePhase.dispatch, TracePhase.exceptionObserved, TracePhase.replyAttempt);
            RequestHandlers.NativeHandle = null;
        });
        Check("actual generic early reply precedes Finish and restoration", () =>
        {
            var request = Request();
            var tool = Generic(request);
            tool.Step();
            tool.Step();
            Equal(200, request.WaitForResponse(1).Status);
            True(tool.IsBusy);
            Equal(ApplyMode.Apply, tool.applyMode);
            True(!Page("0").events.Any(e => e.phase == "finishObserved"));
            tool.Step();
            Equal(false, tool.IsBusy);
            Equal(ApplyMode.None, tool.applyMode);
            Phases(TracePhase.queued, TracePhase.definitions, TracePhase.validation, TracePhase.applyObserved, TracePhase.replyAttempt, TracePhase.finishObserved, TracePhase.deactivateObserved, TracePhase.restorationAttempt);
            var reply = Page("0").events.Single(e => e.phase == "replyAttempt");
            Equal(null, reply.frame);
            Equal("notSampled", reply.frameReason);
            Equal(true, reply.completionAccepted);
        });
        Check("actual generic busy refusal does not overwrite pending context", () =>
        {
            var first = Request();
            var tool = Generic(first);
            var second = Request();
            Equal(false, tool.TryQueueDemolish(default, "private target", second));
            tool.Step();
            tool.Step();
            tool.Step();
            True(first.LifecycleContext.traceId != second.LifecycleContext.traceId);
            Equal("busy", Page("0").events.Single(e => e.traceId == second.LifecycleContext.traceId).phase);
        });
        Check("actual generic rejected validation never emits Apply", () =>
        {
            var request = Request();
            var tool = Generic(request);
            tool.Allow = false;
            tool.Step();
            tool.Step();
            tool.Step();
            Equal(409, request.WaitForResponse(1).Status);
            Equal(false, Page("0").events.Any(e => e.phase == "applyObserved"));
        });
        Check("actual generic displacement retains missing cleanup behavior", () =>
        {
            var request = Request();
            var tool = Generic(request);
            tool.Active = new ToolBaseSystem();
            tool.Stop();
            True(tool.IsBusy);
            Equal(null, request.WaitForResponse(1));
            Equal("stopObserved", Page("0").events.Last().phase);
        });
        Check("actual road replies only in Finish", () =>
        {
            var request = Request();
            var tool = Road(request);
            tool.Step();
            tool.Step();
            Equal(null, request.WaitForResponse(1));
            tool.Step();
            Equal(200, request.WaitForResponse(1).Status);
            Equal(false, tool.IsBusy);
            Phases(TracePhase.applyObserved, TracePhase.finishObserved, TracePhase.replyAttempt, TracePhase.deactivateObserved);
        });
        Check("actual road rejection/displacement and exceptions preserve response classes", () =>
        {
            foreach (var mode in new[]
            {
                "reject",
                "displace",
                "definitions",
                "readback"
            }

            )
            {
                Reset();
                var request = Request();
                var tool = Road(request);
                if (mode == "displace")
                {
                    tool.Active = new ToolBaseSystem();
                    tool.Stop();
                    Equal(409, request.WaitForResponse(1).Status);
                    Equal(false, tool.IsBusy);
                    continue;
                }

                tool.Allow = mode != "reject";
                tool.ThrowAt = mode;
                tool.Step();
                tool.Step();
                tool.Step();
                Equal(mode == "reject" ? 409 : 500, request.WaitForResponse(1).Status);
                if (mode == "readback")
                    Phases(TracePhase.applyObserved, TracePhase.finishObserved, TracePhase.exceptionObserved, TracePhase.replyAttempt);
            }
        });
        Check("actual transit stop Apply and late reply", () =>
        {
            var request = Request();
            var tool = Transit(request);
            tool.Step();
            tool.Step();
            Equal(null, request.WaitForResponse(1));
            tool.Step();
            Equal(200, request.WaitForResponse(1).Status);
            Phases(TracePhase.validation, TracePhase.applyObserved, TracePhase.finishObserved, TracePhase.replyAttempt);
            True(Page("0").events.Where(e=>e.adapter=="transit").All(e=>e.references.All(r=>r.role!="line")));
        });
        Check("actual transit line no per-frame wait logs and deferred rename replies", () =>
        {
            var request = Request();
            var tool = Transit(request, true, "private line name");
            tool.Ready = false;
            tool.Step();
            int count = Page("0").retainedCount;
            tool.Step();
            tool.Step();
            Equal(count, Page("0").retainedCount);
            tool.Ready = true;
            tool.Step();
            tool.Step();
            Equal(null, request.WaitForResponse(1));
            var rename = tool.World.GetOrCreateSystemManaged<BridgeTransitRenameSystem>();
            typeof(BridgeTransitRenameSystem).GetMethod("OnCreate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rename, null);
            typeof(BridgeTransitRenameSystem).GetMethod("OnUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rename, null);
            Equal(200, request.WaitForResponse(1).Status);
            Phases(TracePhase.applyObserved, TracePhase.finishObserved, TracePhase.renameQueued, TracePhase.deactivateObserved, TracePhase.renameDequeued, TracePhase.setterAttempt, TracePhase.responseBuilderAttempt, TracePhase.replyAttempt);
            Equal(1, tool.World.GetOrCreateSystemManaged<Game.UI.NameSystem>().Calls);
            True(!Encoding.UTF8.GetString(ObservationWire.Standalone(Page("0"))).Contains("private"));
        });
        Check("actual old-world deferred rename retains original identity and refuses setter", () =>
        {
            var request = Request();
            var old = request.IngressWorld;
            var tool = Transit(request, true, "name");
            tool.Step();
            tool.Step();
            tool.Step();
            Mod.Worlds.BeginLoad();
            var rename = tool.World.GetOrCreateSystemManaged<BridgeTransitRenameSystem>();
            typeof(BridgeTransitRenameSystem).GetMethod("OnCreate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rename, null);
            typeof(BridgeTransitRenameSystem).GetMethod("OnUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rename, null);
            Equal(409, request.WaitForResponse(1).Status);
            Equal(0, tool.World.GetOrCreateSystemManaged<Game.UI.NameSystem>().Calls);
            if (args.Length > 0)
            {
                Directory.CreateDirectory(args[0]);
                File.WriteAllBytes(Path.Combine(args[0], "lifecycle-old-world.json"), ObservationWire.Standalone(Page()));
            }

            var last = Page("0").events.Last();
            True(WorldIdentity.Same(old, last.ingressWorld));
            True(!WorldIdentity.Same(last.ingressWorld, last.observedWorld));
        });
        Check("actual transit line invalidation before Finish never restores across world", () =>
        {
            var request = Request();
            var tool = Transit(request, true);
            tool.Step();
            tool.Step();
            int ownershipReads = tool.ActiveReads;
            Mod.Worlds.BeginLoad();
            tool.Step();
            Equal(ownershipReads, tool.ActiveReads);
            Equal(409, request.WaitForResponse(1).Status);
            True(tool.Active == tool);
            Equal(false, tool.IsBusy);
        });
        Check("actual rename destroyed pending work keeps original failure behavior", () =>
        {
            var request = Request();
            var tool = Transit(request, true, "name");
            tool.Step();
            tool.Step();
            tool.Step();
            var rename = tool.World.GetOrCreateSystemManaged<BridgeTransitRenameSystem>();
            typeof(BridgeTransitRenameSystem).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rename, null);
            Equal(500, request.WaitForResponse(1).Status);
            Phases(TracePhase.renameQueued, TracePhase.destroyedObserved, TracePhase.guardedWorkRefused, TracePhase.replyAttempt);
        });
        Check("actual duplicate and late Complete observes attempts without changing first bytes", () =>
        {
            var request = Request();
            Generic(request);
            var first = BridgeResponse.Json(new { a = 1 });
            request.Complete(first);
            request.Complete(BridgeResponse.Error(500, "secret"));
            Equal(first, request.WaitForResponse(1));
            var replies = Page("0").events.Where(e => e.phase == "replyAttempt").ToArray();
            Equal(2, replies.Length);
            Equal(true, replies[0].completionAccepted);
            Equal(false, replies[1].completionAccepted);
        });
        Check("production generic/road/transit outcomes equal when recorder succeeds drops or throws", () =>
        {
            foreach (string family in new[]
            {
                "generic",
                "road",
                "stop",
                "line"
            }

            )
                foreach (string failAt in new[]
                {
                    "none",
                    "definitions",
                    "readback"
                }

                )
                {
                    var outcomes = new List<string>();
                    foreach (int mode in new[]
                    {
                        0,
                        1,
                        2
                    }

                    )
                    {
                        Reset();
                        ConstructionLifecycleTrace.Journal = NewJournal(e => mode == 0 ? ObservationWire.TraceEvent(e).Length : mode == 1 ? 0 : throw new Exception("private serializer failure"));
                        var request = Request();
                        if (family == "generic")
                        {
                            var t = Generic(request);
                            t.ThrowAt = failAt;
                            t.Step();
                            t.Step();
                            t.Step();
                            outcomes.Add(Snapshot(t, request, t.IsBusy));
                        }
                        else if (family == "road")
                        {
                            var t = Road(request);
                            t.ThrowAt = failAt;
                            t.Step();
                            t.Step();
                            t.Step();
                            outcomes.Add(Snapshot(t, request, t.IsBusy));
                        }
                        else
                        {
                            var t = Transit(request, family == "line");
                            t.ThrowAt = failAt;
                            t.Step();
                            t.Step();
                            t.Step();
                            outcomes.Add(Snapshot(t, request, t.IsBusy));
                        }
                    }

                    Equal(outcomes[0], outcomes[1]);
                    Equal(outcomes[0], outcomes[2]);
                }
        });
        Check("recorder isolates world frame timestamp ownership and encoder faults", () =>
        {
            var context = new LifecycleTraceContext(Mod.Worlds.Current);
            var journal = NewJournal();
            Record(journal, context, world: () => throw new Exception());
            Record(journal, context, frame: () => throw new Exception());
            Record(journal, context, utc: () => throw new Exception());
            Record(journal, context, owner: () => throw new Exception());
            var page = journal.Read(LifecycleTraceQuery.Parse(new Dictionary<string, string>()), Mod.Worlds.Snapshot, DateTime.UtcNow);
            Equal(4L, page.lostObservations);
            Equal(0L, page.highWater);
            Equal(null, page.evictedFrom);
        });
        Check("recorder bounded count eviction tail and precise prefix cursors", () =>
        {
            var context = new LifecycleTraceContext(Mod.Worlds.Current);
            var journal = ConstructionLifecycleTrace.Journal;
            for (int i = 0; i < 300; i++)
                Record(journal, context);
            var page = Page();
            Equal(256, page.retainedCount);
            Equal(44L, page.evictedEvents);
            Equal(45L, page.retainedFloor);
            Equal(224L, page.tailOmitted);
            Equal(269L, page.events[0].sequence);
            page = Page("0");
            Equal(1L, page.evictedFrom);
            Equal(44L, page.evictedThrough);
            Equal(76L, page.nextSequence);
            True(page.hasMore);
            page = Page("300");
            Equal(0, page.events.Length);
            Equal(300L, page.nextSequence);
            Equal(false, page.hasMore);
        });
        Check("journal actual UTF8 sizes/ref snapshots are immutable and bounded", () =>
        {
            var context = new LifecycleTraceContext(Mod.Worlds.Current);
            var refs = Enumerable.Range(1, 10).Select(i => new LifecycleTraceReference(TraceRole.target, context.ingressWorld, i, 2)).ToArray();
            Record(ConstructionLifecycleTrace.Journal, context, refs: refs);
            refs[0] = null;
            var page = Page();
            Equal(4, page.events[0].references.Count);
            Equal(6, page.events[0].omittedReferences);
            Equal(1, page.events[0].references[0].index);
            Equal((long)ObservationWire.TraceEvent(page.events[0]).Length, page.retainedBytes);
            True(page.retainedBytes <= 8192);
            True(ObservationWire.Standalone(page).Length <= 262144);
        });
        Check("oversized events lost without allocating sequence or fake gap", () =>
        {
            var context = new LifecycleTraceContext(new WorldRef(new string ('雪', 10000), 1));
            Record(ConstructionLifecycleTrace.Journal, context);
            Equal(1L, Page().lostObservations);
            Equal(0L, Page().highWater);
            Equal(null, Page().evictedFrom);
        });
        Check("sequence and loss counters saturate without rollover", () =>
        {
            var journal = ConstructionLifecycleTrace.Journal;
            typeof(LifecycleTraceJournal).GetField("sequence", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(journal, LifecycleTraceJournal.MaxSafe);
            typeof(LifecycleTraceJournal).GetField("lost", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(journal, LifecycleTraceJournal.MaxSafe);
            Record(journal, new LifecycleTraceContext(Mod.Worlds.Current));
            True(Page().saturated);
            Equal(LifecycleTraceJournal.MaxSafe, Page().highWater);
            Equal(LifecycleTraceJournal.MaxSafe, Page().lostObservations);
        });
        Check("all actual generic queue modes retain distinct fixed kind and original response shapes", () =>
        {
            var queues = new Func<BridgeToolSystem, BridgeRequest, bool>[]
            {
                (t, r) => t.TryQueueRoad(default, null, default, default, default, false, default, r),
                (t, r) => t.TryQueueUpgrade(default, "private label", default, r),
                (t, r) => t.TryQueueNodeUpgrade(default, "private label", default, r),
                (t, r) => t.TryQueueDemolish(default, "private label", r),
                (t, r) => t.TryQueueArea(default, null, new float3[3], r)
            };
            var kinds = new[]
            {
                "net",
                "upgrade",
                "nodeUpgrade",
                "demolish",
                "area"
            };
            for (int i = 0; i < queues.Length; i++)
            {
                Reset();
                var r = Request();
                var t = new BridgeToolSystem();
                True(queues[i](t, r));
                t.Step();
                t.Step();
                t.Step();
                Equal(200, r.WaitForResponse(1).Status);
                Equal(kinds[i], Page("0").events.First().kind);
            }
        });
        Check("actual road replace and transit rejection timeout displacement paths retain policy", () =>
        {
            var r = Request();
            var road = new BridgeRoadToolSystem();
            True(road.TryQueueReplace(Array.Empty<Entity>(), default, null, false, r));
            road.Step();
            road.Step();
            road.Step();
            Equal(200, r.WaitForResponse(1).Status);
            Equal("roadReplace", Page("0").events.First().kind);
            foreach (string mode in new[]
            {
                "stopReject",
                "lineReject",
                "timeout",
                "displace"
            }

            )
            {
                Reset();
                r = Request();
                var t = Transit(r, mode != "stopReject");
                if (mode == "displace")
                {
                    t.Active = new ToolBaseSystem();
                    t.Stop();
                }
                else
                {
                    t.Allow = false;
                    t.Step();
                    if (mode == "timeout")
                    {
                        t.Ready = false;
                        t.Expire();
                    }

                    t.Step();
                    t.Step();
                }

                Equal(409, r.WaitForResponse(1).Status);
                Equal(false, t.IsBusy);
                True(!Page("0").events.Any(e => e.phase == "applyObserved"));
            }
        });
        Check("actual transit definition regeneration emits no repeated wait events", () =>
        {
            var r = Request();
            var t = Transit(r, true);
            t.Step();
            int before = Page("0").retainedCount;
            t.PathUpdate = true;
            t.Step();
            t.Step();
            Equal(before, Page("0").retainedCount);
            Equal(3, t.Calls.Count(x => x == "definitions"));
            t.PathUpdate = false;
            t.Step();
            t.Step();
            Equal(200, r.WaitForResponse(1).Status);
        });
        Check("actual token and frame observation failures cannot break queue Apply Finish or reply", () =>
        {
            var r = Request();
            r.Path = null;
            var t = Generic(r);
            t.Step();
            t.Step();
            t.Step();
            Equal(200, r.WaitForResponse(1).Status);
            Equal(null, r.LifecycleContext);
            True(Page().lostObservations > 0);
            Reset();
            BridgeSystem.FrameFault = true;
            try
            {
                r = Request();
                t = Generic(r);
                t.Step();
                t.Step();
                t.Step();
                Equal(200, r.WaitForResponse(1).Status);
                Equal(false, t.IsBusy);
                var reply = Page("0").events.Single();
                Equal("replyAttempt", reply.phase);
                Equal(null, reply.frame);
                True(Page().lostObservations > 0);
            }
            finally
            {
                BridgeSystem.FrameFault = false;
            }
        });
        Check("lost ownership does not claim restoration attempt on actual generic Finish", () =>
        {
            var r = Request();
            var t = Generic(r);
            t.Step();
            t.Step();
            t.Active = new ToolBaseSystem();
            t.Step();
            Equal(false, t.IsBusy);
            Equal(false, Page("0").events.Any(e => e.phase == "restorationAttempt"));
        });
        Check("journal serialized byte ceiling and concurrent snapshots preserve bounds", () =>
        {
            var j = NewJournal(_ => 8192);
            var context = new LifecycleTraceContext(Mod.Worlds.Current);
            for (int i = 0; i < 257; i++)
                Record(j, context);
            var page = j.Read(LifecycleTraceQuery.Parse(new Dictionary<string, string>()), Mod.Worlds.Snapshot, DateTime.UtcNow);
            True(page.retainedBytes + page.retainedCount + 1 <= 2097152L);
            Equal(255, page.retainedCount);
            Equal(2L, page.evictedEvents);
            j = NewJournal();
            System.Threading.Tasks.Parallel.For(0, 1000, i =>
            {
                Record(j, context);
                var p = j.Read(LifecycleTraceQuery.Parse(new Dictionary<string, string>()), Mod.Worlds.Snapshot, DateTime.UtcNow);
                True(p.retainedCount <= 256);
                True(p.retainedBytes <= 2097152);
            });
            page = j.Read(LifecycleTraceQuery.Parse(new Dictionary<string, string>()), Mod.Worlds.Snapshot, DateTime.UtcNow);
            Equal(1000L, page.highWater);
            Equal(744L, page.evictedEvents);
            Equal(0L, page.lostObservations);
        });
        Check("strict trace query cursor boot and decoded duplicate validation", () =>
        {
            foreach (var raw in new[]
            {
                "x=1",
                "limit=0",
                "limit=33",
                "afterSequence=0",
                "bootId=boot&afterSequence=-1",
                "bootId=boot&afterSequence=9007199254740992",
                "limit=1&l%69mit=2"
            }

            )
                Reject(() => LifecycleTraceQuery.DecodeQuery(raw));
            Reject(() => ConstructionLifecycleTrace.Read(new Dictionary<string, string> { { "bootId", "wrong" } }), 409);
            Reject(() => ConstructionLifecycleTrace.Read(new Dictionary<string, string> { { "bootId", "boot" }, { "afterSequence", "1" } }));
        });
        Check("actual O7 typed catalog binding retains history warning and outer current span", () =>
        {
            var request = Request();
            var tool = Generic(request);
            tool.Step();
            tool.Step();
            tool.Step();
            var parsed = ObservationCatalog.Parse("{\"items\":[{\"id\":\"trace\",\"name\":\"construction.lifecycleTrace\",\"args\":{\"limit\":16}}]}");
            var catalog = new ObservationCatalog(new RequestHandlers(), () => 10, () => DateTime.UtcNow);
            var scheduler = new ObservationScheduler(Mod.Worlds, catalog, () => 10, () => 0, new ObservationWire());
            byte[] bytes = null;
            var group = scheduler.Admit(parsed, b => bytes = b);
            scheduler.Pump();
            Equal(null, group.Result.results[0].error);
            True(group.Result.results[0].observation.warnings.Contains("managedHistoryNotNativeObservation"));
            True(bytes.Length <= 262144);
            group.Detach();
            if (args.Length > 0)
            {
                Directory.CreateDirectory(args[0]);
                File.WriteAllBytes(Path.Combine(args[0], "lifecycle-group.json"), bytes);
                File.WriteAllBytes(Path.Combine(args[0], "lifecycle-page.json"), ObservationWire.Standalone(Page()));
                File.WriteAllBytes(Path.Combine(args[0], "lifecycle-metadata.json"), BridgeResponse.Json(new { endpoint = EndpointCatalog.All.Single(e => e.path == "/diagnostics/construction-lifecycle"), nativeValidation = Mod.NativeValidation, capability = EndpointCatalog.CreateOperationManifest(Mod.Identity, Mod.RuntimeSnapshots.Read(DateTime.UtcNow), Mod.NativeValidation, Mod.Worlds.Snapshot).capabilities.Single(c => c.name == "constructionLifecycleTrace") }).Body);
            }
        });
        Check("actual HTTP ingress plus production Handle reads degraded history during loading", () =>
        {
            Mod.Worlds.MarkUnavailable();
            var handlers = new RequestHandlers();
            int called = 0;
            var server = new HttpBridgeServer(0, r =>
            {
                called++;
                return BridgeSystem.Instance.EnqueueAndPump(r);
            }, _ => throw new Exception("operation routing"), Mod.Worlds);
            try
            {
                server.Start();
                var listener = (TcpListener)typeof(HttpBridgeServer).GetField("m_Listener", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(server);
                string Send(string target, string method = "GET", string body = "")
                {
                    using (var c = new TcpClient())
                    {
                        c.Connect(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
                        c.ReceiveTimeout = 3000;
                        var data = Encoding.UTF8.GetBytes(method + " " + target + " HTTP/1.1\r\nHost: localhost\r\nContent-Length: " + Encoding.UTF8.GetByteCount(body) + "\r\n\r\n" + body);
                        c.GetStream().Write(data, 0, data.Length);
                        using (var reader = new StreamReader(c.GetStream()))
                            return reader.ReadToEnd();
                    }
                }

                string response = Send("/diagnostics/construction-lifecycle");
                True(response.StartsWith("HTTP/1.1 200"));
                True(response.Contains("\"readReady\":false"));
                Equal(1, called);
                foreach (var path in new[]
                {
                    "?x=1",
                    "?limit=2&l%69mit=3",
                    "?afterSequence=0"
                }

                )
                    True(Send("/diagnostics/construction-lifecycle" + path).StartsWith("HTTP/1.1 400"));
                True(!Send("/diagnostics/construction-lifecycle", "POST").StartsWith("HTTP/1.1 200"));
                True(!Send("/diagnostics/construction-lifecycle", "GET", "{}").StartsWith("HTTP/1.1 200"));
                Equal(1, called);
            }
            finally
            {
                server.Stop();
            }
        });
        SaveTaskObservationTests.Run(Check, args.Length > 0 ? args[0] : null);
        Console.WriteLine($"Lifecycle: {passed + failed} checks, {passed} passed, {failed} failed");
        return failed == 0 ? 0 : 1;
    }
}
