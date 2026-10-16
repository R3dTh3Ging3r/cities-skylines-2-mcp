using System;
using CS2MCP.Core;

namespace CS2MCP.CoreTests
{
    internal static class DeferredWorkTests
    {
        private static WorldIdentity Ready()
        {
            var identity = new WorldIdentity("boot"); identity.BeginLoad(); identity.CompleteLoad(); return identity;
        }
        private static DeferredWorldWork CityWork(WorldIdentity identity, string outcome = "partial")
            => new DeferredWorldWork(identity, identity.Current, true, outcome, "original-operation");
        public static void Run()
        {
            Program.Check("screenshot_http_headers_preserve_captured_world", () => {
                var listener=new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0);listener.Start();
                try {
                    using(var sender=new System.Net.Sockets.TcpClient()) {
                        sender.Connect((System.Net.IPEndPoint)listener.LocalEndpoint);
                        using(var receiver=listener.AcceptTcpClient()) {
                            var response=new CS2MCP.BridgeResponse { Body=new byte[]{1,2},ContentType="image/png",CaptureWorld=new WorldRef("captured-boot",7),CaptureWorldReady=false };
                            typeof(CS2MCP.HttpBridgeServer).GetMethod("WriteResponse",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)
                                .Invoke(null,new object[]{sender.GetStream(),response});
                            sender.Client.Shutdown(System.Net.Sockets.SocketShutdown.Send);
                            string wire=new System.IO.StreamReader(receiver.GetStream()).ReadToEnd();
                            foreach(string expected in new[]{"X-CS2MCP-Boot-Id: captured-boot\r\n","X-CS2MCP-Epoch: 7\r\n","X-CS2MCP-World-Ready: false\r\n","Content-Type: image/png\r\n"})
                                if(!wire.Contains(expected)) throw new Exception("missing captured metadata: "+expected.Trim());
                        }
                    }
                } finally {listener.Stop();}
            });
            Program.Check("same_world_ready_deferred_stages_execute", () => {
                var identity = Ready(); var work = CityWork(identity); int rename=0, reply=0;
                Program.Equal(true,work.TryRun(()=>rename++,out var error)); Program.Equal(null,error);
                Program.Equal(true,work.TryRun(()=>reply++,out error)); Program.Equal(1,rename); Program.Equal(1,reply);
            });
            Program.Check("stale_rename_never_calls_native_or_response_closure", () => {
                var identity=Ready(); var work=CityWork(identity); var original=work.World; int native=0,reply=0;
                identity.BeginLoad(); identity.CompleteLoad();
                Program.Equal(false,work.TryRun(()=>native++,out var error));
                Program.Equal(false,work.TryRun(()=>reply++,out error));
                Program.Equal(0,native); Program.Equal(0,reply); Program.Equal(original,work.World);
                Program.Equal(ErrorCodes.WorldMismatch,error.code); Program.Equal("partial",error.outcome);
                Program.Equal(false,error.retryable); Program.Equal("original-operation",error.operationId);
            });
            Program.Check("world_change_during_rename_prevents_late_name_readback", () => {
                var identity=Ready(); var work=CityWork(identity); int rename=0, readName=0;
                bool renamed=work.TryRun(()=>{rename++;identity.BeginLoad();},out var error);
                if(renamed) work.TryRun(()=>readName++,out error);
                Program.Equal(false,renamed); Program.Equal(1,rename); Program.Equal(0,readName);
                Program.Equal("partial",error.outcome);
            });
            Program.Check("callback_error_cannot_become_successful_reply", () => {
                var identity=Ready(); var work=CityWork(identity); int reply=0;
                bool success=work.TryRun<int>(()=>throw new InvalidOperationException("name read failed"),out int result,out var error);
                if(success) reply++;
                Program.Equal(false,success); Program.Equal(0,result); Program.Equal(0,reply);
                Program.Equal("partial",error.outcome); Program.Equal(false,error.retryable);
                if(!error.message.Contains("name read failed")) throw new Exception("callback diagnostic lost");
            });
            Program.Check("tile_read_world_change_prevents_updated_write", () => {
                var identity=Ready(); var work=CityWork(identity); int exists=0,updated=0;
                bool valid=work.TryRun(()=>{exists++;identity.BeginLoad();return true;},out bool needsUpdate,out var error);
                if(valid&&needsUpdate) work.TryRun(()=>updated++,out error);
                Program.Equal(1,exists); Program.Equal(0,updated); Program.Equal(false,valid);
            });
            Program.Check("readiness_loss_prevents_city_native_callback", () => {
                var identity=Ready(); var work=CityWork(identity); int calls=0;identity.MarkUnavailable();
                Program.Equal(false,work.TryRun(()=>calls++,out var error));Program.Equal(0,calls);
                Program.Equal(ErrorCodes.NotReady,error.code);Program.Equal("partial",error.outcome);
            });
            Program.Check("screenshot_reload_after_yield_never_captures", () => {
                var identity=Ready();var work=CityWork(identity,"notApplied");int capture=0,publish=0;
                work.ThrowIfInvalid(); // Before yield, as in the production coroutine.
                identity.BeginLoad();identity.CompleteLoad();
                if(work.TryRun(()=>capture++,out var error)) work.TryRun(()=>publish++,out error);
                Program.Equal(0,capture);Program.Equal(0,publish);
            });
            Program.Check("screenshot_world_change_during_capture_never_publishes", () => {
                var identity=Ready();var work=CityWork(identity,"notApplied");int capture=0,publish=0;
                if(work.TryRun(()=>{capture++;identity.BeginLoad();return new byte[]{1};},out var png,out var error))
                    work.TryRun(()=>publish++,out error);
                Program.Equal(1,capture);Program.Equal(0,publish);Program.Equal(ErrorCodes.WorldMismatch,error.code);
            });
            Program.Check("main_menu_screenshot_stays_diagnostic", () => {
                var identity=new WorldIdentity("boot");var work=new DeferredWorldWork(identity,identity.Current,false,"notApplied");int capture=0;
                Program.Equal(true,work.TryRun(()=>capture++,out var error));Program.Equal(1,capture);
                Program.Equal(false,work.Ready);Program.Equal(0L,work.World.epoch);Program.Equal(null,error);
                identity.CompleteLoad();Program.Equal(false,work.TryRun(()=>capture++,out error));Program.Equal(1,capture);
            });
            Program.Check("readiness_recovery_cannot_revive_discarded_continuation", () => {
                var identity=Ready();var work=CityWork(identity);int restore=0;
                identity.MarkUnavailable();Program.Equal(false,work.TryRun(()=>restore++,out _));
                identity.CompleteLoad();Program.Equal(false,work.TryRun(()=>restore++,out var error));
                Program.Equal(0,restore);Program.Equal(false,error.retryable);
            });
            Program.Check("callback_error_discards_later_publication", () => {
                var identity=Ready();var work=CityWork(identity);int publish=0;
                Program.Equal(false,work.TryRun(()=>throw new Exception("failed native read"),out _));
                Program.Equal(false,work.TryRun(()=>publish++,out _));Program.Equal(0,publish);
            });
            Program.Check("stale_line_finish_never_enters_tool_restoration", () => {
                var identity=Ready();var work=CityWork(identity);int finish=0,restore=0;
                identity.BeginLoad();identity.CompleteLoad();
                Program.Equal(false,work.TryRun(()=>finish++,out _));
                Program.Equal(false,work.TryRun(()=>restore++,out _));
                Program.Equal(0,finish);Program.Equal(0,restore);
            });
            Program.Check("timer_never_pauses_a_reloaded_world", () => {
                var identity=Ready();var timer=new WorldBoundPause(identity);int pauses=0;
                timer.Arm(identity.Current,10);identity.BeginLoad();identity.CompleteLoad();
                Program.Equal(false,timer.Poll(100,()=>pauses++,out var error));Program.Equal(0,pauses);Program.Equal(0u,timer.TargetFrame);
            });
            Program.Check("timer_readiness_loss_disarms_before_target", () => {
                var identity=Ready();var timer=new WorldBoundPause(identity);int pauses=0;
                timer.Arm(identity.Current,100);identity.MarkUnavailable();timer.Poll(2,()=>pauses++,out _);
                identity.CompleteLoad();Program.Equal(false,timer.Poll(100,()=>pauses++,out _));Program.Equal(0,pauses);
            });
            Program.Check("timer_same_world_pauses_once_and_clear_disarms", () => {
                var identity=Ready();var timer=new WorldBoundPause(identity);int pauses=0;
                timer.Arm(identity.Current,10);Program.Equal(false,timer.Poll(9,()=>pauses++,out _));
                Program.Equal(true,timer.Poll(10,()=>pauses++,out _));Program.Equal(false,timer.Poll(11,()=>pauses++,out _));Program.Equal(1,pauses);
                timer.Arm(identity.Current,20);timer.Clear();timer.Poll(20,()=>pauses++,out _);Program.Equal(1,pauses);
            });
            Program.Check("timer_callback_failure_is_not_success_and_never_retries", () => {
                var identity=Ready();var timer=new WorldBoundPause(identity);int calls=0;
                timer.Arm(identity.Current,10);
                Program.Equal(false,timer.Poll(10,()=>{calls++;throw new Exception("setter failed");},out var error));
                Program.Equal(false,timer.Poll(11,()=>calls++,out _));Program.Equal(1,calls);Program.Equal(0u,timer.TargetFrame);
                Program.Equal("partial",error.outcome);
            });
        }
    }
}
