import test from "node:test";
import assert from "node:assert/strict";
import {readFileSync} from "node:fs";
import {join} from "node:path";
import {createBridgeClient,BridgeClientError} from "../dist/bridge-client.js";
import {metricReadRequestSchema,metricCatalogSchema,metricBatchObservationSchema,parseMetricBatchObservation,parseMetricObservation,registerDiagnosticMetricTools,metricQuery} from "../dist/diagnostic-metrics.js";
import {parseObservationResult} from "../dist/observation-contracts.js";
const checkedIn=JSON.parse(readFileSync(new URL("./fixtures/diagnostic-metrics-wire.json",import.meta.url),"utf8"));
// Synthetic native-boundary data serialized by the actual C# producer/ObservationWire.
// M4_WIRE_DIR selects freshly emitted managed-test results for the explicit integration gate.
const fixture=name=>JSON.parse(process.env.M4_WIRE_DIR?readFileSync(join(process.env.M4_WIRE_DIR,name+".json"),"utf8"):JSON.stringify(checkedIn[name]));
const cityRequest=()=>fixture("city-request"),lineRequest=()=>fixture("line-request");
const clientWith=fetchImpl=>createBridgeClient({baseUrl:"http://127.0.0.1:1",fetchImpl});
const errorCode=code=>error=>error instanceof BridgeClientError&&error.details.code===code;

test("typed metric client consumes public metric reads",()=>{const client=clientWith(()=>{throw new Error("unused");});assert.equal(typeof client.readMetrics,"function");assert.equal(typeof client.metricCatalog,"function");});
test("actual C# city and selected-line serialization passes final TS request/result parsers",()=>{
  for(const name of ["city","line"]){
    const request=metricReadRequestSchema.parse(fixture(name+"-request"));const observed=parseMetricBatchObservation(fixture(name),request);
    assert.deepEqual(observed.data.values,parseMetricObservation(fixture(name+"-direct"),request).data);
    assert.equal(observed.data.scope.line===null,name==="city");
    const batch={world:observed.world,startFrame:observed.startFrame,endFrame:observed.endFrame,worldChanged:false,results:[{id:"metrics",observation:observed}]};
    assert.equal(parseObservationResult(batch,{expectedWorld:request.world,items:[{id:"metrics",name:"metrics.read",args:request}]}).results.length,1);
  }
  assert.equal(metricCatalogSchema.parse(fixture("catalog")).length,16);
});
test("actual state-fallback failed-source and null-clock evidence survive final TS parser",()=>{
  for(const name of ["fallback","failed-source","invalid-clock"]){
    const result=parseMetricBatchObservation(fixture(name),cityRequest());
    if(name==="invalid-clock")assert.equal(result.data.clock,null);
    else assert.equal(result.data.clock.sourceObservationId,"state");
    if(name==="failed-source")assert.equal(result.data.values["city.money"].value,null);
  }
  const wrong=fixture("fallback");wrong.data.clock.sourceObservationId="city.overview";assert.throws(()=>parseMetricBatchObservation(wrong,cityRequest()));
  const failedOrigin=fixture("failed-source");failedOrigin.data.clock.sourceObservationId="city.overview";assert.throws(()=>parseMetricBatchObservation(failedOrigin,cityRequest()));
});
test("metric strict queries preserve full world refs and reject unknown scopes parameters duplicates before fetch",async()=>{
  let calls=0;const client=clientWith(async()=>{calls++;return new Response("{}");});
  for(const invalid of [
    {...cityRequest(),scope:{kind:"city",area:{}}},
    {...cityRequest(),queries:[{key:"city.money",parameters:{parameter:1}}]},
    {...cityRequest(),queries:[{key:"city.money",parameters:{}},{key:"city.money",parameters:{}}]},
    {...cityRequest(),queries:[{key:"transit.segmentCount",parameters:{}}]},
    {...lineRequest(),scope:{kind:"selectedLine",line:{...lineRequest().scope.line,world:{bootId:"stale",epoch:1}}}},
    {...cityRequest(),queries:[{key:"unknown",parameters:{}}]},
  ])await assert.rejects(()=>client.readMetrics(invalid),errorCode("INVALID_ARGUMENT"));
  assert.equal(calls,0);assert.deepEqual(JSON.parse(metricQuery(lineRequest()).get("query")),lineRequest());
});
test("actual tools use typed GET dictionary/catalog and keys translate to empty parameters",async()=>{
  const calls=[];const client=clientWith(async url=>{
    const address=new URL(url);calls.push(address);
    if(address.pathname==="/v2/metrics/catalog")return Response.json(fixture("catalog"));
    const query=JSON.parse(address.searchParams.get("query"));assert.deepEqual(query,cityRequest());return Response.json(fixture("city-direct"));
  });
  const tools=new Map();registerDiagnosticMetricTools((name,metadata,handler)=>tools.set(name,{metadata,handler}),client);
  assert.deepEqual([...tools.keys()],["cs2_metric_catalog","cs2_observe"]);
  const catalog=tools.get("cs2_metric_catalog");const discovered=await catalog.handler(catalog.metadata.inputSchema.parse({}));assert.equal(discovered.isError,undefined);
  const observe=tools.get("cs2_observe");const input={world:cityRequest().world,scope:{kind:"city"},keys:["city.money","city.population"]};
  const result=await observe.handler(observe.metadata.inputSchema.parse(input));assert.equal(result.isError,undefined);assert.equal(calls.length,2);
  assert.throws(()=>observe.metadata.inputSchema.parse({...input,queries:cityRequest().queries}));
});
test("actual BridgeClient.observe consumes fixed metric entry and caller abort reaches fetch",async()=>{
  const request={expectedWorld:cityRequest().world,items:[{id:"m",name:"metrics.read",args:cityRequest()}]};
  const client=clientWith(async(_url,options)=>{
    assert.deepEqual(JSON.parse(options.body),request);const observation=fixture("city");
    return Response.json({world:observation.world,startFrame:observation.startFrame,endFrame:observation.endFrame,worldChanged:false,results:[{id:"m",observation}]});
  });assert.equal((await client.observe(request)).results[0].observation.data.definitionRevision,"diagnosticMetrics.v1");
  const controller=new AbortController();let fetchSignal;
  const pending=clientWith(async(_url,options)=>{fetchSignal=options.signal;return new Promise((_resolve,reject)=>options.signal.addEventListener("abort",()=>reject(new DOMException("aborted","AbortError")),{once:true}));}).observe(request,{signal:controller.signal});
  controller.abort();await assert.rejects(pending,errorCode("READ_CANCELLED"));assert.equal(fetchSignal.aborted,true);
});
test("metric metadata malformed values members and clock provenance reject",()=>{
  for(const mutate of [
    row=>{row.world.bootId="stale";},row=>{row.data.values["city.money"].value=Infinity;},
    row=>{row.data.values["city.population"].support="unavailable";},row=>{row.data.clock.sourceObservationId="fake";},
    row=>{row.data.sources[0].endFrame=row.endFrame+1;},row=>{row.data.members=[lineRequest().scope.line];},
  ]){const value=fixture("city");mutate(value);assert.throws(()=>parseMetricBatchObservation(value,cityRequest()));}
  const value=fixture("line");value.data.perMember[0].member.version++;assert.throws(()=>parseMetricBatchObservation(value,lineRequest()));
  const nullValue=fixture("failed-source");assert.equal(metricBatchObservationSchema.parse(nullValue).data.values["city.money"].value,null);
});
test("metric value unit and source meanings cannot be silently relabeled",()=>{
  const bad=fixture("city-direct");bad.data["city.money"].unit="kg";assert.throws(()=>parseMetricObservation(bad,cityRequest()));
});
test("all HTTP statuses obey metric body UTF8 JSON and byte caps for both client methods",async()=>{
  for(const invoke of [client=>client.readMetrics(cityRequest()),client=>client.metricCatalog()]){
    for(const response of [()=>new Response("x".repeat(262145),{status:500}),()=>new Response(new Uint8Array([255]),{status:500}),()=>new Response("not json",{status:200})]){
      await assert.rejects(()=>invoke(clientWith(async()=>response())),errorCode("PROTOCOL_ERROR"));
    }
    let calls=0;await assert.rejects(()=>invoke(createBridgeClient({baseUrl:"http://example.com",fetchImpl:async()=>{calls++;return Response.json({});}})),errorCode("INVALID_ARGUMENT"));assert.equal(calls,0);
  }
});

