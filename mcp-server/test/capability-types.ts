import type { Capability } from "../src/contracts.js";
import { compareExposure } from "../src/capabilities.js";

// A supported entry serialized by the bridge includes a null reason.
const wire: Capability = { name: "cs2_budget", support: "supported", reason: null };
const reason: string | null | undefined = compareExposure([wire], []).capabilities[0].reason;
void reason;
