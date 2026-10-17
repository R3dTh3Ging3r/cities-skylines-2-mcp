import test from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { once } from 'node:events';
import { readFileSync } from 'node:fs';
import { startOfflineMcp } from './fake-bridge.mjs';
const dir = process.env.LIFECYCLE_WIRE_DIR ?? new URL('./fixtures/', import.meta.url).pathname;
const group = JSON.parse(readFileSync(`${dir}/lifecycle-group.json`, 'utf8'));
const page = JSON.parse(readFileSync(`${dir}/lifecycle-page.json`, 'utf8'));
const item = { id: 'trace', name: 'construction.lifecycleTrace', args: { limit: 16 } };
test('actual SDK discovers strict bounded diagnostic query and accepts actual C# serialized history', async () => {
    let calls = 0;
    const http = createServer(async (req, res) => { calls++; let body = ''; for await (const chunk of req)
        body += chunk; assert.deepEqual(JSON.parse(body).items, [item]); res.end(JSON.stringify(group)); });
    http.listen(0, '127.0.0.1');
    await once(http, 'listening');
    let mcp;
    try {
        mcp = await startOfflineMcp(`http://127.0.0.1:${http.address().port}`);
        const tools = (await mcp.client.listTools()).tools;
        assert.equal(tools.filter(t => t.name.includes('lifecycle')).length, 0);
        const schema = tools.find(t => t.name === 'cs2_observe_batch').inputSchema;
        const trace = schema.properties.items.items.anyOf.find(t => t.properties.name.const === item.name);
        assert.ok(trace, 'trace branch discoverable');
        assert.equal(trace.properties.args.additionalProperties, false);
        assert.equal(trace.properties.args.properties.limit.minimum, 1);
        assert.equal(trace.properties.args.properties.limit.maximum, 32);
        const result = await mcp.client.callTool({ name: 'cs2_observe_batch', arguments: { items: [item] } });
        assert.equal(result.isError, undefined);
        assert.deepEqual(JSON.parse(result.content[0].text), group);
        assert.equal(calls, 1);
    }
    finally {
        await mcp?.close();
        http.closeAllConnections();
        http.close();
        await once(http, 'close');
    }
});
test('actual SDK bounds huge trace errors and refuses invalid options before HTTP', async () => {
    let calls = 0;
    const http = createServer((_req, res) => { calls++; res.end('{}'); });
    http.listen(0, '127.0.0.1');
    await once(http, 'listening');
    let mcp;
    try {
        mcp = await startOfflineMcp(`http://127.0.0.1:${http.address().port}`);
        const huge = 'private'.repeat(50000);
        for (const args of [{ x: 1 }, { [huge]: 1 }, { afterSequence: 0 }, { bootId: 'boot', afterSequence: -1 }, { bootId: 'boot', afterSequence: 9007199254740992 }, { limit: 0 }, { limit: 33 }, { limit: 1.5 }, { bootId: '' }, { bootId: 1 }, { limit: '2' }, null]) {
            const result = await mcp.client.callTool({ name: 'cs2_observe_batch', arguments: { items: [{ ...item, args }] } });
            assert.equal(result.isError, true);
            assert.ok(Buffer.byteLength(JSON.stringify(result)) <= 262144);
            assert.equal(calls, 0);
        }
    }
    finally {
        await mcp?.close();
        http.closeAllConnections();
        http.close();
        await once(http, 'close');
    }
});
test('strict TS consumer validates finite producer page and original-world event semantics', async () => {
    const { parseLifecycleTracePage } = await import('../dist/construction-lifecycle-trace.js');
    assert.deepEqual(parseLifecycleTracePage(page, { limit: 32 }), page);
    for (const mutate of [p => p.events[0].phase = 'complete', p => p.events[0].traceId = 'admitted-operation', p => p.events[0].frameReason = 'nativeDelivered', p => p.events.reverse(), p => p.highWater = 0, p => p.events[0].cleanup = 'verified', p => p.events[0].references.push(...Array(5).fill(p.events[0].references[0]))]) {
        const bad = structuredClone(page);
        mutate(bad);
        assert.throws(() => parseLifecycleTracePage(bad, { limit: 32 }));
    }
});
test('fixture fixed read consumes source-produced artifact without promoting native probes', () => {
    const fixture = JSON.parse(readFileSync(new URL('../../tests/native/lifecycle.json', import.meta.url), 'utf8'));
    assert.deepEqual(fixture.operations[2], { method: 'GET', path: '/diagnostics/construction-lifecycle' });
    assert.equal(page.provenance, 'managedPassiveHistory');
    assert.equal(page.completion, 'notEstablished');
    assert.ok(page.events.every(e => e.effects === 'unknown' && e.cleanup === 'unverified' && e.authority === 'unverified'));
    const metadata = JSON.parse(readFileSync(`${dir}/lifecycle-metadata.json`, 'utf8'));
    assert.equal(metadata.endpoint.path, fixture.operations[2].path);
    assert.equal(metadata.endpoint.access, 'read');
    assert.equal(metadata.capability.support, 'degraded');
    assert.equal(metadata.nativeValidation.dispatchAvailable, false);
    assert.ok(metadata.nativeValidation.probes.every(p => p.dispatchAvailable === false && p.support === 'unverified'));
});
test('actual source serialized old-world history remains valid without retagging event worlds or frames', async () => {
    const { parseLifecycleTracePage } = await import('../dist/construction-lifecycle-trace.js');
    const old = JSON.parse(readFileSync(`${dir}/lifecycle-old-world.json`, 'utf8'));
    assert.deepEqual(parseLifecycleTracePage(old, { limit: 32 }), old);
    assert.equal(old.readReady, false);
    const last = old.events.at(-1);
    assert.notDeepEqual(last.ingressWorld, last.observedWorld);
    assert.equal(last.frame, null);
    assert.equal(last.ownsActiveTool, null);
    assert.ok(old.events.every(e => e.references.every(r => JSON.stringify(r.world) === JSON.stringify(e.ingressWorld))));
});
test('actual SDK trace-only and mixed outer failures remain finite without arbitrary private bodies', async () => {
    let calls = 0;
    const secret = 'private-native-diagnostic-'.repeat(5000);
    let failure = { error: secret, errorData: { code: 'RESULT_TOO_LARGE', message: secret, nativeErrors: [secret], operationId: secret, retryable: false, outcome: 'unknown' } };
    const http = createServer((_req, res) => { calls++; res.statusCode = 413; res.end(JSON.stringify(failure)); });
    http.listen(0, '127.0.0.1');
    await once(http, 'listening');
    let mcp;
    try {
        mcp = await startOfflineMcp(`http://127.0.0.1:${http.address().port}`);
        for (const items of [[item], [{ id: 'context', name: 'state', args: {} }, item]]) {
            const result = await mcp.client.callTool({ name: 'cs2_observe_batch', arguments: { items } });
            assert.equal(result.isError, true);
            assert.ok(Buffer.byteLength(JSON.stringify(result)) <= 262144);
            assert.ok(!JSON.stringify(result).includes('private-native'));
            assert.equal(JSON.parse(result.content[0].text).error.reason, 'unknownCause');
        }
        assert.equal(calls, 2);
        failure = { errorData: { code: 'WORLD_MISMATCH', message: 'trace cursor boot mismatch', retryable: false, outcome: 'notApplied' } };
        const known = await mcp.client.callTool({ name: 'cs2_observe_batch', arguments: { items: [item] } });
        assert.equal(JSON.parse(known.content[0].text).error.reason, 'bootMismatch');
        assert.equal(calls, 3);
    }
    finally {
        await mcp?.close();
        http.closeAllConnections();
        http.close();
        await once(http, 'close');
    }
});
test('actual SDK item failures are sanitized while ordinary successful mixed observations retain order', async () => {
    const secret = 'private-native-diagnostic';
    const body = structuredClone(group);
    body.results = [{ id: 'context', observation: { ...group.results[0].observation, data: { cityLoaded: true } } }, { id: 'trace', error: { code: 'WORLD_MISMATCH', message: secret, retryable: false, outcome: 'unknown', operationId: secret, nativeErrors: [secret] } }];
    const http = createServer((_req, res) => res.end(JSON.stringify(body)));
    http.listen(0, '127.0.0.1');
    await once(http, 'listening');
    let mcp;
    try {
        mcp = await startOfflineMcp(`http://127.0.0.1:${http.address().port}`);
        const result = await mcp.client.callTool({ name: 'cs2_observe_batch', arguments: { items: [{ id: 'context', name: 'state', args: {} }, item] } });
        assert.equal(result.isError, undefined);
        const observed = JSON.parse(result.content[0].text);
        assert.deepEqual(observed.results[0], body.results[0]);
        assert.equal(observed.results[1].error.reason, 'unknownCause');
        assert.ok(!JSON.stringify(observed).includes(secret));
    }
    finally {
        await mcp?.close();
        http.closeAllConnections();
        http.close();
        await once(http, 'close');
    }
});


