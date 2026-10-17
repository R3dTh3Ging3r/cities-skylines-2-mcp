import test from 'node:test';
import assert from 'node:assert/strict';
import {createServer} from 'node:http';
import {once} from 'node:events';
import {startOfflineMcp} from './fake-bridge.mjs';
import {readFileSync} from 'node:fs';
import {parseConstructionObservation} from '../dist/construction-contracts.js';
import {parseObservationResult} from '../dist/observation-contracts.js';
import {createBridgeClient} from '../dist/bridge-client.js';
import {constructionResult} from '../dist/construction.js';
import {readMessages} from '../dist/construction-read-diagnostics.js';
const wire=JSON.parse(readFileSync(process.env.RECOVERY_WIRE_DIR?`${process.env.RECOVERY_WIRE_DIR}/construction-recovery.json`:new URL('./fixtures/construction-recovery-wire.json',import.meta.url),'utf8'));
const world={bootId:'boot',epoch:1};
const input={world,entities:[{world,index:1,version:1}]};

test('actual MCP distinguishes known slot and byte refusals without echoing unknown bridge text',async()=>{
    let message='direct relationships exceed128 slots; select fewer roots or omit expansion';
    const server=createServer((_req,res)=>{res.statusCode=413;res.end(JSON.stringify({error:message,errorData:{code:'RESULT_TOO_LARGE',message,retryable:false,outcome:'notApplied'}}));});
    server.listen(0,'127.0.0.1');await once(server,'listening');let mcp;
    try {
        mcp=await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
        for(const [text,reason] of [
            [message,'relationshipSlotLimit'],
            ['inventory result exceeds256KiB; select fewer roots or omit expansion','inventoryByteLimit'],
            ['arbitrary private bridge text','unknownLimit'],
        ]) {
            message=text;
            const result=await mcp.client.callTool({name:'cs2_construction_inventory',arguments:input});
            assert.equal(result.isError,true);const error=JSON.parse(result.content[0].text).error;
            assert.equal(error.code,'RESULT_TOO_LARGE');assert.equal(error.reason,reason);
            assert.ok(!error.message.includes('arbitrary private'));
        }
    }finally{await mcp?.close();server.closeAllConnections();server.close();await once(server,'close');}
});

test('actual SDK advertises all required strict page fields',async()=>{
    const mcp=await startOfflineMcp('http://127.0.0.1:1');
    try {
        const tools=(await mcp.client.listTools()).tools;
        const schema=tools.find(t=>t.name==='cs2_construction_inventory').inputSchema;
        const page=schema.properties.relationshipPage;
        assert.deepEqual(new Set(page.required),new Set(['kind','offset','limit']));
        assert.equal(page.additionalProperties,false);assert.equal(page.properties.limit.maximum,32);
        assert.equal(page.properties.offset.maximum,2147483647);
        assert.deepEqual(new Set(schema.required),new Set(['world','entities']));
        assert.deepEqual(page.properties.kind.enum,['owner','attached','subObject','installedUpgrade','subNet','subArea']);
        const group=tools.find(t=>t.name==='cs2_observe_batch').inputSchema;
        const inventory=group.properties.items.items.anyOf.find(t=>t.properties.name.const==='construction.inventory');
        assert.deepEqual(inventory.properties.args.properties.relationshipPage,page);
    }finally{await mcp.close();}
});

