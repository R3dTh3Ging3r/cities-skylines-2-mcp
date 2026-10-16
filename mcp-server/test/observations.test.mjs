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

test('actual O7 listTools exposes the strict bounded fixed catalog and full nested inputs', async () => {
    const mcp = await startOfflineMcp('http://127.0.0.1:1');
    try {
        const schema = (await mcp.client.listTools()).tools.find(t => t.name === 'cs2_observe_batch').inputSchema;
        const exact = (node, required, fields = required) => {
            assert.deepEqual(new Set(node.required), new Set(required));
            assert.deepEqual(Object.keys(node.properties).sort(), [...fields].sort());
            assert.equal(node.additionalProperties, false);
        };
        const resolve = node => {
            while (node.$ref) node = node.$ref.slice(2).split('/').reduce((value, key) => value[key], schema);
            return node;
        };
        const checkWorld = node => {
            node = resolve(node); exact(node, ['bootId', 'epoch']);
            assert.deepEqual(node.properties.bootId, {type: 'string', minLength: 1, maxLength: 128});
            assert.deepEqual(node.properties.epoch, {type: 'integer', minimum: 0, maximum: Number.MAX_SAFE_INTEGER});
        };
        const checkRef = node => {
            node = resolve(node); exact(node, ['world', 'index', 'version']); checkWorld(node.properties.world);
            for (const field of ['index', 'version'])
                assert.deepEqual(node.properties[field], {type: 'integer', minimum: 1, maximum: 2147483647});
        };
        exact(schema, ['items'], ['expectedWorld', 'items']); checkWorld(schema.properties.expectedWorld);
        const items = schema.properties.items;
        assert.equal(items.minItems, 1); assert.equal(items.maxItems, 64);
        const branches = items.items.anyOf.map(resolve);
        const byName = new Map();
        for (const branch of branches) {
            exact(branch, ['id', 'name', 'args']);
            assert.deepEqual(branch.properties.id, {type: 'string', minLength: 1, maxLength: 128});
            const name = branch.properties.name;
            for (const key of name.enum ?? [name.const]) byName.set(key, resolve(branch.properties.args));
        }
        assert.deepEqual([...byName.keys()].sort(), ['state', 'city.overview', 'city.services', 'city.labor',
            'city.budget', 'construction.inventory', 'transit.selectedLine', 'metrics.read'].sort());
        for (const name of ['state', 'city.overview', 'city.services', 'city.labor', 'city.budget'])
            exact(byName.get(name), []);
        const inventory = byName.get('construction.inventory');
        exact(inventory, ['world', 'entities'], ['world', 'entities', 'relationships']); checkWorld(inventory.properties.world);
        assert.deepEqual(inventory.properties.relationships, {type: 'string', const: 'directDepth1'});
        assert.equal(inventory.properties.entities.minItems, 1); assert.equal(inventory.properties.entities.maxItems, 32);
        checkRef(inventory.properties.entities.items);
        const transit = byName.get('transit.selectedLine');
        exact(transit, ['line'], ['line', 'mode', 'purpose']); checkRef(transit.properties.line);
        assert.deepEqual(transit.properties.mode.enum, ['bus', 'tram', 'metro', 'train', 'airplane']);
        assert.deepEqual(transit.properties.purpose.enum, ['passenger', 'cargo']);
        const metrics = byName.get('metrics.read'); exact(metrics, ['world', 'scope', 'queries']); checkWorld(metrics.properties.world);
        const scopes = metrics.properties.scope.anyOf.map(resolve);
        exact(scopes[0], ['kind']); assert.equal(scopes[0].properties.kind.const, 'city');
        exact(scopes[1], ['kind', 'line']); assert.equal(scopes[1].properties.kind.const, 'selectedLine');
        checkRef(scopes[1].properties.line);
        const queries = metrics.properties.queries;
        assert.equal(queries.minItems, 1); assert.equal(queries.maxItems, 64);
        exact(queries.items, ['key', 'parameters']); exact(queries.items.properties.parameters, []);
        assert.deepEqual(new Set(queries.items.properties.key.enum), new Set(['city.population', 'city.populationWithMoveIn', 'city.money', 'city.xp',
            'labor.employed', 'labor.homelessCitizens', 'labor.jobs.total', 'labor.jobs.free',
            'labor.jobs.totalByEducation.uneducated', 'labor.jobs.totalByEducation.poorlyEducated', 'labor.jobs.totalByEducation.educated', 'labor.jobs.totalByEducation.wellEducated', 'labor.jobs.totalByEducation.highlyEducated',
            'labor.jobs.freeByEducation.uneducated', 'labor.jobs.freeByEducation.poorlyEducated', 'labor.jobs.freeByEducation.educated', 'labor.jobs.freeByEducation.wellEducated', 'labor.jobs.freeByEducation.highlyEducated',
            'budget.income', 'budget.expenses', 'budget.taxIncome', 'budget.balance',
            'transit.waypointCount', 'transit.segmentCount', 'transit.brokenWaypointCount', 'transit.invalidSegmentCount']));
    } finally { await mcp.close(); }
});

