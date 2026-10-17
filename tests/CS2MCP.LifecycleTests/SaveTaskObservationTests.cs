using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CS2MCP;
using CS2MCP.Core;
using Newtonsoft.Json.Linq;

internal static class SaveTaskObservationTests
{
    private static readonly AsyncLocal<string> Ambient = new AsyncLocal<string>();

    private static void Equal(object expected, object actual)
    {
        if (!Equals(expected, actual)) throw new Exception($"expected {expected}, got {actual}");
    }

    private static void True(bool value) => Equal(true, value);

    private static LifecycleTracePage Page(LifecycleTraceJournal journal = null) => (journal ?? ConstructionLifecycleTrace.Journal).Read(
        LifecycleTraceQuery.Parse(new Dictionary<string, string> { { "limit", "32" } }),
        Mod.Worlds.Snapshot, DateTime.UtcNow);

    private static JObject Wire(LifecycleTraceJournal journal = null) => JObject.Parse(Encoding.UTF8.GetString(ObservationWire.Standalone(Page(journal))));

    private static long Counter(string key, LifecycleTraceJournal journal = null)
    {
        var value = Wire(journal)["saveTaskObservers"]?[key];
        if (value == null) throw new Exception("missing produced saveTaskObservers." + key);
        return (long)value;
    }

    private static void Await(Func<bool> predicate) => True(SpinWait.SpinUntil(predicate, 5000));

    private static LifecycleTraceEvent[] Events(LifecycleTraceJournal journal = null)
    {
        journal = journal ?? ConstructionLifecycleTrace.Journal;
        var rows = new List<LifecycleTraceEvent>();
        long after = 0;
        for (int page = 0; page < 8; page++)
        {
            var result = journal.Read(LifecycleTraceQuery.Parse(new Dictionary<string, string>
            {
                {
                    "bootId","boot"
                },
                {
                    "afterSequence",after.ToString(CultureInfo.InvariantCulture)
                },
                {
                    "limit","32"
                }
            }), Mod.Worlds.Snapshot, DateTime.UtcNow);
            rows.AddRange(result.events);
            if (!result.hasMore) return rows.ToArray();
            after = result.nextSequence.Value;
        }
        throw new Exception("test exceeded bounded journal traversal");
    }

    private static void HasAttempt() => True(Events().Any(e => e.phase == "saveCallAttempt"));

    private static void Finished() => Await(() => Counter("pending") == 0);

    private sealed class Scope : IDisposable
    {
        private readonly List<TaskCompletionSource<bool>> inputs = new List<TaskCompletionSource<bool>>();

        private readonly List<LifecycleTraceJournal> owners = new List<LifecycleTraceJournal>();
        internal BridgeRequest Request;

        internal Scope()
        {
            SaveNativeBoundary.Reset();
            UseRecorder(e => ObservationWire.TraceEvent(e).Length);
        }

        internal LifecycleTraceJournal UseRecorder(Func<LifecycleTraceEvent, int> measure)
        {
            var journal = new LifecycleTraceJournal("boot", measure);
            ConstructionLifecycleTrace.Journal = journal;
            owners.Add(journal);
            return journal;
        }

        internal TaskCompletionSource<bool> Pending()
        {
            var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            inputs.Add(source);
            return source;
        }

        internal Task<bool> Terminal(string state)
        {
            var source = Pending();
            Complete(source, state);
            return source.Task;
        }

        internal BridgeResponse Invoke(Task<bool> task, string name = "private-save-name", string path = "/game/save")
        {
            var journal = ConstructionLifecycleTrace.Journal;
            if (!owners.Contains(journal)) owners.Add(journal);
            SaveNativeBoundary.NextTask = task;
            var handlers = new RequestHandlers();
            RequestHandlers.NativeHandle = request => handlers.InvokeSave(request);
            Request = new BridgeRequest
            {
                Method = "GET",
                Path = path,
                IngressWorld = Mod.Worlds.Current
            };
            if (name != null) Request.Query.Add("name", name);
            BridgeSystem.Instance.Dispatch(Request, true);
            return Request.WaitForResponse(1);
        }

        public void Dispose()
        {
            BridgeSystem.FrameFault = false;
            foreach (var source in inputs) source.TrySetResult(true);
            foreach (var journal in owners)
            {
                if (Wire(journal)["saveTaskObservers"] != null)
                    Await(() => Counter("pending", journal) == 0);
            }
            RequestHandlers.NativeHandle = null;
            SaveNativeBoundary.OnNativeSave = null;
        }
    }

