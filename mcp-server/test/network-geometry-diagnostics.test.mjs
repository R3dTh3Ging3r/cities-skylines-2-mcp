import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {createServer} from 'node:http';
import {once} from 'node:events';
import {startOfflineMcp} from './fake-bridge.mjs';
import {createBridgeClient} from '../dist/bridge-client.js';
import {parseConstructionObservation} from '../dist/construction-contracts.js';
import {parseObservationResult} from '../dist/observation-contracts.js';
const old = JSON.parse(readFileSync(new URL('./fixtures/construction-relationships-wire.json', import.meta.url), 'utf8'));
const input = {...old.request, geometryDiagnostics: 'sampledCubicV1'};
const wire = JSON.parse(readFileSync(process.env.GEOMETRY_WIRE_DIR ? `${process.env.GEOMETRY_WIRE_DIR}/geometry.json`
    : new URL('./fixtures/network-geometry-wire.json', import.meta.url), 'utf8'));

test('actual inventory SDK discovers the optional computed profile literal without changing required refs', async () => {
    const mcp = await startOfflineMcp('http://127.0.0.1:1');
    try {
        const tools = (await mcp.client.listTools()).tools;
        const schema = tools.find(tool => tool.name === 'cs2_construction_inventory').inputSchema;
        assert.deepEqual(new Set(schema.required), new Set(['world', 'entities']));
        assert.equal(schema.properties.geometryDiagnostics?.const, 'sampledCubicV1');
        const batch = tools.find(tool => tool.name === 'cs2_observe_batch').inputSchema;
        const inventory = batch.properties.items.items.anyOf.find(branch => branch.properties.name.const === 'construction.inventory');
        assert.equal(inventory.properties.args.properties.geometryDiagnostics?.const, 'sampledCubicV1');
    } finally { await mcp.close(); }
});

test('actual C# cubic profiles preserve source geometry, finite evidence, sampled summaries and unavailable roots', () => {
    assert.deepEqual(parseConstructionObservation(wire.direct, wire.request), wire.direct);
    assert.deepEqual(parseObservationResult(wire.group, {expectedWorld: wire.request.world,
        items: [{id: 'g', name: 'construction.inventory', args: wire.request}]}), wire.group);
    const diagnostics = wire.direct.data.geometryDiagnostics;
    assert.equal(diagnostics.evaluations, 51); assert.equal(diagnostics.maxEvaluations, 544);
    assert.equal(diagnostics.physicalUnits, 'unverified'); assert.equal(diagnostics.engineeringValidation, 'unknown');
    assert.equal(wire.direct.data.entities[0].network.length, 99, 'native length replaced');
    const [graded, stationary, building, unsupported, missing, bowed] = diagnostics.profiles;
    assert.equal(graded.samples.length, 17); assert.equal(graded.samples[8].grade.value, 2);
    assert.equal(graded.grade.max.t, 0); assert.equal(graded.radius.unavailableSamples, 17);
    assert.equal(stationary.samples[8].curvature.reason, 'stationaryDerivative');
    assert.ok(bowed.samples[8].curvature.value > 0); assert.ok(bowed.samples[8].radius.value > 0);
    for (const row of [building, unsupported, missing]) { assert.equal(row.status, 'unavailable'); assert.equal(row.samples.length, 0); }
    const baseline = structuredClone(old.baseline); delete baseline.data.geometryDiagnostics;
    assert.deepEqual(parseConstructionObservation(baseline, {world: old.request.world, entities: old.request.entities}), baseline);
});

test('actual C# finite wire keeps underflow overflow and positional uncertainty without false flatness', () => {
    const value = parseConstructionObservation(wire.arithmeticDirect, wire.arithmeticRequest);
    const [lost, overflow, underflow] = value.data.geometryDiagnostics.profiles;
    assert.equal(lost.samples[8].position, null); assert.equal(lost.samples[8].positionReason, 'positionArithmeticUnavailable');
    assert.equal(lost.samples[8].grade.value, null); assert.equal(lost.samples[8].curvature.value, null);
    assert.equal(overflow.samples[0].curvature.reason, 'arithmeticOverflow'); assert.ok(overflow.samples[0].radius.value > 0);
    assert.equal(underflow.samples[0].curvature.value, null); assert.equal(underflow.samples[0].curvature.reason, 'arithmeticUnderflow');
    assert.equal(underflow.samples[0].radius.reason, 'arithmeticOverflow');
});

