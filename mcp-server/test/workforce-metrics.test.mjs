import test from 'node:test';
import assert from 'node:assert/strict';
import {createServer} from 'node:http';
import {once} from 'node:events';
import {readFileSync} from 'node:fs';
import {startOfflineMcp} from './fake-bridge.mjs';
import {createBridgeClient} from '../dist/bridge-client.js';
import {metricKeys, metricReadRequestSchema, metricCatalogSchema, parseMetricObservation, parseMetricBatchObservation} from '../dist/diagnostic-metric-contracts.js';
import {parseObservationResult} from '../dist/observation-contracts.js';
import {compareStoredObservations} from '../dist/observation-comparison.js';

const world = {bootId: 'boot', epoch: 1};
const tiers = ['uneducated', 'poorlyEducated', 'educated', 'wellEducated', 'highlyEducated'];
const keys = ['total', 'free'].flatMap(kind => tiers.map(tier => `labor.jobs.${kind}ByEducation.${tier}`));
const queries = keys.map(key => ({key, parameters: {}}));
const request = {world, scope: {kind: 'city'}, queries};
const legacy = JSON.parse(readFileSync(new URL('./fixtures/diagnostic-metrics-wire.json', import.meta.url), 'utf8'));
const workforce = JSON.parse(readFileSync(process.env.WORKFORCE_WIRE_DIR
    ? `${process.env.WORKFORCE_WIRE_DIR}/workforce.json`
    : new URL('./fixtures/workforce-metrics-wire.json', import.meta.url), 'utf8'));
const collection = () => ({...structuredClone(request), maxSamples: 2, intervalMs: 1000,
    cohort: {kind: 'city', world: structuredClone(world), members: []}});
const exact = (node, required, properties = required) => {
    assert.deepEqual(new Set(node.required), new Set(required));
    assert.deepEqual(Object.keys(node.properties).sort(), [...properties].sort());
    assert.equal(node.additionalProperties, false);
};

test('ten workplace education keys join the v1 contract without invalidating older catalog or batches', () => {
    assert.equal(metricKeys.length, 26);
    assert.deepEqual(metricReadRequestSchema.parse(request), request);
    assert.equal(metricCatalogSchema.parse(legacy.catalog).length, 16);
    assert.deepEqual(parseMetricBatchObservation(legacy.city, legacy['city-request']), legacy.city);
    const catalog = metricCatalogSchema.parse(workforce.catalog);
    assert.equal(catalog.length, 26);
    assert.equal(catalog.filter(definition => definition.key.startsWith('labor.')).length, 14);
    for (const definition of legacy.catalog) assert.deepEqual(catalog.find(row => row.key === definition.key), definition);
    for (const key of keys) {
        const definition = catalog.find(row => row.key === key);
        assert.equal(definition.kind, 'count'); assert.equal(definition.unit, 'workplaceSlots');
        assert.equal(definition.period, null); assert.equal(definition.refresh, 'cachedSimulationCounterRefreshTimeUnknown');
        assert.equal(definition.support.support, 'unverified');
    }
});

test('actual C# workforce direct and O7 wire retain all bucket values and explicit unavailable evidence', async () => {
    assert.deepEqual(workforce.request, request);
    assert.deepEqual(parseMetricObservation(workforce.direct, request), workforce.direct);
    assert.deepEqual(parseMetricBatchObservation(workforce.observation, request), workforce.observation);
    assert.deepEqual(parseMetricBatchObservation(workforce.unavailable, request), workforce.unavailable);
    assert.deepEqual(parseObservationResult(workforce.group, {expectedWorld: world, items: [{id: 'workforce', name: 'metrics.read', args: request}]}), workforce.group);
    assert.equal(workforce.observation.data.clock.sourceObservationId, 'state');
    for (const [index, key] of keys.entries()) {
        assert.equal(workforce.direct.data[key].value, index < 5 ? 11 + index : index === 9 ? 0 : index - 4);
        for (const [name, replacement] of [['unit', 'workers'], ['source', 'CountHouseholdDataSystem.CityWorkerCount']]) {
            const bad = structuredClone(workforce.observation); bad.data.values[key][name] = replacement;
            assert.throws(() => parseMetricBatchObservation(bad, request));
        }
    }
    assert.equal(workforce.unavailable.data.values[keys[0]].value, null);
    assert.equal(workforce.unavailable.data.values[keys[0]].reason, 'workplaceEducationCountersUnavailable');
    assert.equal(workforce.unavailable.data.values[keys[5]].reason, 'invalidNativeCount');
    const calls = [];
    const client = createBridgeClient({baseUrl: 'http://127.0.0.1:1', fetchImpl: async (url, options) => {
        calls.push({url, options});
        return Response.json(options?.method === 'POST' ? workforce.group : workforce.direct);
    }});
    assert.deepEqual(await client.readMetrics(request), workforce.direct);
    assert.deepEqual(await client.observe({expectedWorld: world, items: [{id: 'workforce', name: 'metrics.read', args: request}]}), workforce.group);
    assert.deepEqual(JSON.parse(new URL(calls[0].url).searchParams.get('query')), request);
    assert.equal(calls[1].options.method, 'POST');
});