test('actual native-producer page wire validates direct O7 terminal offsets and original root diagnostics',()=>{
    assert.deepEqual(parseConstructionObservation(wire.direct,wire.request),wire.direct);
    assert.deepEqual(parseObservationResult(wire.group,{items:[{id:'page',name:'construction.inventory',args:wire.request}]}),wire.group);
    const page=wire.direct.data.relationshipPage;
    assert.equal(page.observedCount,129);assert.equal(page.returnedCount,5);assert.equal(page.nextOffset,69);
    assert.deepEqual(page.links.map(l=>l.slot),[64,65,66,67,68]);assert.equal(page.additionalTargetReads,0);
    assert.equal(page.links[0].status,'referenceObserved');assert.deepEqual(page.links[0].target,page.links[3].target);
    assert.equal(page.links[1].reason,'nullReference');assert.equal(page.links[2].rawIndex,-3);
    assert.equal(wire.direct.data.geometryDiagnostics.evaluations,17);
    const terminal=parseConstructionObservation(wire.terminal,wire.terminalRequest).data.relationshipPage;
    assert.equal(terminal.returnedCount,0);assert.equal(terminal.nextOffset,null);assert.equal(terminal.sourceCoverage,'partial');
    for(const mode of ['absent','unavailable']){
        const p=parseConstructionObservation(wire[mode],wire[`${mode}Request`]).data.relationshipPage;
        assert.equal(p.sourceCoverage,mode);assert.equal(p.observedCount,null);assert.equal(p.endObservedCount,null);
        assert.equal(p.returnedCount,0);assert.equal(p.windowComplete,false);assert.equal(p.nextOffset,null);
    }
});

test('shared page parser rejects forged windows identities availability and completeness across direct and O7',()=>{
    const changes=[
        v=>delete v.data.relationshipPage,
        v=>v.data.relationshipPage.root.version++,v=>v.data.relationshipPage.kind='owner',
        v=>v.data.relationshipPage.offset++,v=>v.data.relationshipPage.limit++,
        v=>v.data.relationshipPage.endObservedCount++,v=>v.data.relationshipPage.endExclusive++,
        v=>v.data.relationshipPage.returnedCount--,v=>v.data.relationshipPage.visitedSlots--,
        v=>v.data.relationshipPage.nextOffset=129,v=>v.data.relationshipPage.sourceCoverage='complete',
        v=>v.data.relationshipPage.sources[0].coverageComplete=true,
        v=>v.data.relationshipPage.sources[2].observedCount++,v=>v.data.relationshipPage.sources.reverse(),
        v=>v.data.relationshipPage.links[0].slot=0,v=>v.data.relationshipPage.links[0].target.world.epoch++,
        v=>v.data.relationshipPage.links[0].status='available',v=>v.data.relationshipPage.links[1].reason=null,
        v=>v.data.relationshipPage.links[2].rawIndex=3,v=>v.data.relationshipPage.links[0].targetExists=true,
        v=>v.data.relationshipPage.crossPageConsistency='stable',v=>v.data.relationshipPage.additionalTargetReads=1,
        v=>v.data.relationshipPage.observedCount=Infinity,
        v=>v.warnings=v.warnings.filter(x=>x!=='relationshipPagesNotAtomicSnapshot'),
    ];
    for(const change of changes){
        const value=structuredClone(wire.direct);change(value);assert.throws(()=>parseConstructionObservation(value,wire.request));
        const group=structuredClone(wire.group);group.results[0].observation=value;
        assert.throws(()=>parseObservationResult(group,{items:[{id:'page',name:'construction.inventory',args:wire.request}]}));
    }
    const omitted={...wire.request};delete omitted.relationshipPage;
    assert.throws(()=>parseConstructionObservation(wire.direct,omitted));
    const terminal=structuredClone(wire.terminal);terminal.data.relationshipPage.sourceCoverage='complete';
    assert.throws(()=>parseConstructionObservation(terminal,wire.terminalRequest));
});

