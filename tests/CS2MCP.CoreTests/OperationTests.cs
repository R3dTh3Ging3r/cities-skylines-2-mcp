using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CS2MCP.Core;

namespace CS2MCP.CoreTests
{
    internal static class OperationTests
    {
        private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
        private static WorldIdentity Ready() { var w = new WorldIdentity("boot"); w.BeginLoad(); w.CompleteLoad(); return w; }
        private static CommandRequest Request(WorldIdentity w, string id = null, string args = "{}", string lease = "lease", string fingerprint = null)
            => new CommandRequest(id ?? Guid.NewGuid().ToString(), w.Current, lease, "cs2_build_road", args, fingerprint);
        private static void Code(string code, Action action)
        {
            try { action(); } catch (OperationException e) { Program.Equal(code, e.Error.code); return; }
            throw new Exception("expected " + code);
        }
        public static void Run()
        {
            Program.Check("same_id_same_request_returns_original", () => {
                var w = Ready(); var r = new OperationRegistry(w, _ => null); var q = Request(w);
                var first = r.AdmitWithDisposition(q, q.RequestHash, Now);
                var replay = r.AdmitWithDisposition(q, q.RequestHash, Now.AddSeconds(1));
                Program.Equal(true, first.IsNew); Program.Equal(false, replay.IsNew);
                Program.Equal(first.Record.createdAtUtc, replay.Record.createdAtUtc);
                int starts = 0;
                Parallel.For(0, 32, _ => { if (r.AdmitWithDisposition(q, q.RequestHash, Now).IsNew) System.Threading.Interlocked.Increment(ref starts); });
                Program.Equal(0, starts);
            });
            Program.Check("typed_phase_transition_preserves_wire_phase", () => {
                var w=Ready(); var r=new OperationRegistry(w,_=>null); var q=Request(w);r.Admit(q,q.RequestHash,Now);
                Program.Equal("validating",r.Transition(q.operationId,OperationPhase.Validating,null,null,Now).phase);
            });
            Program.Check("different_payload_conflicts", () => {
                var w = Ready(); var r = new OperationRegistry(w, _ => null); var q = Request(w);
                r.Admit(q, q.RequestHash, Now);
                foreach (var other in new[] { Request(w, q.operationId, "{\"x\":2}"), Request(w, q.operationId, fingerprint:"changed") })
                    Code("IDEMPOTENCY_CONFLICT", () => r.Admit(other, other.RequestHash, Now));
            });
            Program.Check("canonical_hash_sorts_parsed_arguments", () => {
                var w = Ready(); var a = Request(w, args:"{\"z\": [true, null], \"a\":\"\\u0061\"}");
                var b = Request(w, a.operationId, "{\"a\":\"a\",\"z\":[true,null]}");
                Program.Equal(a.RequestHash, b.RequestHash);
                Program.Equal(false, a.RequestHash == Request(w, a.operationId, "{}", fingerprint:"x").RequestHash);
            });
            Program.Check("full_registry_keeps_old_keys", () => {
                var w = Ready(); var r = new OperationRegistry(w, _ => null); CommandRequest first = null;
                for (int i = 0; i < 2048; i++) { var q = Request(w); if (i == 0) first = q;
                    r.Admit(q, q.RequestHash, Now); r.RequestCancel(q.operationId); }
                var excess = Request(w); Code("OPERATION_CAPACITY", () => r.Admit(excess, excess.RequestHash, Now));
                Program.Equal(first.operationId, r.Get(first.operationId).operationId);
                Program.Equal(false, r.AdmitWithDisposition(first, first.RequestHash, Now).IsNew);
            });
            Program.Check("old_world_rejected_before_apply", () => {
                var w = Ready(); var r = new OperationRegistry(w, _ => null); var q = Request(w); int apply = 0;
                r.Admit(q, q.RequestHash, Now); var old = w.Current; w.BeginLoad();
                Program.Equal(old.epoch + 1, w.Current.epoch); Program.Equal(false, w.IsReady);
                Code("WORLD_MISMATCH", () => { r.ValidateForApply(q.operationId); apply++; });
                w.CompleteLoad(); Code("WORLD_MISMATCH", () => r.Admit(q, q.RequestHash, Now));
                Program.Equal(0, apply); Program.Equal("interrupted", r.Get(q.operationId).phase);
            });
            Program.Check("lease_authority_is_rechecked_on_replay_and_apply", () => {
                var w = Ready(); bool authorized = true;
                var r = new OperationRegistry(w, _ => authorized ? null : new BridgeErrorData("LOCKED", "revoked"));
                var q = Request(w); r.Admit(q, q.RequestHash, Now); authorized = false;
                Code("LOCKED", () => r.Admit(q, q.RequestHash, Now)); Code("LOCKED", () => r.ValidateForApply(q.operationId));
                authorized = true; var other = Request(w, q.operationId, lease:"other");
                Code("LOCKED", () => r.Admit(other, other.RequestHash, Now));
            });
            Program.Check("detached_records_and_terminal_result_cannot_be_replaced", () => {
                var w = Ready(); var r = new OperationRegistry(w, _ => null); var q = Request(w);
                var record = r.Admit(q, q.RequestHash, Now); record.error = new BridgeErrorData("fake", "fake");
                Program.Equal(null, r.Get(q.operationId).error);
                r.Transition(q.operationId, "validating", null, null, Now);
                r.Transition(q.operationId, "applying", null, null, Now);
                r.Transition(q.operationId, "verifying", null, null, Now);
                var receipt = new MutationReceipt { warnings = new[] { "original" }, verification = "verified" };
                r.Transition(q.operationId, "succeeded", receipt, null, Now); receipt.warnings[0] = "changed";
                Program.Equal("original", r.Get(q.operationId).receipt.warnings[0]);
                Program.Throws<InvalidOperationException>(() => r.Transition(q.operationId, "failed", null, new BridgeErrorData("fake", "fake"), Now));
                Program.Throws<InvalidOperationException>(() => r.Transition(q.operationId, "queued", null, null, Now));
            });
            Program.Check("cancel_before_apply_releases_pending_but_not_key", () => {
                var w = Ready(); var r = new OperationRegistry(w, _ => null);
                var requests = Enumerable.Range(0, 256).Select(_ => Request(w)).ToArray();
                foreach (var q in requests) r.Admit(q, q.RequestHash, Now);
                var excess = Request(w); Code("BRIDGE_BUSY", () => r.Admit(excess, excess.RequestHash, Now));
                Program.Equal("cancelled", r.RequestCancel(requests[0].operationId).phase);
                r.Admit(excess, excess.RequestHash, Now);
                Program.Equal(false, r.AdmitWithDisposition(requests[0], requests[0].RequestHash, Now).IsNew);
            });
            Program.Check("wrong_method_or_body_never_enqueued", () => {
                int queued = 0; var w = Ready(); var q = Request(w);
                string json = "{\"operationId\":\"" + q.operationId + "\",\"world\":{\"bootId\":\"boot\",\"epoch\":1},\"leaseId\":\"lease\",\"name\":\"cs2_build_road\",\"args\":{}}";
                string[] invalid = {
                    "GET /v2/commands HTTP/1.1\r\nContent-Length: 0\r\n\r\n",
                    "POST /v2/commands HTTP/1.1\r\nContent-Type: application/json\r\nContent-Length: -1\r\n\r\n",
                    "POST /v2/commands HTTP/1.1\r\nContent-Type: application/json\r\nContent-Length: nope\r\n\r\n",
                    "POST /v2/commands HTTP/1.1\r\nContent-Type: application/json\r\nContent-Length: 1048577\r\n\r\n",
                    "POST /v2/commands HTTP/1.1\r\nContent-Type: application/json\r\nContent-Length: 2\r\nContent-Length: 2\r\n\r\n{}",
                    "POST /v2/commands HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n",
                    "POST /v2/commands HTTP/1.1\r\nContent-Type: application/json\r\nContent-Length: 1\r\n\r\n{",
                    "POST /v2/commands HTTP/1.1\r\nContent-Type: application/json\r\nContent-Length: 2\r\n\r\n{}",
                    "POST /v2/operations/not-a-uuid/cancel HTTP/1.1\r\nContent-Length: 0\r\n\r\n",
                    "POST /state HTTP/1.1\r\nContent-Length: 0\r\n\r\n",
                    "GET /state HTTP/1.1\r\nContent-Length: 2\r\n\r\n{}",
                };
                foreach (var wire in invalid) {
                    try { HttpIngress.ReadAndDispatch(new MemoryStream(Encoding.UTF8.GetBytes(wire)), _ => queued++); }
                    catch (OperationException) { }
                }
                Program.Equal(0, queued);
                var body = Encoding.UTF8.GetBytes(json);
                string valid = "POST /v2/commands HTTP/1.1\r\nContent-Type: application/json\r\nContent-Length: " + body.Length + "\r\n\r\n" + json;
                HttpIngress.ReadAndDispatch(new MemoryStream(Encoding.UTF8.GetBytes(valid)), parsed => {
                    Program.Equal(q.operationId, parsed.Command.operationId); queued++;
                });
                Program.Equal(1, queued);
            });
            Program.Check("one_mib_command_is_accepted_and_larger_body_rejects_before_read", () => {
                var w = Ready(); string id = Guid.NewGuid().ToString();
                string prefix = "{\"operationId\":\""+id+"\",\"world\":{\"bootId\":\"boot\",\"epoch\":1},\"leaseId\":\"lease\",\"name\":\"cs2_build_road\",\"args\":{\"padding\":\"";
                string suffix = "\"}}";
                string json = prefix + new string('x',HttpIngress.MaxBodyBytes-prefix.Length-suffix.Length) + suffix;
                int queued=0;
                string header = "POST /v2/commands HTTP/1.1\r\nContent-Type: application/json\r\nContent-Length: ";
                HttpIngress.ReadAndDispatch(new MemoryStream(Encoding.UTF8.GetBytes(header+HttpIngress.MaxBodyBytes+"\r\n\r\n"+json)), _=>queued++);
                Program.Equal(1,queued);
                Program.Throws<OperationException>(() => HttpIngress.ReadAndDispatch(new MemoryStream(Encoding.UTF8.GetBytes(header+(HttpIngress.MaxBodyBytes+1)+"\r\n\r\n")),_=>queued++));
                Program.Equal(1,queued);
            });
            Program.Check("concurrent_new_admission_enqueues_once", () => {
                var w=Ready(); var r=new OperationRegistry(w,_=>null); var q=Request(w); int starts=0;
                Parallel.For(0,32,_=> {if(r.AdmitWithDisposition(q,q.RequestHash,Now).IsNew) System.Threading.Interlocked.Increment(ref starts);});
                Program.Equal(1,starts);
            });
            Program.Check("load_completion_before_manager_ready_survives_loading_observations", () => {
                var identity = new WorldIdentity("boot"); var observer = new WorldLoadObserver(identity);
                observer.BeginLoad(); long epoch = identity.Current.epoch;
                observer.LoadingComplete(); observer.Observe(true,false);
                Program.Equal(epoch,identity.Current.epoch); Program.Equal(false,identity.IsReady);
                observer.Observe(false,true); Program.Equal(true,identity.IsReady);
                observer.Observe(true,false); Program.Equal(epoch+1,identity.Current.epoch); Program.Equal(false,identity.IsReady);
                observer.Observe(false,true); Program.Equal(false,identity.IsReady);
                observer.LoadingComplete(); observer.Observe(false,true); Program.Equal(true,identity.IsReady);
                // Missing preload while ready still fences conservatively at the observed completion.
                observer.LoadingComplete(); Program.Equal(epoch+2,identity.Current.epoch); Program.Equal(false,identity.IsReady);
                observer.Observe(null,null); Program.Equal(false,identity.IsReady);
                observer.Observe(false,true); Program.Equal(true,identity.IsReady);
            });
            Program.Check("invalid_receipt_does_not_partially_transition", () => {
                var w = Ready(); var r = new OperationRegistry(w, _ => null); var q = Request(w); r.Admit(q,q.RequestHash,Now);
                r.Transition(q.operationId,"validating",null,null,Now); r.Transition(q.operationId,"applying",null,null,Now); r.Transition(q.operationId,"verifying",null,null,Now);
                Program.Throws<OperationException>(() => r.Transition(q.operationId,"succeeded",new MutationReceipt { result=new object(),verification="verified" },null,Now));
                Program.Equal("verifying",r.Get(q.operationId).phase);
            });
            Program.Check("receipt_snapshots_preserve_nested_numeric_scalars", () => {
                var w = Ready(); var r = new OperationRegistry(w, _ => null); var q = Request(w); r.Admit(q,q.RequestHash,Now);
                r.Transition(q.operationId,"validating",null,null,Now); r.Transition(q.operationId,"applying",null,null,Now); r.Transition(q.operationId,"verifying",null,null,Now);
                var data = new System.Collections.Generic.Dictionary<string,object> { ["amount"] = 12.5m, ["items"] = new object[] { 123L, -4.5d } };
                r.Transition(q.operationId,"succeeded",new MutationReceipt { result = data, verification="verified" },null,Now);
                var copied = (System.Collections.Generic.IDictionary<string,object>)r.Get(q.operationId).receipt.result;
                Program.Equal(12.5m, copied["amount"]); data["amount"] = 0m;
                Program.Equal(123L, ((System.Collections.Generic.IList<object>)copied["items"])[0]);
                Program.Equal(12.5m, ((System.Collections.Generic.IDictionary<string,object>)r.Get(q.operationId).receipt.result)["amount"]);
            });
            Program.Check("invalid_json_depth_duplicates_and_trailing_data_reject", () => {
                foreach (var json in new[] { "{", "{} {}", "{\"x\":1,\"x\":2}", "{\"x\":NaN}", "{\"x\":01}", "{\"x\":\"\\uD800\"}", new string('[', 65) + "0" + new string(']', 65) })
                    Program.Throws<OperationException>(() => CommandJson.Canonicalize(json));
            });
        }
    }
}