test("actual MCP SDK registers strict metric tools and refuses unknown args before HTTP",async()=>{
  const {createServer}=await import("node:http"),{once}=await import("node:events"),{startOfflineMcp}=await import("./fake-bridge.mjs");
  const calls=[];const server=createServer((req,res)=>{calls.push(req.url);res.end(JSON.stringify(req.url.startsWith("/v2/metrics/catalog")?fixture("catalog"):fixture("city-direct")));});
  server.listen(0,"127.0.0.1");await once(server,"listening");let mcp;
  try{
    mcp=await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);const {tools}=await mcp.client.listTools();
    assert.ok(tools.some(t=>t.name==="cs2_metric_catalog"));assert.ok(tools.some(t=>t.name==="cs2_observe"));
    assert.equal((await mcp.client.callTool({name:"cs2_metric_catalog",arguments:{}})).isError,undefined);
    const valid={world:cityRequest().world,scope:{kind:"city"},keys:["city.money","city.population"]};
    const observed=await mcp.client.callTool({name:"cs2_observe",arguments:valid});assert.equal(observed.isError,undefined);assert.deepEqual(JSON.parse(observed.content[0].text),fixture("city-direct"));
    assert.deepEqual(JSON.parse(new URL(calls[1],"http://test").searchParams.get("query")),cityRequest());
    for(const args of [{...valid,unknown:true},{...valid,scope:{kind:"city",area:{}}},{...valid,world:{...valid.world,unknown:1}},{...valid,queries:cityRequest().queries},{...valid,keys:undefined,queries:[{key:"city.money",parameters:{unknown:1}}]}]){
      assert.equal((await mcp.client.callTool({name:"cs2_observe",arguments:args})).isError,true);
    }
    assert.equal((await mcp.client.callTool({name:"cs2_metric_catalog",arguments:{family:"all",unknown:true}})).isError,true);assert.equal(calls.length,2);
  }finally{await mcp?.close();server.closeAllConnections();server.close();await once(server,"close");}
});
test("per-member metric maps accept reordered keys while differing facts refuse",()=>{
  const row=fixture("line");row.data.perMember[0].metrics=Object.fromEntries(Object.entries(row.data.perMember[0].metrics).reverse());assert.doesNotThrow(()=>parseMetricBatchObservation(row,lineRequest()));
  row.data.perMember[0].metrics["transit.segmentCount"].value=4;assert.throws(()=>parseMetricBatchObservation(row,lineRequest()));
});
