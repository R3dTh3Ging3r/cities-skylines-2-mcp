import test from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { once } from 'node:events';
import { createBridgeClient } from '../dist/bridge-client.js';
import { startOfflineMcp } from './fake-bridge.mjs';
const world = { bootId: 'boot', epoch: 1 };
const request = { items: [{ id: 'a', name: 'state', args: {} }] };
const observation = { data: { cityLoaded: true }, world, startFrame: 10, endFrame: 10, sampledAtUtc: '2026-10-16T00:00:00Z', freshness: 'current', warnings: [] };
const result = { world, startFrame: 10, endFrame: 10, worldChanged: false, results: [{ id: 'a', observation }] };
test('actual MCP observation tool uses strict catalog request and preserves POST body', async () => {
    const calls = [];
    const server = createServer(async (req, res) => { let body = ''; for await (const chunk of req)
        body += chunk; calls.push({ url: req.url, method: req.method, body: JSON.parse(body) }); res.end(JSON.stringify(result)); });
    server.listen(0, '127.0.0.1');
    await once(server, 'listening');
    let mcp;
    try {
        mcp = await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
        const { tools } = await mcp.client.listTools();
        assert.ok(tools.some(t => t.name === 'cs2_observe_batch'), 'observation registration absent');
        const response = await mcp.client.callTool({ name: 'cs2_observe_batch', arguments: request });
        assert.equal(response.isError, undefined);
        assert.deepEqual(JSON.parse(response.content[0].text), result);
        assert.deepEqual(calls, [{ url: '/v2/observations', method: 'POST', body: request }]);
        for (const invalid of [{ ...request, lease: 'x' }, { ...request, dependency: 'a' }, { items: [{ id: 'a', name: 'city.budget', args: { limit: 1 } }] }, { items: [{ id: 'a', name: 'cs2_save_game', args: {} }] }, { items: [{ id: 'a', name: '/state', args: {} }] }, { items: [request.items[0], request.items[0]] }, { items: [] }, { items: Array.from({ length: 65 }, (_, i) => ({ ...request.items[0], id: String(i) })) }]) {
            const reply = await mcp.client.callTool({ name: 'cs2_observe_batch', arguments: invalid });
            assert.equal(reply.isError, true);
            assert.equal(calls.length, 1);
        }
    }
    finally {
        await mcp?.close();
        server.closeAllConnections();
        server.close();
        await once(server, 'close');
    }
});
test('observe validates response union cardinality IDs worlds and frame evidence before exposing it', async () => {
    let payload = result;
    const bridge = createBridgeClient({ baseUrl: 'http://127.0.0.1:1234', fetchImpl: async () => new Response(JSON.stringify(payload)) });
    assert.equal(typeof bridge.observe, 'function');
    assert.deepEqual(await bridge.observe(request), result);
    const bad = [{ ...result, results: [] }, { ...result, results: [{ id: 'wrong', observation }] }, { ...result, results: [{ id: 'a', observation, error: { code: 'X', message: 'bad', outcome: 'notApplied', retryable: false } }] },
        { ...result, results: [{ id: 'a' }] }, { ...result, endFrame: 9 }, { ...result, worldChanged: true }, { ...result, results: [{ id: 'a', observation: { ...observation, world: { ...world, epoch: 2 } } }] }, { ...result, results: [{ id: 'a', observation: { ...observation, startFrame: 12 } }] },
        { ...result, results: [{ id: 'a', observation: { ...observation, freshness: 'fresh' } }] }, { ...result, results: [{ id: 'a', observation: { ...observation, warnings: null } }] }];
    for (payload of bad)
        await assert.rejects(bridge.observe(request), e => e.details.code === 'PROTOCOL_ERROR');
});
test('observe freezes request before await and independently rejects unknown/mixed inventory args', async () => {
    let sent;
    const bridge = createBridgeClient({ baseUrl: 'http://127.0.0.1', fetchImpl: async (_url, opts) => { sent = JSON.parse(opts.body); return new Response(JSON.stringify(result)); } });
    assert.equal(typeof bridge.observe, 'function');
    const copy = structuredClone(request);
    const call = bridge.observe(copy);
    copy.items[0].id = 'changed';
    await call;
    assert.equal(sent.items[0].id, 'a');
    await assert.rejects(bridge.observe({ ...request, family: 'roads' }));
    await assert.rejects(bridge.observe({ items: [{ id: 'a', name: 'construction.inventory', args: { world, entities: [{ world: { ...world, epoch: 2 }, index: 1, version: 1 }] } }] }));
});
test('observe composes caller abort and timeout and removes its listener', async () => {
    const abort = new AbortController();
    let added = 0, removed = 0, calls = 0;
    const add = abort.signal.addEventListener.bind(abort.signal), remove = abort.signal.removeEventListener.bind(abort.signal);
    abort.signal.addEventListener = (...args) => { added++; return add(...args); };
    abort.signal.removeEventListener = (...args) => { removed++; return remove(...args); };
    const bridge = createBridgeClient({ baseUrl: 'http://127.0.0.1', timeoutMs: 20, fetchImpl: async (_url, opts) => { calls++; return await new Promise((resolve, reject) => opts.signal.addEventListener('abort', () => reject(new DOMException('aborted', 'AbortError')), { once: true })); } });
    assert.equal(typeof bridge.observe, 'function');
    const pending = bridge.observe(request, { signal: abort.signal });
    abort.abort();
    await assert.rejects(pending, e => e.details.code === 'READ_CANCELLED');
    assert.equal(added, removed);
    await assert.rejects(bridge.observe(request), e => e.details.code === 'TRANSPORT_UNAVAILABLE');
    assert.equal(calls, 2);
    await assert.rejects(bridge.observe(request, { signal: abort.signal }));
    assert.equal(calls, 2);
});
test('observe bounds actual streamed UTF8 response bytes without trusting Content-Length', async () => {
    let cancelled = false;
    const bridge = createBridgeClient({ baseUrl: 'http://127.0.0.1', fetchImpl: async () => new Response(new ReadableStream({ start(c) { c.enqueue(new Uint8Array(4194305)); }, cancel() { cancelled = true; } }), { headers: { 'content-length': '1' } }) });
    assert.equal(typeof bridge.observe, 'function');
    await assert.rejects(bridge.observe(request), e => e.details.code === 'PROTOCOL_ERROR');
    assert.equal(cancelled, true);
});
test('actual MCP cancellation aborts the outstanding observation HTTP request', async () => {
    let received;
    const incoming = new Promise(resolve => received = resolve);
    let closed = false;
    const server = createServer(async (req, res) => { for await (const chunk of req) { } res.on('close', () => closed = true); received(); });
    server.listen(0, '127.0.0.1');
    await once(server, 'listening');
    let mcp;
    try {
        mcp = await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
        const abort = new AbortController();
        const pending = mcp.client.callTool({ name: 'cs2_observe_batch', arguments: request }, undefined, { signal: abort.signal });
        await incoming;
        abort.abort();
        await assert.rejects(pending);
        const deadline = Date.now() + 1500;
        while (!closed && Date.now() < deadline)
            await new Promise(resolve => setTimeout(resolve, 10));
        assert.equal(closed, true, 'MCP cancellation did not detach HTTP observation');
    }
    finally {
        await mcp?.close();
        server.closeAllConnections();
        server.close();
        await once(server, 'close');
    }
});
