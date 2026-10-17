# Bounded passive save-task observation

Base: `8f62388de452f8cbd0aaaa95d413134dbb54897b`, reviewed lifecycle trace PR11.
This increment observes the retained `GameManager.Save(..., Texture)` return
binding `Task<bool>` through the existing journal, fixed GET and O7 item.
It does not implement O4 checkpoint verification or alter save admission.

## Frozen behavior

Move the existing SaveGame method into a focused partial if needed for exact-source
tests. Preserve guards, default name, native calls, argument evaluation/order,
preview identity/ownership and immediate legacy response. Record call attempt,
return or synchronous throw without changing exception behavior. Attempt/throw
describe the call expression, including instance and argument evaluation; they
do not prove entry into the native method when a getter throws. A null task
retains the legacy response and reports unavailable observation.

Use fixed save adapter/kind and phases saveCallAttempt, saveCallReturned,
saveCallThrew, saveTaskTrue, saveTaskFalse, saveTaskFaulted, saveTaskCanceled,
saveObserverSkipped and saveObserverUnavailable. Retain original immutable
request diagnostic identity and ingress world through reload. Do not record
names, paths, exception details, metadata, previews or native references.
Task outcome is separate from native saveSuccess, event delivery, destination
package, durable artifact and verified operation completion. False, fault and
cancellation also do not prove absence of partial effects.

## Bounded observation

Allow 32 active diagnostic reservations per journal/boot, covering registration,
queued and running continuations until finally release. Invoke the original save
regardless of capacity. Full capacity skips attachment and visibly counts lost
observation; it never queues, serializes, refuses, retries or cancels actual saves.
Never-completing tasks retain their slots; reload does not recycle them.

Use a static continuation with CancellationToken.None, default continuation
options and TaskScheduler.Default. Its state holds only immutable context,
managed journal and an exactly-once release guard. Suppress ambient execution
context during registration and restore its prior state. No Task.Run, worker,
timer, synchronous wait, Unity/ECS/UI/native calls or native delegate retention.
Read bool Result only after RanToCompletion; fault/cancel use terminal status.
Release reservations despite attachment or recorder failures.

Add bounded read metadata saveTaskObservers with capacity=32, pending 0..32,
skippedAtCapacity and attachmentFailures. Use the existing journal lock and
saturating counters, without a task registry. Capacity/null/attachment failures
increment lostObservations for missed terminal capture; failed event recording
uses its existing additional loss count. No fabricated sequence is allocated.
Terminal events use managed world/readiness and UTC, frame not sampled. UTC is
observation time, not exact task completion time; reply ordering is unconstrained.
All existing journal, page, MCP item and O7 group caps remain effective.

## Consumed proof and completion

Link the exact production SaveGame control flow against native boundary doubles.
Verify immediate and delayed true/false/fault/cancel; unchanged legacy response,
native order/arguments/preview; sync throw, null and early guard branches;
concurrency, old-world attribution, full capacity/slot release and never-ending
tasks; recorder/attachment failures and managed-only ambient-free callbacks.
Verify actual C# wire through strict TypeScript and real stdio SDK consumers.
Run focused canonical net472 compilation with SkipDeploy=true using cached
dependencies and exact-source portable runtime tests. These do not establish
installed game API compatibility or runtime save behavior.

One existing source implementer owns source/tests/API docs. Root owns this plan,
consolidated native gate and acceptance receipt. Existing independent reviewer
reviews the frozen whole increment; fix Important findings before draft PR.
No game, actual save, install, deploy, merge, native event subscription, inferred
destination or speculative preview cleanup. After this supported capture is
complete, consolidate the remaining native gate rather than add unused scaffolding.
