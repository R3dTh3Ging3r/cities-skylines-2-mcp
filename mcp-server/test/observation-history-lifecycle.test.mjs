import test from 'node:test';
import assert from 'node:assert/strict';
import {PassThrough} from 'node:stream';
import {EventEmitter,once} from 'node:events';
import {spawn} from 'node:child_process';
import {createInterface} from 'node:readline';
import {createServer} from 'node:http';
import {McpServer} from '@modelcontextprotocol/sdk/server/mcp.js';
import {StdioServerTransport} from '@modelcontextprotocol/sdk/server/stdio.js';
import {bindObservationHistoryLifecycle} from '../dist/observation-history-lifecycle.js';
import {spec,wire} from './history-fixtures.mjs';
const delay=ms=>new Promise(resolve=>setTimeout(resolve,ms));

test('real SDK close preserves throwing prior callbacks and removes only owned hooks',async()=>{
 const stdin=new PassThrough(),stdout=new PassThrough(),signals=new EventEmitter(),errors=[];
 const server=new McpServer({name:'test',version:'1'}),transport=new StdioServerTransport(stdin,stdout);
 let disposed=0,priorTransport=0,priorServer=0,other=0;
 const existing=()=>other++;signals.on('SIGTERM',existing);
 transport.onclose=()=>{priorTransport++;throw new Error('prior transport');};
 server.server.onclose=()=>{priorServer++;throw new Error('prior server');};
 const lifecycle=bindObservationHistoryLifecycle({server,transport,history:{dispose:()=>disposed++},stdin,stdout,process:signals,reportError:e=>errors.push(e)});
 await server.connect(transport);assert.equal(stdin.listenerCount('end'),1);
 server.server.onerror?.(new Error('recoverable protocol report'));assert.equal(disposed,0);
 await server.close();await lifecycle.shutdown();
 assert.equal(disposed,1);assert.equal(priorTransport,1);assert.equal(priorServer,1);assert.equal(errors.length,2);
 assert.equal(stdin.listenerCount('data'),0);assert.equal(stdin.listenerCount('end'),0);assert.equal(signals.listenerCount('SIGTERM'),1);signals.emit('SIGTERM');assert.equal(other,1);
});
test('EOF stream error signal and failed connect synchronously dispose with normal SDK close',async()=>{
 for(const mode of ['eof','error','signal','connectFailure']){
 const stdin=new PassThrough(),stdout=new PassThrough(),signals=new EventEmitter();
 const server=new McpServer({name:'test',version:'1'}),transport=new StdioServerTransport(stdin,stdout);let disposed=0;
 const lifecycle=bindObservationHistoryLifecycle({server,transport,history:{dispose:()=>disposed++},stdin,stdout,process:signals,reportError:()=>{}});
 if(mode==='connectFailure'){transport.start=async()=>{throw new Error('start failed');};await assert.rejects(server.connect(transport));await lifecycle.shutdown();}
 else {await server.connect(transport);if(mode==='eof')stdin.emit('end');if(mode==='error')stdout.emit('error',new Error('broken pipe'));if(mode==='signal')signals.emit('SIGTERM');assert.equal(disposed,1);await lifecycle.shutdown();}
 assert.equal(disposed,1);assert.equal(signals.listenerCount('SIGTERM'),0);
 }
});

async function actualExit(kind,admission=false){
 let entered;const incoming=new Promise(resolve=>entered=resolve);let detached=false;
 const http=createServer(async(req,res)=>{
 if(!admission&&req.url.startsWith('/v2/metrics/catalog'))return res.end(JSON.stringify(wire.catalog));
 for await(const chunk of req){}
 res.on('close',()=>detached=true);entered();
 });http.listen(0,'127.0.0.1');await once(http,'listening');
 const windowsSignal=process.platform==='win32'&&kind==='signal';
 const entryUrl=new URL('../dist/index.js',import.meta.url).href;
 // Windows child.kill('SIGTERM') is forceful, so use IPC to emit the same Node signal event there.
 const windowsBootstrap=`process.on('message',message=>{if(message==='SIGTERM'){process.emit('SIGTERM');process.disconnect();}});void import(${JSON.stringify(entryUrl)});`;
 const child=spawn(process.execPath,windowsSignal?['-e',windowsBootstrap]:['dist/index.js'],{cwd:new URL('..',import.meta.url),env:{...process.env,CS2_BRIDGE_URL:`http://127.0.0.1:${http.address().port}`},stdio:windowsSignal?['pipe','pipe','pipe','ipc']:['pipe','pipe','pipe']});
 child.stderr.on('data',()=>{});const lines=createInterface({input:child.stdout});const pending=new Map();
 lines.on('line',line=>{const response=JSON.parse(line);pending.get(response.id)?.(response);});
 const send=(id,method,params)=>new Promise(resolve=>{pending.set(id,resolve);child.stdin.write(JSON.stringify({jsonrpc:'2.0',id,method,params})+'\n');});
 const exited=once(child,'exit');
 try{
 await send(1,'initialize',{protocolVersion:'2024-11-05',capabilities:{},clientInfo:{name:'lifecycle-test',version:'1'}});
 child.stdin.write(JSON.stringify({jsonrpc:'2.0',method:'notifications/initialized'})+'\n');
 void send(2,'tools/call',{name:'cs2_collect_observations',arguments:spec()});
 await Promise.race([incoming,delay(1500).then(()=>{throw new Error('no HTTP poll/admission');})]);
 if(kind==='eof')child.stdin.end();else if(windowsSignal)child.send('SIGTERM');else child.kill('SIGTERM');
 const exit=await Promise.race([exited,delay(1500).then(()=>{throw new Error('collector kept stdio child alive after disconnect');})]);
 assert.equal(exit[0],0);for(let i=0;!detached&&i<30;i++)await delay(10);assert.equal(detached,true,'pending HTTP was not detached');
 }finally{child.kill('SIGKILL');lines.close();http.closeAllConnections();http.close();await once(http,'close');}
}
test('actual stdio EOF aborts collection HTTP and exits before client termination fallback',()=>actualExit('eof'));
test('stdio SIGTERM lifecycle aborts pending admission HTTP and exits cleanly',()=>actualExit('signal',true));

