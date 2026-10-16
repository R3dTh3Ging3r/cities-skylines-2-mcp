# Bounded geometry source acceptance — October 16

Independent Sol 6.1/high review accepted specification compliance and code
quality/security for `88d1965..f80e6cf78ea8a76e4e12e251f70a91d8dc268383`.
Root accepts that bounded verdict; no Important findings remain. The initial
implementation is `6a4e8441edeaa635632c4a2523bb59c262de06bb`. Later handoff-only
commits contain the same reviewed source. This is partial C1, not full C1 or
native acceptance.

`cs2_construction_inventory` reads 1–32 explicit same-world EntityRefs through
the game-thread queue. It returns finite network cubics and versioned endpoints,
owner/prefab metadata, and root-building oriented bounds. It rejects stale worlds,
invalid references and unsupported query options before entity access. Whole
facility/platform/approach/connector envelopes, spatial scans and pagination are
not implemented. Unknown fields remain null with reasons. Existing curve readback
is preserved, and no new writer is enabled.

Two Important issues were fixed before acceptance: the observation now follows
the shared data/world/sample/frame/freshness/warnings contract, and the actual MCP
registration rejects unknown filters instead of allowing the SDK to strip them.
Production-source and actual stdio regressions cover both fixes. The reviewer
also checked bounded native access, retained field provenance, root-bound math,
world validation, real HTTP routing/parser seams and truthful capability labels.

## Verification evidence

- Initial implementation: 36 portable Node tests and 69 exact-linked BCL tests
  passed across the selected suites.
- Final correction: 11 construction BCL checks and 7 construction/capability
  Node checks passed; TypeScript and canonical net472 core builds passed with
  zero build warnings/errors.
- Independent reviewer: 11 BCL and 3 construction Node checks passed; final
  actual stdio reproduction rejected both unknown family and bbox options with
  zero HTTP requests. Diff whitespace checks passed.

These are initial and targeted runs, not a fresh full-suite total. BCL execution
used a temporary net8 harness linking exact production/test sources; it does not
establish net472 execution, native ECS behavior or game serialization. The prior
full Node run had 15 PowerShell-unavailable failures and two retained TODOs.
No dependencies were reinstalled and unavailable suites were not repeated.

Windows/Framework execution, Node18 compatibility, PowerShell fixtures, actual
game serializer, native compilation and geometry/lifecycle parity remain open.
Full C1, G1–G4 and CON-01 remain incomplete/notRun. The earlier d21a563 curve smoke
pass does not certify this new candidate. Follow the [isolated PC build handoff](PC-BUILD-2026-10-16.md)
at the exact published candidate SHA with SkipDeploy=true. No installation,
gameplay, save operation or merge is part of this source acceptance.