test('actual reused transit producer histories reach SDK without stale result attribution', async () => {
    const { parseLifecycleTracePage } = await import('../dist/construction-lifecycle-trace.js');
    const cases = ['line-stop-same-world', 'line-stop-new-world', 'stop-line-same-world', 'stop-line-new-world'];
    const fixtures = cases.map(name => JSON.parse(readFileSync(`${dir}/lifecycle-reuse-${name}.json`, 'utf8')));
    let calls = 0;
    const http = createServer(async (req, res) => {
        let body = '';
        for await (const chunk of req) body += chunk;
        const current = fixtures[calls++];
        assert.deepEqual(JSON.parse(body), {
            expectedWorld: current.world,
            items: [{ id: 'reuse', name: 'construction.lifecycleTrace', args: { bootId: current.world.bootId, afterSequence: 0, limit: 32 } }],
        });
        res.end(JSON.stringify(current));
    });
    http.listen(0, '127.0.0.1');
    await once(http, 'listening');
    let mcp;
    try {
        mcp = await startOfflineMcp(`http://127.0.0.1:${http.address().port}`);
        for (const [index, wire] of fixtures.entries()) {
            const query = { bootId: wire.world.bootId, afterSequence: 0, limit: 32 };
            const result = await mcp.client.callTool({ name: 'cs2_observe_batch', arguments: {
                expectedWorld: wire.world,
                items: [{ id: 'reuse', name: 'construction.lifecycleTrace', args: query }],
            } });
            assert.equal(result.isError, undefined);
            assert.deepEqual(JSON.parse(result.content[0].text), wire);
            const history = parseLifecycleTracePage(wire.results[0].observation.data, query);
            const [previous, queued] = history.events.filter(e => e.phase === 'queued');
            const current = history.events.filter(e => e.traceId === queued.traceId);
            assert.notEqual(queued.traceId, previous.traceId);
            assert.equal(queued.kind, cases[index].startsWith('line-stop') ? 'stop' : 'line');
            assert.equal(queued.references.length, queued.kind === 'stop' ? 1 : 2);
            assert.ok(current.every(e => e.references.every(r => r.index !== 7)));
            assert.ok(current.filter(e => ['queued', 'definitions', 'validation', 'validationAccepted'].includes(e.phase))
                .every(e => e.references.every(r => r.index !== 19)));
            for (const phase of ['applyObserved', 'finishObserved']) {
                const reference = current.find(e => e.phase === phase).references.find(r => r.index === 19);
                assert.equal(reference.version, 3);
                assert.equal(reference.role, queued.kind);
                assert.deepEqual(reference.world, queued.ingressWorld);
            }
            assert.equal(queued.ingressWorld.epoch !== previous.ingressWorld.epoch, cases[index].endsWith('new-world'));
            if (queued.kind === 'line') {
                for (const phase of ['renameQueued', 'renameDequeued', 'setterAttempt', 'responseBuilderAttempt']) {
                    const reference = current.find(e => e.phase === phase).references.find(r => r.index === 19);
                    assert.equal(reference.role, 'line');
                    assert.deepEqual(reference.world, queued.ingressWorld);
                }
            }
            assert.ok(Buffer.byteLength(JSON.stringify(result)) <= 262144);
        }
        assert.equal(calls, 4);
    } finally {
        await mcp?.close();
        http.closeAllConnections();
        http.close();
        await once(http, 'close');
    }
});