test('actual SDK keeps error hooks through queued onclose errors for owner and external close',async()=>{
 for(const external of [false,true]){
 const stdin=new PassThrough(),stdout=new PassThrough(),signals=new EventEmitter(),reported=[];
 const server=new McpServer({name:'queued-errors',version:'1'}),transport=new StdioServerTransport(stdin,stdout);let disposed=0,settled=false,probe;
 const observed=new Promise(resolve=>probe=resolve);
 server.server.onclose=()=>{queueMicrotask(()=>{
 const errors=[];for(const stream of [stdin,stdout])try{stream.emit('error',new Error('queued during close'));}catch(error){errors.push(error);}
 probe({settled,errorCount:errors.length,listeners:stdout.listenerCount('error')});
 });throw new Error('prior callback still contained');};
 const lifecycle=bindObservationHistoryLifecycle({server,transport,history:{dispose:()=>disposed++},stdin,stdout,process:signals,reportError:e=>reported.push(e)});
 await server.connect(transport);const closing=external?server.close():lifecycle.shutdown();closing.then(()=>settled=true);
 const result=await observed;await closing;
 assert.equal(result.settled,false);assert.equal(result.errorCount,0,'queued stream error escaped while close was pending');assert.ok(result.listeners>0);assert.equal(disposed,1);assert.equal(stdout.listenerCount('error'),0);assert.equal(signals.listenerCount('SIGTERM'),0);assert.equal(reported.length,3);
 }
});

test('pending delayed SDK close and rejection hold error hooks without recursive close',async()=>{
 for(const rejected of [false,true])for(const external of [false,true]){
 const stdin=new PassThrough(),stdout=new PassThrough(),signals=new EventEmitter(),reported=[];
 const server=new McpServer({name:'delayed-close',version:'1'}),transport=new StdioServerTransport(stdin,stdout);
 const originalClose=server.close;let finish,entered,closeCalls=0,disposed=0;
 const pending=new Promise(resolve=>finish=resolve),started=new Promise(resolve=>entered=resolve);
 // Extend the public close promise around the real SDK close, without replacing protocol internals.
 server.close=async function(){closeCalls++;await originalClose.call(this);entered();await pending;if(rejected)throw new Error('delayed close rejected');};
 const unrelated=()=>{};stdout.on('error',unrelated);signals.on('SIGTERM',unrelated);
 const lifecycle=bindObservationHistoryLifecycle({server,transport,history:{dispose:()=>disposed++},stdin,stdout,process:signals,reportError:e=>reported.push(e)});
 await server.connect(transport);const closing=external?server.close():lifecycle.shutdown();const result=closing.then(()=>null,error=>error);
 await started;assert.equal(disposed,1);assert.equal(stdout.listenerCount('error'),2);
 for(let i=0;i<3;i++){stdin.emit('error',new Error('pending stdin'));stdout.emit('error',new Error('pending stdout'));}await Promise.resolve();assert.equal(closeCalls,1);
 finish();const error=await result;if(external&&rejected)assert.equal(error.message,'delayed close rejected');else assert.equal(error,null);
 await lifecycle.shutdown();assert.equal(closeCalls,1);assert.equal(stdout.listenerCount('error'),1);assert.equal(signals.listenerCount('SIGTERM'),1);assert.equal(stdin.listenerCount('error'),0);
 }
});