test('actual O7 SDK errors stay bounded for huge unknown keys at every input depth with zero HTTP', async () => {
    let calls = 0;
    const server = createServer((_req, res) => { calls++; res.end('{}'); });
    server.listen(0, '127.0.0.1'); await once(server, 'listening');
    let mcp;
    try {
        mcp = await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
        const huge = '界'.repeat(100000), ref = {world, index: 1, version: 1};
        const args = {world, entities: [ref], relationships: 'directDepth1'};
        const item = {id: 'i', name: 'construction.inventory', args};
        const input = {expectedWorld: world, items: [item]};
        const cases = [
            ['valid top', {...input, [huge]: 1}], ['missing top', {[huge]: 1}],
            ['valid item', {...input, items: [{...item, [huge]: 1}]}], ['missing item', {items: [{[huge]: 1}]}],
            ['valid args', {...input, items: [{...item, args: {...args, [huge]: 1}}]}],
            ['missing args', {...input, items: [{...item, args: {[huge]: 1}}]}],
            ['valid ref', {...input, items: [{...item, args: {...args, entities: [{...ref, [huge]: 1}]}}]}],
            ['missing ref', {...input, items: [{...item, args: {...args, entities: [{[huge]: 1}]}}]}],
            ['valid world', {...input, expectedWorld: {...world, [huge]: 1}}],
            ['missing world', {...input, expectedWorld: {[huge]: 1}}],
            ['missing required', {}], ['invalid items', {items: null}],
        ];
        const failures = [];
        for (const [name, invalid] of cases) {
            const response = await mcp.client.callTool({name: 'cs2_observe_batch', arguments: invalid});
            assert.equal(response.isError, true, name);
            const bytes = Buffer.byteLength(JSON.stringify(response));
            if (bytes > 262144) failures.push(`${name}: ${bytes} bytes`);
            assert.equal(calls, 0, name);
        }
        assert.deepEqual(failures, [], 'SDK diagnostics exceeded 256KiB');
    } finally {
        await mcp?.close(); server.closeAllConnections(); server.close(); await once(server, 'close');
    }
});