test('actual SDK page validation rejects huge nested unknown missing mixed and ambiguous requests before HTTP',async()=>{
    let calls=0;const server=createServer((_req,res)=>{calls++;res.end('{}');});
    server.listen(0,'127.0.0.1');await once(server,'listening');let mcp;
    try{
        mcp=await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);const huge='界'.repeat(100000),p=wire.request.relationshipPage;
        const bad=[{}, {[huge]:1},{...wire.request,[huge]:1},{...wire.request,world:{...world,[huge]:1}},
            {...wire.request,entities:[{...input.entities[0],[huge]:1}]},{...wire.request,entities:[{[huge]:1}]},
            {...wire.request,relationshipPage:{...p,[huge]:1}},{...wire.request,relationshipPage:{[huge]:1}},
            {...wire.request,relationshipPage:{}},{...wire.request,relationshipPage:null},
            ...['kind','offset','limit'].map(key=>{const page={...p};delete page[key];return {...wire.request,relationshipPage:page};}),
            ...[-1,1.5,2147483648,null].map(offset=>({...wire.request,relationshipPage:{...p,offset}})),
            ...[0,33,1.5,null].map(limit=>({...wire.request,relationshipPage:{...p,limit}})),
            {...wire.request,relationshipPage:{...p,kind:'recursive'}},{...wire.request,relationships:'directDepth1'},
            {...wire.request,entities:[input.entities[0],{...input.entities[0],index:2}]},
            {...wire.request,entities:[{...input.entities[0],world:{...world,epoch:2}}]},
        ];
        for(const args of bad)for(const name of ['cs2_construction_inventory','cs2_observe_batch']){
            const response=await mcp.client.callTool({name,arguments:name==='cs2_construction_inventory'?args:{items:[{id:'p',name:'construction.inventory',args}]}});
            assert.equal(response.isError,true);assert.ok(Buffer.byteLength(JSON.stringify(response))<=262144);assert.equal(calls,0);
        }
    }finally{await mcp?.close();server.closeAllConnections();server.close();await once(server,'close');}
});

test('actual MCP page GET and O7 POST lead to an explicit chosen-target geometry read without auto expansion',async()=>{
    const calls=[];const server=createServer(async(req,res)=>{
        let body='';for await(const chunk of req)body+=chunk;
        const url=new URL(req.url,'http://test');calls.push({method:req.method,url,body:body?JSON.parse(body):null});
        res.end(JSON.stringify(req.method==='POST'?wire.group:url.searchParams.has('relationshipKind')?wire.direct:wire.targets));
    });
    server.listen(0,'127.0.0.1');await once(server,'listening');let mcp;
    try{
        mcp=await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
        const first=await mcp.client.callTool({name:'cs2_construction_inventory',arguments:wire.request});assert.equal(first.isError,undefined);
        const observed=JSON.parse(first.content[0].text);assert.deepEqual(observed,wire.direct);assert.equal(calls.length,1);
        const group=await mcp.client.callTool({name:'cs2_observe_batch',arguments:{items:[{id:'page',name:'construction.inventory',args:wire.request}]}});
        assert.equal(group.isError,undefined);assert.deepEqual(JSON.parse(group.content[0].text),wire.group);assert.equal(calls.length,2);
        const chosen=[...new Map(observed.data.relationshipPage.links.filter(l=>l.target).map(l=>[l.target.index,l.target])).values()];
        const targets=await mcp.client.callTool({name:'cs2_construction_inventory',arguments:{world:wire.request.world,entities:chosen}});
        assert.equal(targets.isError,undefined);assert.deepEqual(JSON.parse(targets.content[0].text),wire.targets);assert.equal(calls.length,3);
        assert.equal(calls[0].url.searchParams.get('relationshipOffset'),'64');assert.equal(calls[0].url.searchParams.get('relationshipLimit'),'5');
        assert.deepEqual(calls[1].body.items[0].args.relationshipPage,wire.request.relationshipPage);
        assert.equal(calls[2].url.searchParams.get('entities'),'2:1,99:1');assert.equal(calls[2].url.searchParams.has('relationshipKind'),false);
        assert.equal(wire.targets.data.entities[1].unavailableReason,'missingDeletedOrRecycled');
    }finally{await mcp?.close();server.closeAllConnections();server.close();await once(server,'close');}
});

