import type { Observation } from "../src/contracts.js";

const wire:Observation<string>={data:"sample",world:null,sampledAtUtc:"2026-10-16T00:00:00Z",
  startFrame:null,endFrame:null,freshness:"unavailable",warnings:["worldUnavailable"]};
const current:Observation<string>={...wire,world:{bootId:"boot",epoch:1},startFrame:9,endFrame:10,
  freshness:"current",warnings:["multiFrameReadNotAtomic"]};
const warming:Observation<string>={...wire,freshness:"warming"};
// @ts-expect-error Observation requires warnings even when the array is empty.
const missingWarnings:Observation<string>={data:"sample",world:null,sampledAtUtc:"now",startFrame:null,endFrame:null,freshness:"unavailable"};
// @ts-expect-error Unknown freshness spellings must not enter the shared wire contract.
const invalid:Observation<string>={...wire,freshness:"fresh"};
// @ts-expect-error The unpublished value alias was removed in favor of shared data.
const oldAlias=current.value;
void current;void warming;void missingWarnings;void invalid;void oldAlias;