// All fixtures below originate from the real SaveGame partial with native leaves doubled.
test('actual SDK consumes passive true false fault cancel capacity and old-world save task evidence', async () => {
    const names = ['true', 'false', 'faulted', 'canceled', 'capacity', 'old-world'];
    const fixtures = names.map(name => JSON.parse(readFileSync(`${dir}/lifecycle-save-${name}.json`, 'utf8')));
    let calls = 0;
    const http = createServer(async (req, res) => {
        let body = '';
        for await (const chunk of req) body += chunk;
        assert.deepEqual(JSON.parse(body).items, [{ id: 'save', name: item.name, args: { limit: 32 } }]);
        res.end(JSON.stringify(fixtures[calls++]));
    });
    http.listen(0, '127.0.0.1');
    await once(http, 'listening');
    let mcp;
    try {
        mcp = await startOfflineMcp(`http://127.0.0.1:${http.address().port}`);
        for (const [index, wire] of fixtures.entries()) {
            const result = await mcp.client.callTool({ name: 'cs2_observe_batch', arguments: {
                items: [{ id: 'save', name: item.name, args: { limit: 32 } }],
            } });
            assert.equal(result.isError, undefined);
            assert.deepEqual(JSON.parse(result.content[0].text), wire);
            const history = wire.results[0].observation.data;
            const saves = history.events.filter(e => e.adapter === 'save');
            assert.ok(saves.length > 0);
            assert.ok(saves.every(e => e.kind === 'save' && e.route === 'gameSave' && e.frame === null &&
                e.frameReason === 'notSampled' && e.status === null && e.ownsActiveTool === null &&
                e.completionAccepted === null && e.references.length === 0 && e.omittedReferences === 0 &&
                e.effects === 'unknown' && e.cleanup === 'unverified' && e.authority === 'unverified'));
            if (index < 4) {
                const suffix = names[index][0].toUpperCase() + names[index].slice(1);
                assert.equal(saves.filter(e => e.phase === `saveTask${suffix}`).length, 1);
                assert.deepEqual(history.saveTaskObservers, { capacity: 32, pending: 0, skippedAtCapacity: 0, attachmentFailures: 0 });
            } else if (names[index] === 'capacity') {
                assert.deepEqual(history.saveTaskObservers, { capacity: 32, pending: 32, skippedAtCapacity: 1, attachmentFailures: 0 });
                assert.equal(history.lostObservations, 1);
                assert.equal(saves.at(-1).phase, 'saveObserverSkipped');
            } else {
                const last = saves.at(-1);
                assert.equal(last.phase, 'saveTaskTrue');
                assert.notDeepEqual(last.ingressWorld, last.observedWorld);
                assert.equal(last.observedReady, false);
                assert.notDeepEqual(last.ingressWorld, wire.world);
            }
            assert.ok(!JSON.stringify(result).includes('private-'));
            assert.ok(Buffer.byteLength(JSON.stringify(result)) <= 262144);
        }
        assert.equal(calls, names.length, 'one HTTP call per explicit read; no polling or retries');
    } finally {
        await mcp?.close();
        http.closeAllConnections();
        http.close();
        await once(http, 'close');
    }
});

