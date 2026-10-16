import test from 'node:test';
import assert from 'node:assert/strict';
import {createServer} from 'node:http';
import {once} from 'node:events';
import {createBridgeClient} from '../dist/bridge-client.js';
test('metric catalog caller cancellation closes actual pending HTTP before timeout',async()=>{
 const server=createServer();
 let accept;const started=new Promise(resolve=>accept=resolve);let closed;
 server.on('request',(_req,res)=>{closed=once(res,'close');accept();});server.listen(0,'127.0.0.1');await once(server,'listening');
 const bridge=createBridgeClient({baseUrl:`http://127.0.0.1:${server.address().port}`,timeoutMs:1500});const controller=new AbortController();
 const pending=bridge.metricCatalog('all',{signal:controller.signal});const caught=pending.catch(e=>e);
 try{await started;controller.abort();const error=await caught;assert.equal(error.details.code,'READ_CANCELLED');await closed;}
 finally{server.closeAllConnections();server.close();await once(server,'close');}
});
test('catalog cancellation also aborts a bounded response body and removes caller listener',async()=>{
 let entered;const incoming=new Promise(resolve=>entered=resolve);let closed=false;
 const server=createServer((_req,res)=>{res.writeHead(200,{'content-type':'application/json'});res.write('[');res.on('close',()=>closed=true);entered();});server.listen(0,'127.0.0.1');await once(server,'listening');
 const bridge=createBridgeClient({baseUrl:`http://127.0.0.1:${server.address().port}`});const abort=new AbortController();let added=0,removed=0;
 const add=abort.signal.addEventListener.bind(abort.signal),remove=abort.signal.removeEventListener.bind(abort.signal);
 abort.signal.addEventListener=(...args)=>{added++;return add(...args);};abort.signal.removeEventListener=(...args)=>{removed++;return remove(...args);};
 const pending=bridge.metricCatalog('all',{signal:abort.signal});const caught=pending.catch(e=>e);
 try{await incoming;await new Promise(resolve=>setTimeout(resolve,30));abort.abort();assert.equal((await caught).details.code,'READ_CANCELLED');assert.equal(added,removed);
 for(let i=0;!closed&&i<50;i++)await new Promise(resolve=>setTimeout(resolve,10));assert.equal(closed,true);
 }finally{server.closeAllConnections();server.close();await once(server,'close');}
});