test('direct and O7 parsers reject missing malformed count identity and extremum evidence', () => {
    const changes = [
        value => delete value.data.geometryDiagnostics,
        value => value.data.geometryDiagnostics.profiles.reverse(),
        value => value.data.geometryDiagnostics.evaluations++,
        value => value.data.geometryDiagnostics.maxEvaluations++,
        value => value.data.geometryDiagnostics.profiles[0].samples.pop(),
        value => value.data.geometryDiagnostics.profiles[0].samples[2].t = .3,
        value => value.data.geometryDiagnostics.profiles[0].samples[0].position.y++,
        value => value.data.geometryDiagnostics.profiles[0].samples[8].position.x = Infinity,
        value => value.data.geometryDiagnostics.profiles[0].samples[8].grade.reason = 'notKnown',
        value => value.data.geometryDiagnostics.profiles[0].samples[8].grade.value = null,
        value => value.data.geometryDiagnostics.profiles[0].samples[8].radius.value = 0,
        value => value.data.geometryDiagnostics.profiles[0].samples[8].curvature.value = -1,
        value => value.data.geometryDiagnostics.profiles[0].grade.supportedSamples--,
        value => value.data.geometryDiagnostics.profiles[0].grade.min.t = .5,
        value => value.data.geometryDiagnostics.profiles[0].grade.max.position.x++,
        value => value.data.geometryDiagnostics.profiles[1].curvature.min = {value: 0, t: 0, position: {x: 0, y: 0, z: 0}, positionReason: null},
        value => value.data.geometryDiagnostics.profiles[2].reason = null,
        value => value.data.geometryDiagnostics.profiles[2].samples = [structuredClone(value.data.geometryDiagnostics.profiles[0].samples[0])],
        value => value.data.geometryDiagnostics.physicalUnits = 'metres',
        value => value.warnings = value.warnings.filter(warning => warning !== 'computedGeometrySamplesNotEngineeringValidation'),
    ];
    for (const change of changes) {
        const value = structuredClone(wire.direct); change(value);
        assert.throws(() => parseConstructionObservation(value, wire.request));
        const group = structuredClone(wire.group); group.results[0].observation = value;
        assert.throws(() => parseObservationResult(group, {items: [{id: 'g', name: 'construction.inventory', args: wire.request}]}));
    }
    const omitted = {...wire.request}; delete omitted.geometryDiagnostics;
    assert.throws(() => parseConstructionObservation(wire.direct, omitted));
});

test('actual MCP direct GET and fixed O7 POST expose the same observed source and computed profile', async () => {
    const calls = [];
    const server = createServer(async (req, res) => {
        let body = ''; for await (const chunk of req) body += chunk;
        calls.push({url: req.url, method: req.method, body: body ? JSON.parse(body) : null});
        res.end(JSON.stringify(req.method === 'POST' ? wire.group : wire.direct));
    });
    server.listen(0, '127.0.0.1'); await once(server, 'listening'); let mcp;
    try {
        mcp = await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
        const direct = await mcp.client.callTool({name: 'cs2_construction_inventory', arguments: wire.request});
        assert.equal(direct.isError, undefined); assert.deepEqual(JSON.parse(direct.content[0].text), wire.direct);
        const args = {expectedWorld: wire.request.world, items: [{id: 'g', name: 'construction.inventory', args: wire.request}]};
        const batch = await mcp.client.callTool({name: 'cs2_observe_batch', arguments: args});
        assert.equal(batch.isError, undefined); assert.deepEqual(JSON.parse(batch.content[0].text), wire.group);
        assert.equal(calls.length, 2); assert.equal(calls[0].method, 'GET');
        assert.equal(new URL(calls[0].url, 'http://test').searchParams.get('geometryDiagnostics'), 'sampledCubicV1');
        assert.deepEqual(calls[1], {url: '/v2/observations', method: 'POST', body: args});
    } finally { await mcp?.close(); server.closeAllConnections(); server.close(); await once(server, 'close'); }
});

