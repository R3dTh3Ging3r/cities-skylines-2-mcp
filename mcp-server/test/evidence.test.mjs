import test from 'node:test';
import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { createServer } from 'node:http';
import { mkdtemp, mkdir, readFile, writeFile, rm } from 'node:fs/promises';
import { resolve, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const script = join(root, 'scripts/Test-NativeFixture.ps1');
const required = ['catalogReadOnly', 'capabilityExposure', 'buildIdentity', 'worldIdentity',
  'budgetPanel', 'calendarPanel', 'serviceCountsPanel', 'stopRolesPanel', 'sliderBoundsPanel'];
const stamp = '2026-10-11T12:00:00.000Z';
const hash = 'a'.repeat(64);
const definition = JSON.parse(await readFile(join(root, 'tests/native/contracts.json'), 'utf8'));
const catalogSource = await readFile(join(root, 'CS2MCP.Bridge/EndpointCatalog.cs'), 'utf8');
const catalogHash = createHash('sha256').update(catalogSource).digest('hex');
const endpoints = [...catalogSource.matchAll(/new EndpointDescriptor\("([^"]+)", "([^"]+)", "([^"]+)", "([^"]+)"\)/g)]
  .map(([, path, access, capability, completionMechanism]) => ({ path, access, capability, completionMechanism, legacyHttpVerbs: ['GET'] }));
const panelProbes = {
  budgetPanel: ['incomeSign', 'expenseSign', 'balance', 'monthlyPeriod', 'hourlyRefresh'],
  calendarPanel: ['rawDate', 'displayDate', 'monthBoundary', 'yearBoundary'],
  serviceCountsPanel: ['ordinaryBuildings', 'extensions', 'aggregation'],
  stopRolesPanel: ['ordinaryBus', 'ordinaryTram', 'placeableRole', 'placeholderExclusion', 'integratedExclusion'],
  sliderBoundsPanel: ['warmedState', 'taxBoundsAndStep', 'feeBoundsAndStep', 'serviceBoundsAndStep', 'policyBoundsAndStep'],
};

async function run(args) {
  return await new Promise((done, reject) => {
    const child = spawn('powershell', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', script, ...args],
      { cwd: root, windowsHide: true, env: { ...process.env, CS2_BRIDGE_URL: 'http://invalid.example' } });
    let stdout = '', stderr = '';
    child.stdout.on('data', chunk => { stdout += chunk; });
    child.stderr.on('data', chunk => { stderr += chunk; });
    const timer = setTimeout(() => { child.kill(); reject(new Error('PowerShell fixture timed out')); }, 25000);
    child.once('error', error => { clearTimeout(timer); reject(error); });
    child.once('close', code => { clearTimeout(timer); done({ code, stdout, stderr }); });
  });
}

async function workspace(fn) {
  const parent = join(root, '.local/v01');
  await mkdir(parent, { recursive: true });
  const directory = await mkdtemp(join(parent, 'ev-'));
  try { await fn(directory); } finally { await rm(directory, { recursive: true, force: true }); }
}

// Synthetic *input* to the real validator, never recorded as native release evidence.
function report(artifact) {
  const binding = { fixture: 'contracts', buildIdentity: 'candidate+revision', bootId: 'disposable-boot', epoch: 1 };
  const value = {
    schemaVersion: 1, fixture: 'contracts', fixtureVersion: 1, provenance: 'native',
    startedAtUtc: stamp, finishedAtUtc: stamp, limitations: ['synthetic validator test input'],
    build: { identity: { assemblyVersion: '0.4.0', modVersion: '0.9.0', buildIdentity: binding.buildIdentity,
      sourceRevision: 'b'.repeat(40), gameVersion: 'fixture-game' }, identityReasons: {},
      sourceRevision: 'b'.repeat(40), sourceDirty: false, installedSha256: hash,
      candidateSha256: hash, loadedModuleSha256: hash, hashReasons: {} },
    world: { bootId: binding.bootId, epoch: 1, reason: null, startContext: {}, endContext: {} },
    checks: required.map(id => ({ id, status: 'passed',
      expected: Object.fromEntries((panelProbes[id] ?? ['probe']).map(key => [key, 'synthetic matching observation'])),
      actual: Object.fromEntries((panelProbes[id] ?? ['probe']).map(key => [key, 'synthetic matching observation'])),
      observedAtUtc: stamp, provenance: id.endsWith('Panel') ? 'manualPanel' : 'native',
      binding: { ...binding }, artifacts: [artifact], reason: null })),
    artifacts: [artifact], validated: true, compatibilityVerified: true,
  };
  const catalog = { operations: definition.operations, endpoints };
  Object.assign(value.checks[0], { expected: structuredClone(catalog), actual: structuredClone(catalog),
    provenance: 'source', binding: { fixture: 'contracts', fixtureVersion: 1, catalogSha256: catalogHash } });
  const metadata = { protocolVersion: 1, schemaVersion: 1, endpoints };
  Object.assign(value.checks[1], { expected: structuredClone(metadata), actual: {
    ...structuredClone(metadata), identity: structuredClone(value.build.identity),
    runtime: { observedAtUtc: stamp, ageMilliseconds: 0, cityLoaded: true, isLoading: false },
  } });
  const build = { identity: value.build.identity, loadedModuleSha256: value.build.loadedModuleSha256 };
  Object.assign(value.checks[2], { expected: structuredClone(build), actual: structuredClone(build) });
  const world = { bootId: value.world.bootId, epoch: value.world.epoch };
  Object.assign(value.checks[3], { expected: structuredClone(world), actual: structuredClone(world) });
  return value;
}

async function casesAgainstValidator(cases) {
  await workspace(async directory => {
    const artifact = join(directory, 'observation.json');
    await writeFile(artifact, '{}');
    for (const [name, mutate, expected, encode = JSON.stringify] of cases) {
      const value = report(artifact);
      mutate(value);
      const path = join(directory, 'report.json');
      await writeFile(path, encode(value));
      const result = await run(['-ValidateEvidence', path]);
      assert.equal(result.code, expected, `${name}: ${result.stdout}\n${result.stderr}`);
    }
  });
}

test('native identity checks reject offline provenance, stale bindings and contradictory observations', async () => {
  await casesAgainstValidator([
    ['offline build observation', r => { r.checks[2].provenance = 'offline'; }, 1],
    ['offline world observation', r => { r.checks[3].provenance = 'offline'; }, 1],
    ['stale build binding', r => { r.checks[2].binding.buildIdentity = 'old-build'; }, 1],
    ['stale world binding', r => { r.checks[3].binding.epoch = 0; }, 1],
    ['missing identity binding', r => { r.checks[2].binding = null; }, 1],
    ['different fixture binding', r => { r.checks[3].binding.fixture = 'other'; }, 1],
    ['contradictory loaded hash', r => { r.checks[2].actual.loadedModuleSha256 = 'c'.repeat(64); }, 1],
    ['contradictory expected identity', r => { r.checks[2].expected.identity.gameVersion = 'other-game'; }, 1],
    ['contradictory world actual', r => { r.checks[3].actual.bootId = 'other-boot'; }, 1],
    ['contradictory world expected', r => { r.checks[3].expected.epoch = 2; }, 1],
    ['identity observation outside interval', r => { r.checks[2].observedAtUtc = '2026-10-11T11:59:00Z'; }, 1],
  ]);
});

test('offline validation checks imported source catalog and capabilities against authority', async () => {
  await casesAgainstValidator([
    ['protocol/schema99 empty endpoints', r => { r.checks[1].actual = { protocolVersion: 99, schemaVersion: 99, endpoints: [] }; }, 1],
    ['protocol string', r => { r.checks[1].actual.protocolVersion = '1'; }, 1],
    ['schema string', r => { r.checks[1].actual.schemaVersion = '1'; }, 1],
    ['manifest expected mismatch', r => { r.checks[1].expected.protocolVersion = 99; }, 1],
    ['manifest missing route', r => { r.checks[1].actual.endpoints.pop(); }, 1],
    ['manifest duplicate route', r => { r.checks[1].actual.endpoints[1] = r.checks[1].actual.endpoints[0]; }, 1],
    ['manifest write classification', r => { r.checks[1].actual.endpoints.find(e => e.path === '/state').access = 'cityWrite'; }, 1],
    ['manifest wrong method', r => { r.checks[1].actual.endpoints[0].legacyHttpVerbs = ['POST']; }, 1],
    ['manifest extra method', r => { r.checks[1].actual.endpoints[0].legacyHttpVerbs = ['GET', 'POST']; }, 1],
    ['manifest wrong capability', r => { r.checks[1].actual.endpoints[0].capability = 'other'; }, 1],
    ['manifest wrong completion', r => { r.checks[1].actual.endpoints[0].completionMechanism = 'other'; }, 1],
    ['manifest offline provenance', r => { r.checks[1].provenance = 'offline'; }, 1],
    ['manifest stale binding', r => { r.checks[1].binding.bootId = 'old-boot'; }, 1],
    ['manifest wrong identity', r => { r.checks[1].actual.identity.modVersion = 'other'; }, 1],
    ['manifest stale runtime', r => { r.checks[1].actual.runtime.observedAtUtc = '2026-10-11T11:59:50Z'; }, 1],
    ['source operation mutation', r => { r.checks[0].actual.operations[0].path = '/sim/control'; }, 1],
    ['source missing endpoints', r => { r.checks[0].actual.endpoints = []; }, 1],
    ['source wrong metadata', r => { r.checks[0].actual.endpoints[0].completionMechanism = 'other'; }, 1],
    ['source wrong expected operations', r => { r.checks[0].expected.operations = []; }, 1],
    ['source native provenance', r => { r.checks[0].provenance = 'native'; }, 1],
    ['source stale binding', r => { r.checks[0].binding.catalogSha256 = 'c'.repeat(64); }, 1],
  ]);
});

test('actual PowerShell validator gates required evidence and rejects malformed reports', async () => {
  await workspace(async directory => {
    const artifact = join(directory, 'observation.json');
    await writeFile(artifact, '{}');
    const cases = [
      ['complete', () => {}, 0],
      ['failed required', r => { r.checks[4].status = 'failed'; r.checks[4].reason = 'mismatch'; r.validated = r.compatibilityVerified = false; }, 2],
      ['notRun required', r => { r.checks[4].status = 'notRun'; r.checks[4].reason = 'no panel'; r.validated = r.compatibilityVerified = false; }, 2],
      ['unknown version', r => { r.build.identity.gameVersion = null; r.checks[1].actual.identity.gameVersion = null; r.build.identityReasons.gameVersion = 'notObserved'; r.checks[2].status = 'notRun'; r.checks[2].reason = 'unknown game'; r.validated = r.compatibilityVerified = false; }, 2],
      ['false validation claim', r => { r.checks[4].status = 'notRun'; r.checks[4].reason = 'no panel'; }, 1],
      ['invalid fixture', r => { r.fixture = '../contracts'; }, 1],
      ['long artifact', r => { r.artifacts = ['x'.repeat(241)]; }, 1],
      ['artifact traversal', r => { r.artifacts = [join(directory, '../outside.json')]; }, 1],
      ['missing check', r => { r.checks.pop(); }, 1],
      ['duplicate check', r => { r.checks[1] = r.checks[0]; }, 1],
      ['unknown check', r => { r.checks[0].id = 'optionalSuccess'; }, 1],
      ['wrong enum case', r => { r.checks[0].status = 'Passed'; }, 1],
      ['boolean string', r => { r.validated = 'true'; }, 1],
      ['invalid hash', r => { r.build.candidateSha256 = 'bad'; }, 1],
      ['impossible timestamp', r => { r.finishedAtUtc = '2020-01-01T00:00:00Z'; }, 1],
      ['panel mismatch', r => { r.checks[4].actual = { probes: 'different' }; }, 1],
      ['missing panel probe', r => { delete r.checks[4].expected.hourlyRefresh; delete r.checks[4].actual.hourlyRefresh; }, 1],
      ['panel wrong world', r => { r.checks[4].binding = { ...r.checks[4].binding, epoch: 2 }; }, 1],
      ['panel epoch string', r => { r.checks[4].binding.epoch = '1'; }, 1],
      ['synthetic native claim', r => { r.provenance = 'offline'; }, 1],
      ['missing passed artifact', r => { r.checks[4].artifacts = []; }, 1],
    ];
    for (const [name, mutate, expected] of cases) {
      const value = report(artifact);
      mutate(value);
      const path = join(directory, 'report.json');
      await writeFile(path, JSON.stringify(value));
      const result = await run(['-ValidateEvidence', path]);
      assert.equal(result.code, expected, `${name}: ${result.stdout}\n${result.stderr}`);
    }
  });
});

test('actual panel comparison ignores nested object key order and preserves JSON types and array order', async () => {
  const nested = { amount: 25, enabled: true, note: null, items: [{ x: 1, y: 'two' }, 3] };
  const set = (r, actual) => {
    r.checks[4].expected.balance = structuredClone(nested);
    r.checks[4].actual.balance = actual;
  };
  await casesAgainstValidator([
    ['nested keys reordered', r => {
      set(r, { items: [{ y: 'two', x: 1 }, 3], note: null, enabled: true, amount: 25 });
      r.checks[4].actual = Object.fromEntries(Object.entries(r.checks[4].actual).reverse());
    }, 0],
    ['equivalent numeric CLR representations', r => set(r, structuredClone(nested)), 0,
      r => JSON.stringify(r).replace('"amount":25', '"amount":25.0')],
    ['distinct large double and integer', r => set(r, { ...nested, amount: 'largeIntegerFixture' }), 1,
      r => JSON.stringify(r).replace('"amount":25', '"amount":9007199254740992.0')
        .replace('"largeIntegerFixture"', '9007199254740993')],
    ['empty nested objects and arrays', r => set(r, { ...nested, items: [[], {}] }), 1],
    ['equal empty nested containers', r => {
      r.checks[4].expected.balance = { object: {}, array: [] };
      r.checks[4].actual.balance = { array: [], object: {} };
    }, 0],
    ['nested number changed', r => set(r, { ...nested, amount: 26 }), 1],
    ['number becomes string', r => set(r, { ...nested, amount: '25' }), 1],
    ['boolean becomes number', r => set(r, { ...nested, enabled: 1 }), 1],
    ['null becomes empty string', r => set(r, { ...nested, note: '' }), 1],
    ['string case differs', r => set(r, { ...nested, items: [{ x: 1, y: 'Two' }, 3] }), 1],
    ['object key case differs', r => set(r, { ...nested, items: [{ X: 1, y: 'two' }, 3] }), 1],
    ['array order differs', r => set(r, { ...nested, items: [3, { x: 1, y: 'two' }] }), 1],
    ['extra nested key', r => set(r, { ...nested, extra: 1 }), 1],
    ['object becomes array', r => set(r, Object.values(nested)), 1],
    ['empty object differs from empty array', r => {
      r.checks[4].expected.balance = {};
      r.checks[4].actual.balance = [];
    }, 1],
  ]);
});

test('registered read fixture collects explicit fake loopback evidence without native parity', async () => {
  assert.ok(endpoints.length > 0);
  for (const operation of definition.operations) {
    assert.equal(endpoints.find(e => e.path === operation.path)?.access, 'read');
    assert.equal(operation.method, 'GET');
  }
  const requests = [];
  let redirect = false;
  let missingManifest = false;
  let malformedRuntime = false;
  const sockets = new Set();
  const server = createServer((req, res) => {
    requests.push(req.url);
    if (redirect) { res.writeHead(302, { Location: 'http://invalid.example/' }); res.end(); return; }
    const response = req.url === '/capabilities' ? {
      protocolVersion: 1, schemaVersion: 1, endpoints: missingManifest ? [] : endpoints,
      identity: { assemblyVersion: '0.4.0', modVersion: '0.9.0', buildIdentity: 'fake', sourceRevision: null, gameVersion: null },
      runtime: { observedAtUtc: new Date().toISOString(), ageMilliseconds: malformedRuntime ? true : 0, cityLoaded: true, isLoading: false },
    } : { fixture: true, cityName: 'Fake disposable', frame: 1 };
    res.setHeader('Content-Type', 'application/json'); res.end(JSON.stringify(response));
  });
  server.on('connection', s => { sockets.add(s); s.on('close', () => sockets.delete(s)); });
  await new Promise(done => server.listen(0, '127.0.0.1', done));
  const url = `http://127.0.0.1:${server.address().port}`;
  try {
    await workspace(async directory => {
      let result = await run(['-Fixture', 'contracts', '-EvidenceDir', directory, '-BaseUrl', url, '-Offline']);
      assert.equal(result.code, 2, result.stdout + result.stderr);
      const value = JSON.parse(await readFile(join(directory, 'report.json'), 'utf8'));
      assert.equal(value.validated, false);
      assert.equal(value.compatibilityVerified, false);
      assert.equal(value.provenance, 'offline');
      assert.ok(value.checks.filter(c => c.id.endsWith('Panel')).every(c => c.status === 'notRun'));
      assert.equal(value.world.bootId, null);
      assert.deepEqual(requests, ['/capabilities', ...definition.operations.filter(o => o.path !== '/capabilities').map(o => o.path)]);
      result = await run(['-ValidateEvidence', join(directory, 'report.json')]);
      assert.equal(result.code, 2, result.stdout + result.stderr);
      requests.length = 0;
      malformedRuntime = true;
      result = await run(['-Fixture', 'contracts', '-EvidenceDir', directory, '-BaseUrl', url, '-Offline']);
      assert.equal(result.code, 2, result.stdout + result.stderr);
      assert.deepEqual(requests, ['/capabilities']);
      requests.length = 0;
      malformedRuntime = false;
      missingManifest = true;
      result = await run(['-Fixture', 'contracts', '-EvidenceDir', directory, '-BaseUrl', url, '-Offline']);
      assert.equal(result.code, 2, result.stdout + result.stderr);
      assert.deepEqual(requests, ['/capabilities']);
      requests.length = 0;
      redirect = true;
      result = await run(['-Fixture', 'contracts', '-EvidenceDir', directory, '-BaseUrl', url, '-Offline']);
      assert.equal(result.code, 2, result.stdout + result.stderr);
      assert.deepEqual(requests, ['/capabilities']);
      requests.length = 0;
      for (const args of [
        ['-Fixture', 'mutation', '-EvidenceDir', directory, '-BaseUrl', url],
        ['-Fixture', 'contracts', '-EvidenceDir', directory, '-BaseUrl', 'https://example.com'],
        ['-Fixture', 'contracts', '-EvidenceDir', directory, '-BaseUrl', url + '/escape'],
        ['-Fixture', 'contracts', '-EvidenceDir', root, '-BaseUrl', url],
      ]) assert.equal((await run(args)).code, 1);
      assert.deepEqual(requests, []);
    });
  } finally {
    for (const socket of sockets) socket.destroy();
    await new Promise(done => server.close(done));
  }
});