test('bounded HTTP and escaped MCP pressure have independent finite causes and no silent partial success',async()=>{
    let mode='transport';const server=createServer((_req,res)=>res.end(mode==='transport'?' '.repeat(262145):JSON.stringify(wire.direct)));
    server.listen(0,'127.0.0.1');await once(server,'listening');let mcp;
    try{
        mcp=await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
        const result=await mcp.client.callTool({name:'cs2_construction_inventory',arguments:wire.request});
        const error=JSON.parse(result.content[0].text).error;assert.equal(error.code,'RESULT_TOO_LARGE');assert.equal(error.reason,'transportResponseByteLimit');
        assert.ok(Buffer.byteLength(JSON.stringify(result))<262144);
        const wrapped=constructionResult({hostile:'\u0001'.repeat(50000)});assert.equal(wrapped.isError,true);
        assert.equal(JSON.parse(wrapped.content[0].text).error.reason,'mcpResultByteLimit');
        mode='good';const client=createBridgeClient({baseUrl:`http://127.0.0.1:${server.address().port}`});assert.deepEqual(await client.constructionInventory(wire.request),wire.direct);
    }finally{await mcp?.close();server.closeAllConnections();server.close();await once(server,'close');}
});

test('actual O7 SDK bounds outer inventory and mixed-group refusals without leaking arbitrary diagnostics',async()=>{
    const privateText='private-bridge-body-'.repeat(5000);
    const exact={error:privateText,errorData:{code:'RESULT_TOO_LARGE',message:privateText,retryable:false,outcome:'notApplied'}};
    assert.equal(Buffer.byteLength(JSON.stringify(exact)),200106);
    const bodies=[
        [exact,'RESULT_TOO_LARGE','unknownLimit'],
        [{error:privateText},'PROTOCOL_ERROR','unknownCause'],
        [{errorData:{code:privateText,message:privateText,nativeErrors:[privateText],operationId:privateText,retryable:true,outcome:'unknown'}},'PROTOCOL_ERROR','unknownCause'],
        [{error:privateText,errorData:{code:'RESULT_TOO_LARGE',message:privateText,nativeErrors:[privateText],operationId:privateText,retryable:false,outcome:'notApplied'}},'RESULT_TOO_LARGE','unknownLimit'],
        [{errorData:{code:'RESULT_TOO_LARGE',message:'direct relationships exceed128 slots; select fewer roots or omit expansion',retryable:false,outcome:'notApplied'}},'RESULT_TOO_LARGE','relationshipSlotLimit'],
        [{errorData:{code:'RESULT_TOO_LARGE',message:'inventory result exceeds256KiB; select fewer roots or omit expansion',retryable:false,outcome:'notApplied'}},'RESULT_TOO_LARGE','inventoryByteLimit'],
        [{errorData:{code:'RESULT_TOO_LARGE',message:readMessages.observationItemByteLimit,nativeErrors:[privateText],operationId:privateText,retryable:false,outcome:'notApplied'}},'RESULT_TOO_LARGE','observationItemByteLimit'],
    ];
    let body,calls=0;
    const server=createServer((_req,res)=>{calls++;res.statusCode=413;res.end(JSON.stringify(body));});
    server.listen(0,'127.0.0.1');await once(server,'listening');let mcp;
    try{
        mcp=await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
        const exactPage={...input,relationshipPage:{kind:'subObject',offset:0,limit:1}};
        for(const args of [exactPage,input,wire.request])for(const mixed of [false,true])for(const [payload,code,reason] of bodies){
            body=payload;const before=calls;
            const items=[{id:'p',name:'construction.inventory',args},...(mixed?[{id:'state',name:'state',args:{}}]:[])];
            const result=await mcp.client.callTool({name:'cs2_observe_batch',arguments:{items}});
            const encoded=JSON.stringify(result);
            assert.equal(result.isError,true);assert.ok(Buffer.byteLength(encoded)<=262144);
            assert.equal(encoded.includes('private-bridge-body-'),false);assert.equal(encoded.includes('nativeErrors'),false);assert.equal(encoded.includes('operationId'),false);
            const failure=JSON.parse(result.content[0].text).error;
            assert.equal(failure.code,code);assert.equal(failure.reason,reason);assert.equal(calls,before+1,'outer refusal retried');
        }
        body={errorData:{code:'CONTROLLED_CONTEXT_ERROR',message:'controlled context diagnostic',nativeErrors:['controlled'],operationId:'context-id',retryable:false,outcome:'notApplied'}};
        const context=await mcp.client.callTool({name:'cs2_observe_batch',arguments:{items:[{id:'state',name:'state',args:{}}]}});
        assert.equal(context.structuredContent.errorData.code,'CONTROLLED_CONTEXT_ERROR');
        assert.deepEqual(context.structuredContent.errorData.nativeErrors,['controlled']);assert.equal(context.structuredContent.errorData.operationId,'context-id');
    }finally{await mcp?.close();server.closeAllConnections();server.close();await once(server,'close');}
});

