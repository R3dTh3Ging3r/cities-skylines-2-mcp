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
 const child=spawn(process.execPath,['dist/index.js'],{cwd:new URL('..',import.meta.url),env:{...process.env,CS2_BRIDGE_URL:`http://127.0.0.1:${http.address().port}`},stdio:['pipe','pipe','pipe']});
 child.stderr.on('data',()=>{});const lines=createInterface({input:child.stdout});const pending=new Map();
 lines.on('line',line=>{const response=JSON.parse(line);pending.get(response.id)?.(response);});
 const send=(id,method,params)=>new Promise(resolve=>{pending.set(id,resolve);child.stdin.write(JSON.stringify({jsonrpc:'2.0',id,method,params})+'\n');});
 const exited=once(child,'exit');
 try{
 await send(1,'initialize',{protocolVersion:'2024-11-05',capabilities:{},clientInfo:{name:'lifecycle-test',version:'1'}});
 child.stdin.write(JSON.stringify({jsonrpc:'2.0',method:'notifications/initialized'})+'\n');
 void send(2,'tools/call',{name:'cs2_collect_observations',arguments:spec()});
 await Promise.race([incoming,delay(1500).then(()=>{throw new Error('no HTTP poll/admission');})]);
 if(kind==='eof')child.stdin.end();else child.kill('SIGTERM');
 const exit=await Promise.race([exited,delay(1500).then(()=>{throw new Error('collector kept stdio child alive after disconnect');})]);
 assert.equal(exit[0],0);for(let i=0;!detached&&i<30;i++)await delay(10);assert.equal(detached,true,'pending HTTP was not detached');
 }finally{child.kill('SIGKILL');lines.close();http.closeAllConnections();http.close();await once(http,'close');}
}
test('actual stdio EOF aborts collection HTTP and exits before client termination fallback',()=>actualExit('eof'));
test('actual stdio SIGTERM aborts pending admission HTTP and exits cleanly',()=>actualExit('signal',true));