test('actual metric MCP discovery preserves requiredness, XOR alternatives and all fixed keys', async () => {
    const mcp = await startOfflineMcp('http://127.0.0.1:1');
    try {
        const tools = (await mcp.client.listTools()).tools;
        const schema = tools.find(tool => tool.name === 'cs2_observe').inputSchema;
        const resolve = node => {
            while (node.$ref) node = node.$ref.slice(2).split('/').reduce((value, key) => value[key], schema);
            return node;
        };
        exact(schema, ['world', 'scope'], ['world', 'scope', 'keys', 'queries']);
        for (const field of ['keys', 'queries']) {
            assert.equal(schema.properties[field].minItems, 1);
            assert.equal(schema.properties[field].maxItems, 64);
        }
        assert.deepEqual(schema.properties.keys.items.enum, metricKeys);
        assert.deepEqual(resolve(schema.properties.queries.items.properties.key).enum, metricKeys);
        exact(schema.properties.queries.items, ['key', 'parameters']);
        exact(schema.properties.queries.items.properties.parameters, []);
        exact(schema.properties.world, ['bootId', 'epoch']);
        assert.equal(schema.properties.world.properties.epoch.maximum, Number.MAX_SAFE_INTEGER);
        const scopes = schema.properties.scope.anyOf;
        exact(scopes[0], ['kind']); exact(scopes[1], ['kind', 'line']);
        const line = scopes[1].properties.line;
        exact(line, ['world', 'index', 'version']);
        for (const field of ['index', 'version'])
            assert.deepEqual(line.properties[field], {type: 'integer', minimum: 1, maximum: 2147483647});
        const catalog = tools.find(tool => tool.name === 'cs2_metric_catalog').inputSchema;
        exact(catalog, [], ['family']);
        assert.equal(catalog.properties.family.default, 'all');
        assert.deepEqual(catalog.properties.family.enum, ['all', 'city', 'labor', 'budget', 'transit']);
        const batch = tools.find(tool => tool.name === 'cs2_observe_batch').inputSchema;
        const metricBranch = batch.properties.items.items.anyOf.find(branch => branch.properties.name.const === 'metrics.read');
        assert.deepEqual(metricBranch.properties.args.properties.queries.items.properties.key.enum, metricKeys);
        const collect = tools.find(tool => tool.name === 'cs2_collect_observations').inputSchema;
        assert.deepEqual(collect.properties.queries.items.properties.key.enum, metricKeys);
    } finally { await mcp.close(); }
});