    private static void Complete(TaskCompletionSource<bool> source, string state)
    {
        switch (state)
        {
            case "true":
                source.SetResult(true);
                break;
            case "false":
                source.SetResult(false);
                break;
            case "faulted":
                source.SetException(new InvalidOperationException("private-task-fault-body"));
                break;
            case "canceled":
                source.SetCanceled();
                break;
            default: throw new Exception("unknown test task state");
        }
    }

    private static string Phase(string state) => "saveTask" + char.ToUpperInvariant(state[0]) + state.Substring(1);

    private static void Legacy(BridgeResponse response, string name = "private-save-name")
    {
        Equal(200, response.Status);
        Equal("{\"saving\":true,\"name\":\"" + name + "\",\"note\":\"save runs asynchronously; it appears in the game's load menu when finished\"}", Encoding.UTF8.GetString(response.Body));
        Equal(name, SaveNativeBoundary.SavedName);
        Equal(SaveNativeBoundary.Menu.Info, SaveNativeBoundary.SavedInfo);
        Equal(SaveNativeBoundary.Preview, SaveNativeBoundary.SavedPreview);
        True(SaveNativeBoundary.SavedDatabase != null);
    }

    private static void NativeOrder() => Equal("city,menu,saveInfo:False,preview:PreviewSaveGame-CS2MCP:680:383,camera,capture,manager,database,save", string.Join(",", SaveNativeBoundary.Calls));

    private static void AssertTerminal(string phase, LifecycleTraceJournal journal = null)
    {
        var terminal = Events(journal).Single(e => e.phase == phase);
        Equal("save", terminal.adapter);
        Equal("save", terminal.kind);
        Equal("gameSave", terminal.route);
        Equal(null, terminal.frame);
        Equal("notSampled", terminal.frameReason);
        Equal(null, terminal.ownsActiveTool);
        Equal(null, terminal.status);
        Equal(0, terminal.references.Count);
        Equal("unknown", terminal.effects);
        Equal("unverified", terminal.cleanup);
        Equal("unverified", terminal.authority);
        var text = Encoding.UTF8.GetString(ObservationWire.Standalone(Page(journal)));
        True(!text.Contains("private-save-name") && !text.Contains("private-task-fault-body"));
    }

    private static void Export(string directory, string name)
    {
        if (directory == null) return;
        Directory.CreateDirectory(directory);
        var input = ObservationCatalog.Parse("{\"items\":[{\"id\":\"save\",\"name\":\"construction.lifecycleTrace\",\"args\":{\"limit\":32}}]}");
        var scheduler = new ObservationScheduler(Mod.Worlds, new ObservationCatalog(new RequestHandlers(), () => 10, () => DateTime.UtcNow), () => 10, () => 0, new ObservationWire());
        byte[] bytes = null;
        var group = scheduler.Admit(input, value => bytes = value);
        scheduler.Pump();
        Equal(null, group.Result.results[0].error);
        True(bytes.Length <= 262144);
        File.WriteAllBytes(Path.Combine(directory, "lifecycle-save-" + name + ".json"), bytes);
        group.Detach();
    }