test('actual SDK invalid profile options and huge nested keys are bounded with zero HTTP', async () => {
    let calls = 0;
    const server = createServer((_req, res) => { calls++; res.end('{}'); });
    server.listen(0, '127.0.0.1'); await once(server, 'listening'); let mcp;
    try {
        mcp = await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
        const huge = '界'.repeat(100000);
        const cases = [{}, {[huge]: 1}, {...wire.request, [huge]: 1},
            {...wire.request, world: {...wire.request.world, [huge]: 1}},
            {...wire.request, entities: [{...wire.request.entities[0], [huge]: 1}]},
            {...wire.request, entities: [{[huge]: 1}]}, {...wire.request, depth: 2},
            ...[true, 17, null, 'adaptive', 'sampledCubicV2'].map(geometryDiagnostics => ({...wire.request, geometryDiagnostics})),
            {...wire.request, entities: [wire.request.entities[0], wire.request.entities[0]]},
            {...wire.request, entities: [{...wire.request.entities[0], world: {...wire.request.world, epoch: 2}}]},
        ];
        for (const args of cases) for (const name of ['cs2_construction_inventory', 'cs2_observe_batch']) {
            const response = await mcp.client.callTool({name, arguments: name === 'cs2_construction_inventory' ? args : {items: [{id: 'g', name: 'construction.inventory', args}]}});
            assert.equal(response.isError, true); assert.ok(Buffer.byteLength(JSON.stringify(response)) <= 262144); assert.equal(calls, 0);
        }
    } finally { await mcp?.close(); server.closeAllConnections(); server.close(); await once(server, 'close'); }
});

test('computed profiles retain independent direct transport and escaped MCP wrapper byte limits', async () => {
    const root = wire.direct.data.entities[0], profile = wire.direct.data.geometryDiagnostics.profiles[0];
    let selected;
    for (let count = 1; count <= 32; count++) {
        const request = {world: wire.request.world, entities: Array.from({length: count}, (_, i) => ({world: wire.request.world, index: 20 + i, version: 1})), geometryDiagnostics: 'sampledCubicV1'};
        const value = structuredClone(wire.direct); value.data.relationships = null;
        value.data.requestedEntities = structuredClone(request.entities);
        value.data.entities = request.entities.map(entity => ({...structuredClone(root), entity, assetKey: '\u0001'.repeat(256), assetKeyReason: null}));
        value.data.geometryDiagnostics.profiles = request.entities.map(entity => ({...structuredClone(profile), entity}));
        value.data.geometryDiagnostics.evaluations = count * 17;
        const raw = JSON.stringify(value);
        if (Buffer.byteLength(raw) <= 262144 && Buffer.byteLength(JSON.stringify({content: [{type: 'text', text: raw}]})) > 262144) {
            parseConstructionObservation(value, request); selected = {request, value, raw}; break;
        }
    }
    assert.ok(selected, 'no bounded HTTP / oversized wrapper pressure fixture');
    const server = createServer((_req, res) => res.end(selected.raw));
    server.listen(0, '127.0.0.1'); await once(server, 'listening'); let mcp;
    try {
        const client = createBridgeClient({baseUrl: `http://127.0.0.1:${server.address().port}`});
        assert.deepEqual(await client.constructionInventory(selected.request), selected.value);
        mcp = await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
        const result = await mcp.client.callTool({name: 'cs2_construction_inventory', arguments: selected.request});
        assert.equal(result.isError, true); assert.equal(JSON.parse(result.content[0].text).error.code, 'RESULT_TOO_LARGE');
        assert.ok(Buffer.byteLength(JSON.stringify(result)) < 262144);
    } finally { await mcp?.close(); server.closeAllConnections(); server.close(); await once(server, 'close'); }
});

test('actual SDK forwards valid profile mode but refuses a legacy response missing requested profiles', async () => {
    const calls = [];
    const server = createServer((req, res) => { calls.push(req.url); res.end(JSON.stringify(old.direct)); });
    server.listen(0, '127.0.0.1'); await once(server, 'listening'); let mcp;
    try {
        mcp = await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
        const result = await mcp.client.callTool({name: 'cs2_construction_inventory', arguments: input});
        assert.equal(calls.length, 1, 'valid mode rejected before actual GET');
        assert.equal(new URL(calls[0], 'http://test').searchParams.get('geometryDiagnostics'), 'sampledCubicV1');
        assert.equal(result.isError, true, 'requested result must be required');
    } finally { await mcp?.close(); server.closeAllConnections(); server.close(); await once(server, 'close'); }
});