test('O7 preserves ordered non-inventory data while bounding inventory item errors and large64-item success groups',async()=>{
    const base=structuredClone(wire.group),source=wire.group.results[0].observation;
    const itemError={code:'RESULT_TOO_LARGE',message:'private-item-body',retryable:false,outcome:'notApplied',nativeErrors:['private-item-body'],operationId:'private-item-body'.repeat(20000)};
    const contextError={code:'CONTROLLED_CONTEXT_ERROR',message:'controlled context diagnostic',retryable:false,outcome:'notApplied',nativeErrors:['controlled'],operationId:'context-id'};
    let mode='item',calls=0;
    const server=createServer(async(req,res)=>{
        let text='';for await(const chunk of req)text+=chunk;const request=JSON.parse(text);calls++;
        const value=structuredClone(base);
        value.results=request.items.map((item,index)=>mode!=='success'&&index===0?{id:item.id,error:mode==='known'?{...itemError,message:readMessages.relationshipSlotLimit}:itemError}:
            mode!=='success'&&index===63?{id:item.id,error:contextError}:
            item.name==='construction.inventory'?{id:item.id,observation:structuredClone(source)}:
            {id:item.id,observation:{...structuredClone(source),data:{payload:'x'.repeat(6000)}}});
        res.end(JSON.stringify(value));
    });
    server.listen(0,'127.0.0.1');await once(server,'listening');let mcp;
    try{
        mcp=await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
        const items=[{id:'page',name:'construction.inventory',args:wire.request},...Array.from({length:63},(_,i)=>({id:`state${i}`,name:'state',args:{}}))];
        const failure=await mcp.client.callTool({name:'cs2_observe_batch',arguments:{items}});
        assert.equal(failure.isError,undefined);const encoded=JSON.stringify(failure);
        assert.equal(encoded.includes('private-item-body'),false);
        const result=JSON.parse(failure.content[0].text);assert.deepEqual(result.results.map(row=>row.id),items.map(item=>item.id));
        assert.equal(result.results[0].error.code,'RESULT_TOO_LARGE');assert.match(result.results[0].error.message,/unknownLimit/);
        assert.equal(result.results[1].observation.data.payload.length,6000);assert.equal(calls,1);
        assert.deepEqual(result.results[63].error,contextError);
        mode='known';const known=await mcp.client.callTool({name:'cs2_observe_batch',arguments:{items}});
        const knownData=JSON.parse(known.content[0].text);assert.equal(knownData.results[0].error.message,readMessages.relationshipSlotLimit);
        assert.equal(knownData.results[0].error.code,'RESULT_TOO_LARGE');assert.equal(JSON.stringify(known).includes('private-item-body'),false);assert.equal(calls,2);
        mode='success';const success=await mcp.client.callTool({name:'cs2_observe_batch',arguments:{items}});
        assert.equal(success.isError,undefined);assert.ok(Buffer.byteLength(JSON.stringify(success))>262144);
        const data=JSON.parse(success.content[0].text);assert.equal(data.results.length,64);
        assert.deepEqual(data.results[0].observation,source);assert.equal(data.results[63].observation.data.payload.length,6000);assert.equal(calls,3);
    }finally{await mcp?.close();server.closeAllConnections();server.close();await once(server,'close');}
});
