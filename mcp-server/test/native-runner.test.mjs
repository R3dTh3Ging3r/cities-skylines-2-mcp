import test from 'node:test';
import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { createServer } from 'node:http';
import { mkdtemp, mkdir, readFile, writeFile, rm, symlink, readdir } from 'node:fs/promises';
import { resolve, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const script = join(root, 'scripts/Test-NativeFixture.ps1');
const source = await readFile(join(root, 'CS2MCP.Bridge/EndpointCatalog.cs'), 'utf8');
const endpoints = [...source.matchAll(/new EndpointDescriptor\("([^"]+)", "([^"]+)", "([^"]+)", "([^"]+)"\)/g)]
  .map(([, path, access, capability, completionMechanism]) => ({ path, access, capability, completionMechanism, legacyHttpVerbs: ['GET'] }));
const probeIds = ['preloadOrder', 'toolApplyFinish', 'manualToolSwitch', 'saveIdentity', 'previewOwnership'];
const state = { world: { bootId: 'fake-boot', epoch: 1 }, city: { identity: 'fake-city' } };
const context = () => ({ schemaVersion: 1, fixture: 'lifecycle', disposableCityConfirmed: true,
  world: structuredClone(state.world), city: structuredClone(state.city), permittedFixtureScope: ['lifecycle'],
  checkpoint: { name: 'fixture-baseline', assetIdentity: 'fake-checkpoint', verified: true,
    separatelyNamed: true, cityIdentity: 'fake-city', world: structuredClone(state.world) } });

async function run(args) {
  return new Promise((done, reject) => {
    const child = spawn('powershell', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', script, ...args],
      { cwd: root, windowsHide: true });
    let stdout = '', stderr = '';
    child.stdout.on('data', c => { stdout += c; }); child.stderr.on('data', c => { stderr += c; });
    const timer = setTimeout(() => { child.kill(); reject(new Error('runner timed out')); }, 25000);
    child.once('error', e => { clearTimeout(timer); reject(e); });
    child.once('close', code => { clearTimeout(timer); done({ code, stdout, stderr }); });
  });
}
async function fixture(fn) {
  const parent = join(root, '.local'); await mkdir(parent, { recursive: true });
  const directory = await mkdtemp(join(parent, 'o1-'));
  const requests = []; let responseMode = 'valid';
  const server = createServer((req, res) => {
    requests.push({ method: req.method, path: req.url });
    // An incomplete JSON body after a successful HTTP response avoids HttpWebRequest's
    // transparent GET reconnection and exercises the runner's own stop/no-repeat policy.
    if (responseMode === 'broken') { res.setHeader('Content-Type', 'application/json'); res.end('{"partial":'); return; }
    const value = req.url === '/capabilities' ? { protocolVersion: 1, schemaVersion: 1, endpoints,
      identity: { assemblyVersion: '0.4.0', modVersion: '0.9.0', buildIdentity: 'fake', sourceRevision: null, gameVersion: null },
      runtime: { observedAtUtc: new Date().toISOString(), ageMilliseconds: 0, cityLoaded: true, isLoading: false },
      nativeValidation: { enabled: true, dispatchAvailable: false, reason: 'operationGuardsNotImplemented',
        probes: probeIds.map(id => ({ id, fixture: 'lifecycle', validationOnly: true, support: 'unverified', dispatchAvailable: false })) },
    } : state;
    if (req.url === '/capabilities' && responseMode === 'disabled') value.nativeValidation.enabled = false;
    if (req.url === '/capabilities' && responseMode === 'forged') value.nativeValidation.dispatchAvailable = true;
    res.setHeader('Content-Type', 'application/json'); res.end(JSON.stringify(value));
  });
  await new Promise(done => server.listen(0, '127.0.0.1', done));
  const url = `http://127.0.0.1:${server.address().port}`;
  const collect = extra => {
    const options = new Map([['-Fixture', 'lifecycle'], ['-EvidenceDir', directory], ['-BaseUrl', url], ['-Offline', null]]);
    for (let i = 0; i < extra.length; i++) options.set(extra[i], extra[i + 1]?.startsWith('-') || i + 1 === extra.length ? null : extra[++i]);
    return run([...options].flatMap(([key, value]) => value === null ? [key] : [key, value]));
  };
  try { await fn({ directory, requests, collect, url, setMode: mode => { responseMode = mode; } }); }
  finally { await new Promise(done => server.close(done)); await rm(directory, { recursive: true, force: true }); }
}
async function resultReport(f, extra = []) {
  const result = await f.collect(extra);
  assert.equal(result.code, 2, result.stdout + result.stderr);
  const value = JSON.parse(await readFile(join(f.directory, 'report.json'), 'utf8'));
  assert.equal(value.fixture, 'lifecycle'); assert.equal(value.nativeOutcome, 'notRun');
  assert.equal(value.validated, false); assert.equal(value.compatibilityVerified, false);
  assert.deepEqual(value.receipts, []);
  assert.ok(value.checks.filter(c => probeIds.includes(c.id)).every(c => c.status === 'notRun'));
  assert.ok(f.requests.every(r => r.method === 'GET' && ['/capabilities', '/state'].includes(r.path)));
  if (value.checks.find(c => c.id === 'capabilityExposure').status === 'passed')
    assert.deepEqual(f.requests, [{ method: 'GET', path: '/capabilities' }, { method: 'GET', path: '/state' }]);
  return value;
}

test('native_runner_refuses_mutation_by_default', async () => {
  await fixture(async f => {
    const value = await resultReport(f);
    assert.ok(value.preparation.refusalReasons.includes('mutationOptInRequired'));
    assert.equal(value.provenance, 'offline'); assert.equal(value.world.bootId, null);
    const before = f.requests.length;
    assert.equal((await run(['-ValidateEvidence', join(f.directory, 'report.json')])).code, 2);
    assert.equal(f.requests.length, before, 'offline validator must not contact bridge');
  });
});
test('rejects_missing_or_mismatched_fixture_context', async () => {
  for (const [mutate, reason] of [
    [null, 'fixtureContextMissing'],
    [c => { c.world.bootId = 'other'; }, 'fixtureWorldMismatch'],
    [c => { c.city.identity = 'other'; }, 'fixtureCityMismatch'],
    [c => { c.permittedFixtureScope = ['other']; }, 'fixtureScopeMismatch'],
    [c => { c.disposableCityConfirmed = false; }, 'disposableCityNotConfirmed'],
  ]) await fixture(async f => {
    if (mutate) { const c = context(); mutate(c); await writeFile(join(f.directory, 'fixture-context.json'), JSON.stringify(c)); }
    const value = await resultReport(f, ['-AllowMutation']);
    assert.equal(value.preparation.status, 'failed'); assert.ok(value.preparation.refusalReasons.includes(reason), JSON.stringify(value.preparation));
  });
});
test('rejects_unverified_checkpoint', async () => {
  for (const [mutate, reason] of [
    [c => { c.checkpoint.verified = false; }, 'checkpointNotVerified'],
    [c => { c.checkpoint.separatelyNamed = false; }, 'checkpointNotSeparatelyNamed'],
    [c => { c.checkpoint.cityIdentity = 'other'; }, 'checkpointContextMismatch'],
    [c => { c.checkpoint.world.epoch = 0; }, 'checkpointContextMismatch'],
  ]) await fixture(async f => {
    const c = context(); mutate(c); await writeFile(join(f.directory, 'fixture-context.json'), JSON.stringify(c));
    const value = await resultReport(f, ['-AllowMutation']);
    assert.ok(value.preparation.refusalReasons.includes(reason), JSON.stringify(value.preparation));
  });
});
test('opt-in context never substitutes for producer guards or real evidence', async () => {
  await fixture(async f => {
    await writeFile(join(f.directory, 'fixture-context.json'), JSON.stringify(context()));
    const value = await resultReport(f, ['-AllowMutation']);
    assert.ok(value.preparation.refusalReasons.includes('operationGuardsNotImplemented'));
    assert.ok(value.preparation.refusalReasons.includes('freshWorldLeaseCheckpointEvidenceUnavailable'));
    assert.equal(value.world.bootId, null);
    assert.ok(value.artifacts.some(p => p.endsWith('fixture-context-snapshot.json')));
    const before = await readFile(join(f.directory, 'report.json'), 'utf8');
    const calls = f.requests.length;
    assert.equal((await f.collect(['-AllowMutation'])).code, 1);
    assert.equal(f.requests.length, calls);
    assert.equal(await readFile(join(f.directory, 'report.json'), 'utf8'), before);
  });
});
test('evidence confinement runs before HTTP or writes', async () => {
  await fixture(async f => {
    for (const args of [
      ['-EvidenceRoot', join(root, '.local/v04'), '-EvidenceDir', f.directory],
      ['-EvidenceRoot', root],
      ['-EvidenceDir', root],
      ['-EvidenceDir', join(f.directory, 'x'.repeat(241))],
    ]) {
      const result = await f.collect(args); assert.equal(result.code, 1, result.stdout + result.stderr);
    }
    assert.deepEqual(f.requests, []);
    assert.equal((await f.collect(['-EvidenceRoot', join(root, '.local')])).code, 2);
  });
});
test('unknown read result stops collection and is retained without retry', async () => {
  await fixture(async f => {
    f.setMode('broken'); const value = await resultReport(f);
    assert.deepEqual(f.requests, [{ method: 'GET', path: '/capabilities' }]);
    assert.ok(value.preparation.refusalReasons.includes('readOutcomeUnknown'));
    assert.ok(value.artifacts.some(p => p.endsWith('refusal.json')));
  });
});
test('junction ancestors cannot escape the supplied evidence root', async () => {
  await fixture(async f => {
    const suppliedRoot = join(f.directory, 'ev'); const target = join(f.directory, 'target');
    await mkdir(suppliedRoot); await mkdir(target);
    const link = join(suppliedRoot, 'link'); await symlink(target, link, 'junction');
    const result = await f.collect(['-EvidenceRoot', suppliedRoot, '-EvidenceDir', join(link, 'run')]);
    assert.equal(result.code, 1, result.stdout + result.stderr);
    assert.match(result.stderr, /reparse/i);
    assert.deepEqual(f.requests, []); assert.deepEqual(await readdir(target), []);
  });
});
test('validation mode is required and fabricated dispatch metadata is refused', async () => {
  for (const mode of ['disabled', 'forged']) await fixture(async f => {
    f.setMode(mode); await writeFile(join(f.directory, 'fixture-context.json'), JSON.stringify(context()));
    const value = await resultReport(f, ['-AllowMutation']);
    if (mode === 'disabled') assert.ok(value.preparation.refusalReasons.includes('validationModeDisabled'));
    else {
      assert.equal(value.checks.find(c => c.id === 'capabilityExposure').status, 'failed');
      assert.deepEqual(f.requests, [{ method: 'GET', path: '/capabilities' }]);
    }
  });
});
test('malformed context is preserved and preexisting receipts cannot be overwritten', async () => {
  await fixture(async f => {
    await writeFile(join(f.directory, 'fixture-context.json'), '{"unfinished":');
    const value = await resultReport(f, ['-AllowMutation']);
    assert.ok(value.preparation.refusalReasons.includes('fixtureContextMalformed'));
    assert.equal(await readFile(join(f.directory, 'fixture-context-snapshot.json'), 'utf8'), '{"unfinished":');
  });
  await fixture(async f => {
    const receipt = join(f.directory, 'receipt-00.json'); await writeFile(receipt, '{"outcome":"unknown"}');
    assert.equal((await f.collect(['-AllowMutation'])).code, 1);
    assert.deepEqual(f.requests, []);
    assert.equal(await readFile(receipt, 'utf8'), '{"outcome":"unknown"}');
  });
});
test('lifecycle offline validator rejects forged success and erased refusals', async () => {
  await fixture(async f => {
    const value = await resultReport(f); const reportPath = join(f.directory, 'report.json');
    for (const mutate of [
      v => { v.nativeOutcome = 'passed'; },
      v => { v.checks.find(c => c.id === 'saveIdentity').status = 'passed'; },
      v => { v.preparation.refusalReasons = []; },
      v => { v.receipts = [{ outcome: 'applied' }]; },
      v => { v.preparation.dispatchAvailable = true; },
      v => { v.checks.find(c => c.id === 'preloadOrder').actual = { observed: true }; },
      v => { v.checks.find(c => c.id === 'preloadOrder').expected = ['invented assertion']; },
    ]) {
      const forged = structuredClone(value); mutate(forged); await writeFile(reportPath, JSON.stringify(forged));
      assert.equal((await run(['-ValidateEvidence', reportPath])).code, 1);
    }
  });
});
// These cases must exercise real admission, not an O1 test-only authorization model.
test.todo('validation_mode_does_not_bypass_world_or_lease — wire to O2/O5 admission suites');
test.todo('release_defaults_reject_probe_commands — wire to O2/O5 command admission');