    internal static void Run(Action<string, Action> check, string wireDirectory)
    {
        check("actual SaveGame keeps immediate legacy response and exposes delayed task outcome", () =>
        {
            using (var scope = new Scope())
            {
                var task = scope.Pending();
                var response = scope.Invoke(task.Task);
                Legacy(response);
                NativeOrder();
                HasAttempt();
                Equal(32L, Counter("capacity"));
                Equal(1L, Counter("pending"));
                True(!Events().Any(e => e.phase == "saveTaskTrue"));
                task.SetResult(true);
                Finished();
                AssertTerminal("saveTaskTrue");
            }
        });
        foreach (string name in new[] { (string)null, "" })
            check("actual SaveGame retains default name for " + (name == null ? "omitted" : "empty") + " input", () =>
            {
                using (var scope = new Scope())
                {
                    var response = scope.Invoke(scope.Terminal("true"), name);
                    string actual = (string)JObject.Parse(Encoding.UTF8.GetString(response.Body))["name"];
                    True(System.Text.RegularExpressions.Regex.IsMatch(actual, @"^CS2MCP \d{4}-\d{2}-\d{2} \d{2}-\d{2}-\d{2}$"));
                    Legacy(response, actual);
                    NativeOrder();
                    Finished();
                    AssertTerminal("saveTaskTrue");
                }
            });
        foreach (string state in new[] { "true", "false", "faulted", "canceled" })
            check("actual SaveGame observes already-terminal " + state + " without changing response", () =>
            {
                using (var scope = new Scope())
                {
                    Legacy(scope.Invoke(scope.Terminal(state)));
                    NativeOrder();
                    HasAttempt();
                    Finished();
                    AssertTerminal(Phase(state));
                    Export(wireDirectory, state);
                }
            });
        foreach (string state in new[] { "false", "faulted", "canceled" })
            check("actual SaveGame observes delayed " + state + " after legacy reply", () =>
            {
                using (var scope = new Scope())
                {
                    var task = scope.Pending();
                    Legacy(scope.Invoke(task.Task));
                    HasAttempt();
                    Equal(1L, Counter("pending"));
                    Complete(task, state);
                    Finished();
                    AssertTerminal(Phase(state));
                }
            });
        check("actual save city/menu/preparation guards retain original calls and no task capture", () =>
        {
            foreach (string boundary in new[] { "city", "menu", "saveInfo:False", "capture" })
            using (var scope = new Scope())
            {
                if (boundary == "city") SaveNativeBoundary.CityAvailable = false;
                else if (boundary == "menu") SaveNativeBoundary.MenuAvailable = false;
                else SaveNativeBoundary.ThrowAt = boundary;
                var response = scope.Invoke(scope.Pending().Task);
                Equal(boundary == "city" || boundary == "menu" ? 503 : 500, response.Status);
                True(!SaveNativeBoundary.Calls.Contains("save"));
                Equal(0L, Counter("pending"));
                Equal(0L, Counter("attachmentFailures"));
                True(!Events().Any(e => e.phase.StartsWith("saveCall")));
            }
        });
        check("actual save call-expression getter/invocation throws preserve legacy exceptions and status", () =>
        {
            foreach (string boundary in new[] { "manager", "database", "save" })
            using (var scope = new Scope())
            {
                SaveNativeBoundary.ThrowAt = boundary;
                var response = scope.Invoke(scope.Pending().Task);
                Equal(500, response.Status);
                Equal(0L, Counter("pending"));
                HasAttempt();
                Equal(1, Events().Count(e => e.phase == "saveCallThrew"));
                True(!Events().Any(e => e.phase == "saveCallReturned"));
                True(!Encoding.UTF8.GetString(ObservationWire.Standalone(Page())).Contains("private-native-failure"));
                try
                {
                    new RequestHandlers().InvokeSave(scope.Request);
                    throw new Exception("native throw was swallowed");
                }
                catch (InvalidOperationException error)
                {
                    Equal("private-native-failure", error.Message);
                }
            }
        });
        check("actual save null returned task remains saving and exposes unavailable observation", () =>
        {
            using (var scope = new Scope())
            {
                Legacy(scope.Invoke(null));
                NativeOrder();
                HasAttempt();
                Equal(0L, Counter("pending"));
                Equal(1L, Counter("attachmentFailures"));
                Equal(1L, Page().lostObservations);
                Equal(1, Events().Count(e => e.phase == "saveObserverUnavailable"));
            }
        });
        check("injected registration failure releases its reservation and preserves caller flow", () =>
        {
            using (var scope = new Scope())
            {
                var task = scope.Pending();
                using (ExecutionContext.SuppressFlow())
                {
                    Legacy(scope.Invoke(task.Task));
                    Equal(1L, Counter("pending"));
                    string calls = string.Join(",", SaveNativeBoundary.Calls);
                    var observation = BridgeSaveTaskObservation.Begin(scope.Request);
                    observation.Returned(task.Task, (antecedent, state) =>
                    {
                        Equal(task.Task, antecedent);
                        True(state != null);
                        True(ExecutionContext.IsFlowSuppressed());
                        Equal(2L, Counter("pending"));
                        throw new InvalidOperationException("private-registration-failure");
                    });
                    True(ExecutionContext.IsFlowSuppressed());
                    Equal(calls, string.Join(",", SaveNativeBoundary.Calls));
                }
                Equal(false, ExecutionContext.IsFlowSuppressed());
                HasAttempt();
                Equal(1L, Counter("pending"));
                Equal(1L, Counter("attachmentFailures"));
                Equal(1L, Page().lostObservations);
                Equal(1, Events().Count(e => e.phase == "saveObserverUnavailable"));
                task.SetResult(true);
                Finished();
                AssertTerminal("saveTaskTrue");
            }
        });
        check("32 never-ending save observers survive reload and never gate additional native saves", () =>
        {
            using (var scope = new Scope())
            {
                var tasks = new List<TaskCompletionSource<bool>>();
                for (int i = 0; i < 32; i++)
                {
                    var task = scope.Pending();
                    tasks.Add(task);
                    Legacy(scope.Invoke(task.Task));
                }
                Equal(32L, Counter("pending"));
                Mod.Worlds.BeginLoad();
                Mod.Worlds.CompleteLoad();
                var skipped = scope.Pending();
                Legacy(scope.Invoke(skipped.Task));
                var skippedId = scope.Request.LifecycleContext.traceId;
                Equal(33, SaveNativeBoundary.Calls.Count(e => e == "save"));
                Equal(32L, Counter("pending"));
                Equal(1L, Counter("skippedAtCapacity"));
                Equal(1L, Page().lostObservations);
                Equal(1, Events().Count(e => e.phase == "saveObserverSkipped"));
                Export(wireDirectory, "capacity");
                skipped.SetResult(true);
                tasks[0].SetResult(true);
                Await(() => Counter("pending") == 31);
                var next = scope.Pending();
                Legacy(scope.Invoke(next.Task));
                Equal(32L, Counter("pending"));
                foreach (var task in tasks) task.TrySetResult(true);
                next.SetResult(true);
                Finished();
                Equal(33, Events().Count(e => e.phase == "saveTaskTrue"));
                True(!Events().Any(e => e.traceId == skippedId && e.phase.StartsWith("saveTask")));
            }
        });
        check("concurrent save terminal callbacks release bounded reservations exactly once", () =>
        {
            using (var scope = new Scope())
            {
                var tasks = Enumerable.Range(0, 32).Select(_ => scope.Pending()).ToArray();
                foreach (var task in tasks) Legacy(scope.Invoke(task.Task));
                Equal(32L, Counter("pending"));
                System.Threading.Tasks.Parallel.For(0, tasks.Length, i => Complete(tasks[i], new[] { "true", "false", "faulted", "canceled" }
                [i % 4]));
                Finished();
                var terminal = Events().Where(e => e.phase.StartsWith("saveTask")).ToArray();
                Equal(32, terminal.Length);
                Equal(32, terminal.Select(e => e.traceId).Distinct().Count());
                foreach (var task in tasks) Equal(false, task.TrySetResult(false));
                Equal(0L, Counter("pending"));
            }
        });
        check("save recorders can succeed drop or throw without changing native calls or responses", () =>
        {
            foreach (string state in new[] { "true", "false", "faulted", "canceled" })
            {
                var outcomes = new List<string>();
                foreach (int mode in new[] { 0, 1, 2 }) using (var scope = new Scope())
                {
                    scope.UseRecorder(e => mode == 0 ? ObservationWire.TraceEvent(e).Length : mode == 1 ? 0 : throw new Exception("private-recorder-failure"));
                    var response = scope.Invoke(scope.Terminal(state));
                    Finished();
                    outcomes.Add(string.Join(",", SaveNativeBoundary.Calls) + "|" + response.Status + "|" + Convert.ToBase64String(response.Body));
                    Equal(0L, Counter("pending"));
                    if (mode != 0) True(Page().lostObservations > 0);
                }
                Equal(outcomes[0], outcomes[1]);
                Equal(outcomes[0], outcomes[2]);
            }
        });
        check("save continuation stays ambient-free on default scheduler with no native getter", () =>
        {
            using (var scope = new Scope())
            {
                int callbackThread = 0;
                string ambient = "unset";
                bool defaultScheduler = false;
                scope.UseRecorder(e =>
                {
                    if (e.phase == "saveTaskTrue")
                    {
                        ambient = Ambient.Value;
                        callbackThread = Thread.CurrentThread.ManagedThreadId;
                        defaultScheduler = TaskScheduler.Current == TaskScheduler.Default && SynchronizationContext.Current == null;
                    }
                    return ObservationWire.TraceEvent(e).Length;
                });
                var task = scope.Pending();
                Ambient.Value = "private-ambient-name";
                try
                {
                    SaveNativeBoundary.OnNativeSave = () =>
                    {
                        Equal("private-ambient-name", Ambient.Value);
                        Equal(false, ExecutionContext.IsFlowSuppressed());
                    };
                    Legacy(scope.Invoke(task.Task));
                    Equal("private-ambient-name", Ambient.Value);
                    Equal(false, ExecutionContext.IsFlowSuppressed());
                    string calls = string.Join(",", SaveNativeBoundary.Calls);
                    BridgeSystem.FrameFault = true;
                    task.SetResult(true);
                    Finished();
                    Equal(null, ambient);
                    True(defaultScheduler);
                    True(callbackThread != SaveNativeBoundary.MainThread);
                    Equal(calls, string.Join(",", SaveNativeBoundary.Calls));
                    AssertTerminal("saveTaskTrue");
                }
                finally
                {
                    Ambient.Value = null;
                    BridgeSystem.FrameFault = false;
                }
            }
        });
        check("save terminal keeps original journal and world through reload and journal replacement", () =>
        {
            using (var scope = new Scope())
            {
                var task = scope.Pending();
                Legacy(scope.Invoke(task.Task));
                var context = scope.Request.LifecycleContext;
                var owner = ConstructionLifecycleTrace.Journal;
                Equal(1L, Counter("pending", owner));
                Mod.Worlds.BeginLoad();
                BridgeSystem.FrameFault = true;
                var replacement = scope.UseRecorder(e => ObservationWire.TraceEvent(e).Length);
                task.SetResult(true);
                Await(() => Counter("pending", owner) == 0);
                Equal(0, Events(replacement).Length);
                Equal(0L, Counter("pending", replacement));
                var terminal = Events(owner).Single(e => e.phase == "saveTaskTrue");
                Equal(context.traceId, terminal.traceId);
                True(WorldIdentity.Same(context.ingressWorld, terminal.ingressWorld));
                Equal(false, terminal.observedReady);
                True(!WorldIdentity.Same(terminal.ingressWorld, terminal.observedWorld));
                Equal(null, terminal.frame);
                Equal(null, terminal.ownsActiveTool);
                ConstructionLifecycleTrace.Journal = owner;
                var direct = new RequestHandlers().Handle(new BridgeRequest
                {
                    Method = "GET",
                    Path = "/diagnostics/construction-lifecycle"
                });
                Equal(200, direct.Status);
                Equal(false, (bool)JObject.Parse(Encoding.UTF8.GetString(direct.Body))["readReady"]);
                BridgeSystem.FrameFault = false;
                Mod.Worlds.CompleteLoad();
                Export(wireDirectory, "old-world");
            }
        });
        check("missing diagnostic context cannot prevent save or strand a reservation", () =>
        {
            using (var scope = new Scope())
            {
                Legacy(scope.Invoke(scope.Pending().Task, path: null));
                Equal(null, scope.Request.LifecycleContext);
                Equal(0L, Counter("pending"));
                Equal(1L, Counter("attachmentFailures"));
                True(Page().lostObservations > 0);
            }
        });
        check("save observation counters saturate without rollover or changing native saves", () =>
        {
            using (var scope = new Scope())
            {
                var journal = ConstructionLifecycleTrace.Journal;
                typeof(LifecycleTraceJournal).GetField("saveAttachmentFailures", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(journal, LifecycleTraceJournal.MaxSafe);
                typeof(LifecycleTraceJournal).GetField("lost", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(journal, LifecycleTraceJournal.MaxSafe);
                Legacy(scope.Invoke(null));
                Equal(LifecycleTraceJournal.MaxSafe, Counter("attachmentFailures"));
                Equal(LifecycleTraceJournal.MaxSafe, Page().lostObservations);
                True(Page().saturated);
                for (int i = 0; i < 32; i++) Legacy(scope.Invoke(scope.Pending().Task));
                typeof(LifecycleTraceJournal).GetField("saveSkipped", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(journal, LifecycleTraceJournal.MaxSafe);
                Legacy(scope.Invoke(scope.Pending().Task));
                Equal(LifecycleTraceJournal.MaxSafe, Counter("skippedAtCapacity"));
                Equal(32L, Counter("pending"));
            }
        });
    }
}
