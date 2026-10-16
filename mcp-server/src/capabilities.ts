import type { Capability, CapabilityManifest, ExposureReport } from "./contracts.js";

/** Client visibility is supplied evidence, independent of implementation/support. */
export function compareExposure(server: Capability[], registeredTools: string[], clientTools?: string[]): ExposureReport {
  const registered = new Set(registeredTools);
  const exposed = clientTools === undefined ? undefined : new Set(clientTools);
  return {
    registeredTools: [...registered],
    clientTools: exposed === undefined ? null : [...exposed],
    capabilities: server.map((capability) => ({
      ...capability,
      registered: registered.has(capability.name),
      clientExposure: exposed === undefined ? "unknown" : exposed.has(capability.name) ? "exposed" : "notExposed",
    })),
  };
}

export function withMcpExposure(manifest: CapabilityManifest, mcpVersion: string,
  registeredTools: string[], clientTools?: string[], clientOnlyTools: readonly string[] = []) {
  return {
    ...manifest,
    identity: { ...manifest.identity, mcpVersion },
    exposure: compareExposure(manifest.capabilities, registeredTools, clientTools),
    // These are managed MCP operations, not native bridge endpoint descriptors.
    clientOnlyCapabilities: compareExposure(clientOnlyTools.map(name => ({name,
      support: "supported", reason: "boundedProcessLocalHistory"})), registeredTools, clientTools).capabilities,
  };
}