test('actual MCP catalog to collect stop page and stored comparison uses the emitted workforce producer samples', async () => {
    const calls = []; let polls = 0;
    const server = createServer(async (req, res) => {
        calls.push(req.url);
        if (req.url.startsWith('/v2/metrics/catalog')) return res.end(JSON.stringify(workforce.catalog));
        if (req.url.startsWith('/v2/metrics?')) return res.end(JSON.stringify(workforce.direct));
        let body = ''; for await (const chunk of req) body += chunk;
        const input = JSON.parse(body);
        assert.equal(req.url, '/v2/observations');
        assert.deepEqual(input.items[0].args, request);
        const observation = workforce.samples[polls++];
        assert.ok(observation, 'unexpected extra poll');
        res.end(JSON.stringify({world, startFrame: observation.startFrame, endFrame: observation.endFrame,
            worldChanged: false, results: [{id: input.items[0].id, observation}]}));
    });
    server.listen(0, '127.0.0.1'); await once(server, 'listening'); let mcp;
    try {
        mcp = await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
        const tool = async (name, args) => {
            const response = await mcp.client.callTool({name, arguments: args});
            assert.equal(response.isError, undefined, JSON.stringify(response));
            assert.ok(Buffer.byteLength(JSON.stringify(response)) <= 262144);
            return JSON.parse(response.content[0].text);
        };
        assert.equal((await tool('cs2_metric_catalog', {})).length, 26);
        assert.deepEqual(await tool('cs2_observe', {world, scope: {kind: 'city'}, keys}), workforce.direct);
        assert.deepEqual(await tool('cs2_observe', request), workforce.direct);
        const windows = [];
        for (let window = 0; window < 2; window++) {
            const started = await tool('cs2_collect_observations', collection());
            const deadline = Date.now() + 3000;
            while (polls < (window + 1) * 2 && Date.now() < deadline) await new Promise(resolve => setTimeout(resolve, 10));
            assert.equal(polls, (window + 1) * 2);
            let retained;
            do {
                retained = await tool('cs2_read_observation_history', {id: started.id});
                if (retained.samples.length < 2) await new Promise(resolve => setTimeout(resolve, 10));
            } while (retained.samples.length < 2 && Date.now() < deadline);
            assert.equal(retained.samples.length, 2);
            const stopped = await tool('cs2_stop_observation_collection', {id: started.id});
            assert.equal(stopped.sampleCount, 2);
            const page = await tool('cs2_read_observation_history', {id: started.id, limit: 1});
            assert.equal(page.samples.length, 1); assert.equal(page.nextSequence, 2);
            assert.deepEqual(page.samples[0].observation, workforce.samples[window * 2]);
            const next = await tool('cs2_read_observation_history', {id: started.id, afterSequence: page.nextSequence - 1, limit: 1});
            assert.deepEqual(next.samples[0].observation, workforce.samples[window * 2 + 1]);
            windows.push({id: started.id, fromSequence: 1, toSequence: 2});
        }
        const before = calls.length;
        const compared = await tool('cs2_compare_observations', {baseline: windows[0], current: windows[1],
            cohort: collection().cohort, policy: {mode: 'rawNormalizedTime', phaseTolerance: .001,
                maxObservedPhaseStep: .2, frameDurationTolerance: 0, maxSourceSpanFrames: 3}});
        assert.equal(calls.length, before, 'stored comparison fetched native data');
        assert.equal(compared.status, 'comparable', JSON.stringify(compared.reasons)); assert.equal(compared.improvement, null);
        assert.equal(compared.pairs.length, 2);
        assert.equal(compared.pairs[0].baselineEvidence.clock.sourceObservationId, 'state');
        for (const [index, key] of keys.entries()) {
            assert.equal(compared.pairs[0].metrics[key].delta, index < 5 ? 10 : 20);
            assert.equal(compared.changes[key].baselineChange, index < 5 ? 2 : 3);
            assert.equal(compared.changes[key].currentChange, index < 5 ? 2 : 3);
            assert.equal(compared.changes[key].improvement, null);
        }
    } finally {
        await mcp?.close(); server.closeAllConnections(); server.close(); await once(server, 'close');
    }
});

test('actual old catalog refuses absent workforce key before polling and still serves old keys', async () => {
    const calls = [];
    const server = createServer((req, res) => { calls.push(req.url); res.end(JSON.stringify(req.url.startsWith('/v2/metrics?') ? legacy['city-direct'] : legacy.catalog)); });
    server.listen(0, '127.0.0.1'); await once(server, 'listening'); let mcp;
    try {
        mcp = await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
        const old = await mcp.client.callTool({name: 'cs2_metric_catalog', arguments: {}});
        assert.equal(JSON.parse(old.content[0].text).length, 16);
        const observed = await mcp.client.callTool({name: 'cs2_observe', arguments: legacy['city-request']});
        assert.equal(observed.isError, undefined);
        assert.deepEqual(JSON.parse(observed.content[0].text), legacy['city-direct']);
        const response = await mcp.client.callTool({name: 'cs2_collect_observations', arguments: collection()});
        assert.equal(response.isError, true);
        assert.equal(JSON.parse(response.content[0].text).error.code, 'INVALID_ARGUMENT');
        assert.equal(calls.filter(url => url.startsWith('/v2/metrics/catalog')).length, 2);
        assert.equal(calls.filter(url => url === '/v2/observations').length, 0);
        assert.equal(calls.length, 3);
    } finally {
        await mcp?.close(); server.closeAllConnections(); server.close(); await once(server, 'close');
    }
});