test('actual O7 facade preserves every typed branch and rejects cross-field errors before POST', async () => {
    const calls = [];
    const server = createServer(async (req, res) => {
        let body = ''; for await (const chunk of req) body += chunk;
        const input = JSON.parse(body); calls.push(input);
        res.end(JSON.stringify({...result, results: input.items.map(({id}) => ({id,
            error: {code: 'SOURCE_UNAVAILABLE', message: 'controlled test source', retryable: false, outcome: 'notApplied'}}))}));
    });
    server.listen(0, '127.0.0.1'); await once(server, 'listening'); let mcp;
    try {
        mcp = await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
        const ref = {world, index: 1, version: 1};
        const inventory = {world, entities: [ref], relationships: 'directDepth1'};
        const metrics = {world, scope: {kind: 'city'}, queries: [{key: 'city.population', parameters: {}}]};
        const transitMetrics = {world, scope: {kind: 'selectedLine', line: ref}, queries: [{key: 'transit.waypointCount', parameters: {}}]};
        const input = {expectedWorld: world, items: [
            ...['state', 'city.overview', 'city.services', 'city.labor', 'city.budget'].map(name => ({id: name, name, args: {}})),
            {id: 'inventory', name: 'construction.inventory', args: inventory},
            {id: 'transit', name: 'transit.selectedLine', args: {line: ref, mode: 'train', purpose: 'cargo'}},
            {id: 'cityMetrics', name: 'metrics.read', args: metrics},
            {id: 'transitMetrics', name: 'metrics.read', args: transitMetrics},
        ]};
        assert.equal((await mcp.client.callTool({name: 'cs2_observe_batch', arguments: input})).isError, undefined);
        assert.deepEqual(calls, [input]);
        const item = (name, args) => ({items: [{id: 'i', name, args}]});
        const invalid = [
            {items: [input.items[0], input.items[0]]},
            item('construction.inventory', {...inventory, entities: [ref, {...ref, version: 2}]}),
            item('construction.inventory', {...inventory, entities: [{...ref, world: {...world, epoch: 2}}]}),
            item('construction.inventory', {...inventory, entities: [{...ref, version: 2147483648}]}),
            item('construction.inventory', {...inventory, entities: Array.from({length: 33}, (_, i) => ({...ref, index: i + 1}))}),
            item('construction.inventory', {...inventory, relationships: 'directDepth2'}),
            item('construction.inventory', {...inventory, family: 'roads'}),
            item('construction.inventory', {...inventory, bbox: [0, 1]}),
            item('transit.selectedLine', {line: ref, mode: 'ship'}),
            item('transit.selectedLine', {line: ref, purpose: 'mixed'}),
            item('transit.selectedLine', {line: ref, recursive: true}),
            item('metrics.read', {...metrics, queries: [...metrics.queries, ...metrics.queries]}),
            item('metrics.read', {...metrics, queries: transitMetrics.queries}),
            item('metrics.read', {...transitMetrics, scope: {kind: 'selectedLine', line: {...ref, world: {...world, epoch: 2}}}}),
            item('metrics.read', {...metrics, queries: [{key: 'city.population', parameters: {unknown: 1}}]}),
            item('metrics.read', {...metrics, scope: {kind: 'city', line: null}}),
            item('metrics.unknown', {}), item('state', {limit: 1}), item('state', undefined),
            {items: [{name: 'state', args: {}}]}, {items: [{id: '', name: 'state', args: {}}]},
            {items: [{id: 'x'.repeat(129), name: 'state', args: {}}]},
            {...request, expectedWorld: {...world, epoch: Number.MAX_SAFE_INTEGER + 1}},
            {...request, expectedWorld: {...world, bootId: ' '}},
        ];
        for (const args of invalid) {
            const reply = await mcp.client.callTool({name: 'cs2_observe_batch', arguments: args});
            assert.equal(reply.isError, true); assert.ok(Buffer.byteLength(JSON.stringify(reply)) <= 262144);
            assert.equal(calls.length, 1);
        }
    } finally {
        await mcp?.close(); server.closeAllConnections(); server.close(); await once(server, 'close');
    }
});

test('actual O7 retains 64-item success groups larger than the SDK error cap', async () => {
    const input = {items: Array.from({length: 64}, (_, i) => ({id: String(i), name: 'state', args: {}}))};
    const group = {...result, results: input.items.map(({id}) => ({id, observation: {
        ...observation, data: {boundedTestPayload: 'x'.repeat(5000)}}}))};
    assert.ok(Buffer.byteLength(JSON.stringify(group)) > 262144);
    const server = createServer((_req, res) => res.end(JSON.stringify(group)));
    server.listen(0, '127.0.0.1'); await once(server, 'listening'); let mcp;
    try {
        mcp = await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
        const response = await mcp.client.callTool({name: 'cs2_observe_batch', arguments: input});
        assert.equal(response.isError, undefined); assert.deepEqual(JSON.parse(response.content[0].text), group);
    } finally {
        await mcp?.close(); server.closeAllConnections(); server.close(); await once(server, 'close');
    }
});
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