test('strict save wire rejects unsupported task success claims unbounded counters and native evidence', async () => {
    const { parseLifecycleTracePage } = await import('../dist/construction-lifecycle-trace.js');
    const source = JSON.parse(readFileSync(`${dir}/lifecycle-save-true.json`, 'utf8')).results[0].observation.data;
    assert.deepEqual(parseLifecycleTracePage(source, { limit: 32 }), source);
    const terminal = p => p.events.find(e => e.phase === 'saveTaskTrue');
    for (const mutate of [
        p => delete p.saveTaskObservers,
        p => p.saveTaskObservers.capacity = 33,
        p => p.saveTaskObservers.pending = 33,
        p => p.saveTaskObservers.pending = -1,
        p => p.saveTaskObservers.attachmentFailures = Number.MAX_SAFE_INTEGER + 1,
        p => p.saveTaskObservers.skippedAtCapacity = 1.5,
        p => p.saveTaskObservers.nativeSuccess = true,
        p => terminal(p).phase = 'saveSuccess',
        p => terminal(p).adapter = 'envelope',
        p => terminal(p).kind = 'direct',
        p => terminal(p).route = 'cameraSet',
        p => { terminal(p).frame = 10; terminal(p).frameReason = null; },
        p => terminal(p).ownsActiveTool = true,
        p => terminal(p).status = 200,
        p => terminal(p).omittedReferences = 1,
        p => terminal(p).references.push({ role: 'target', world: terminal(p).ingressWorld, index: 1, version: 1 }),
        p => terminal(p).durable = true,
    ]) {
        const bad = structuredClone(source);
        mutate(bad);
        assert.throws(() => parseLifecycleTracePage(bad, { limit: 32 }));
    }
});