test('workforce stored comparisons refuse gaps mismatches and unavailable buckets without zero filling', () => {
    const ids = ['00000000-0000-4000-8000-000000000001', '00000000-0000-4000-8000-000000000002'];
    const setup = () => {
        const rows = ids.map((id, window) => ({id, spec: collection(),
            definitions: workforce.catalog.filter(definition => keys.includes(definition.key)),
            definitionRevision: 'diagnosticMetrics.v1', status: 'stopped', stopReason: 'stopped', nextSequence: 3,
            samples: workforce.samples.slice(window * 2, window * 2 + 2).map((observation, index) => ({
                sequence: index + 1, collectedAtUtc: '2026-10-16T12:00:00Z', observation: structuredClone(observation)})),
            gaps: [], agedGaps: {}}));
        const input = {baseline: {id: ids[0], fromSequence: 1, toSequence: 2}, current: {id: ids[1], fromSequence: 1, toSequence: 2},
            cohort: collection().cohort, policy: {mode: 'rawNormalizedTime', phaseTolerance: .001,
                maxObservedPhaseStep: .2, frameDurationTolerance: 0, maxSourceSpanFrames: 3}};
        return {rows, run: () => compareStoredObservations(input, id => rows.find(row => row.id === id))};
    };
    const valid = setup();
    assert.equal(valid.run().status, 'comparable');
    const missing = valid.rows[0].samples[0].observation.data.values[keys[0]];
    Object.assign(missing, {value: null, support: 'unavailable', reason: 'workplaceEducationCountersUnavailable'});
    const refused = valid.run();
    assert.equal(refused.status, 'notComparable'); assert.equal(refused.improvement, null);
    assert.equal(refused.pairs[0].metrics[keys[0]].baseline.value, null);
    assert.equal(refused.pairs[0].metrics[keys[0]].delta, null);
    assert.equal(refused.changes[keys[0]].baselineChange, null);
    for (const mutate of [
        row => row.gaps.push({reason: 'readError', fromSequence: 1, toSequence: 1, count: 1}),
        row => row.spec.cohort.world.epoch++,
        row => row.samples[1].observation.data.clock.rawNormalizedTime = row.samples[0].observation.data.clock.rawNormalizedTime,
        row => row.samples[0].observation.data.clock = null,
    ]) {
        const fixture = setup(); mutate(fixture.rows[1]);
        assert.equal(fixture.run().status, 'notComparable'); assert.equal(fixture.run().improvement, null);
    }
});

test('catalog completeness preserves the original v1 definitions while accepting bounded additive subsets', async () => {
    for (const catalog of [legacy.catalog, workforce.catalog, [...legacy.catalog, ...workforce.catalog.filter(row => keys.includes(row.key)).slice(0, 1)]]) {
        const client = createBridgeClient({baseUrl: 'http://127.0.0.1', fetchImpl: async () => Response.json(catalog)});
        assert.deepEqual(await client.metricCatalog(), catalog);
    }
    const incomplete = workforce.catalog.filter(row => row.key !== 'city.money');
    const client = createBridgeClient({baseUrl: 'http://127.0.0.1', fetchImpl: async () => Response.json(incomplete)});
    await assert.rejects(() => client.metricCatalog(), error => error.details.code === 'PROTOCOL_ERROR');
});

test('actual metric facades bound malformed huge unknown errors before any HTTP', async () => {
    let calls = 0;
    const server = createServer((_req, res) => { calls++; res.end('{}'); });
    server.listen(0, '127.0.0.1'); await once(server, 'listening'); let mcp;
    try {
        mcp = await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
        const huge = '界'.repeat(100000);
        const valid = {world, scope: {kind: 'city'}, keys: ['city.money']};
        const malformed = [
            {}, {...valid, queries: [{key: 'city.money', parameters: {}}]},
            {world, scope: {kind: 'city'}}, {...valid, keys: ['city.money', 'city.money']},
            {...valid, unknown: true}, {...valid, keys: ['unknown']},
            {...valid, [huge]: 1}, {[huge]: 1},
            {...valid, world: {...world, [huge]: 1}}, {...valid, world: {[huge]: 1}},
            {...valid, scope: {kind: 'city', [huge]: 1}}, {...valid, scope: {[huge]: 1}},
            {world, scope: {kind: 'city'}, queries: [{key: 'city.money', parameters: {[huge]: 1}}]},
            {world, scope: {kind: 'city'}, queries: [{[huge]: 1}]},
            {...valid, scope: {kind: 'selectedLine', line: {world, index: 1, version: 1, [huge]: 1}}},
        ];
        const failures = [];
        for (const [name, args] of [
            ...malformed.map(args => ['cs2_observe', args]),
            ['cs2_metric_catalog', {[huge]: 1}], ['cs2_metric_catalog', {family: 'all', [huge]: 1}],
            ['cs2_metric_catalog', {family: 'unknown'}],
        ]) {
            const result = await mcp.client.callTool({name, arguments: args});
            assert.equal(result.isError, true);
            const bytes = Buffer.byteLength(JSON.stringify(result));
            if (bytes > 262144) failures.push(`${name}: ${bytes} bytes`);
            assert.equal(calls, 0);
        }
        assert.deepEqual(failures, [], 'SDK errors exceeded256KiB');
    } finally {
        await mcp?.close(); server.closeAllConnections(); server.close(); await once(server, 'close');
    }
});
