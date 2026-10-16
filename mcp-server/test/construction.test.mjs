import assert from 'node:assert/strict';
import test from 'node:test';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
import {startOfflineMcp} from './fake-bridge.mjs';
import {createBridgeClient} from '../dist/bridge-client.js';
import {createServer} from 'node:http';
import {once} from 'node:events';
import {readFileSync} from 'node:fs';
const wire=JSON.parse(readFileSync(new URL('./fixtures/construction-relationships-wire.json',import.meta.url),'utf8'));
function baseline(world,entities){const value=structuredClone(wire.baseline);value.world=world;value.data.requestedEntities=entities;value.data.entities=entities.map(entity=>({...structuredClone(wire.baseline.data.entities[0]),entity}));return value;}
async function startInventoryBridge(){const requests=[];const server=createServer((req,res)=>{const u=new URL(req.url,'http://test');const query=Object.fromEntries(u.searchParams);requests.push({path:u.pathname,query,rawQuery:u.search.slice(1),method:req.method});res.end(JSON.stringify(baseline({bootId:query.bootId,epoch:Number(query.epoch)},query.entities.split(',').map(ref=>{const [index,version]=ref.split(':').map(Number);return {world:{bootId:query.bootId,epoch:Number(query.epoch)},index,version};}))));});server.listen(0,'127.0.0.1');await once(server,'listening');return {url:`http://127.0.0.1:${server.address().port}`,requests,close:async()=>{server.closeAllConnections();server.close();await once(server,'close');}};}
const world={bootId:'boot + /?',epoch:7};
const entity=(index=1)=>({world,index,version:2});
test('actual MCP registration encodes observed world and 32 explicit entity refs through typed inventory transport',async()=>{
 const fake=await startInventoryBridge();let mcp;
 try {
  mcp=await startOfflineMcp(fake.url);
  const {tools}=await mcp.client.listTools();const tool=tools.find(t=>t.name==='cs2_construction_inventory');assert.ok(tool,'bounded inventory registration missing');
  assert.match(tool.description,/root.*geometry|root.*bounds/i);assert.match(tool.description,/32/);
  const result=await mcp.client.callTool({name:tool.name,arguments:{world,entities:Array.from({length:32},(_,i)=>entity(i+1))}});
  assert.equal(result.isError,undefined);const req=fake.requests.at(-1);assert.equal(req.path,'/v2/construction/inventory');assert.equal(req.query.bootId,world.bootId);assert.equal(req.query.epoch,'7');
  assert.equal(req.query.entities,Array.from({length:32},(_,i)=>`${i+1}:2`).join(','));assert.ok(req.rawQuery.includes('%20'));assert.equal(req.method,'GET');
  const before=fake.requests.length;
  for(const patch of [{entities:[]},{entities:Array.from({length:33},(_,i)=>entity(i+1))},{entities:[entity(),entity()]},{entities:[entity(),entity(1.5)]},{entities:[{...entity(),world:{...world,epoch:8}}]},{world:{...world,epoch:Number.MAX_SAFE_INTEGER+1}},{cursor:'x'},{includeFamilies:true},{family:'roads'},{bbox:{min:{x:0,y:0,z:0},max:{x:1,y:1,z:1}}}]) {
   const r=await mcp.client.callTool({name:tool.name,arguments:{world,entities:[entity()],...patch}});assert.equal(r.isError,true,JSON.stringify(patch));assert.equal(fake.requests.length,before,JSON.stringify(patch));
  }
  assert.equal(fake.requests.length,before);
 }finally{await mcp?.close();await fake.close();}
});
test('typed inventory client validates independently before fetching and bounds encoded query',async()=>{
 let calls=0,url;const bridge=createBridgeClient({baseUrl:'http://127.0.0.1:1234',fetchImpl:async u=>{calls++;url=u;return Response.json(baseline(world,[entity()]));}});
 assert.equal(typeof bridge.constructionInventory,'function','typed inventory transport missing');
 await bridge.constructionInventory({world,entities:[entity()]});assert.equal(calls,1);assert.match(url,/bootId=boot%20%2B%20%2F%3F/);
 for(const patch of [{entities:[]},{entities:[{...entity(),index:NaN}]},{entities:[{...entity(),version:Infinity}]},{world:{...world,bootId:'a'.repeat(129)}},{entities:[entity(),{...entity(),version:3}]},{spatial:{}}])
  await assert.rejects(bridge.constructionInventory({world,entities:[entity()],...patch}));
 assert.equal(calls,1);
});

test('shared Observation TypeScript contract requires data freshness warnings and nullable evidence',()=>{
 const root=fileURLToPath(new URL('../',import.meta.url));
 const result=spawnSync(process.execPath,['node_modules/typescript/bin/tsc','--noEmit','--strict','--skipLibCheck',
  '--target','ES2022','--module','Node16','--moduleResolution','Node16','test/construction-types.ts'],{cwd:root,encoding:'utf8',windowsHide:true});
 assert.equal(result.status,0,result.stdout+result.stderr);
});
