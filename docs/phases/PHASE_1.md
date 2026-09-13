# Phase 1: non-enforcing developer foundation

Status: **complete for the non-enforcing developer foundation**. Validated on 2026-09-12 on Windows 10 Home 22H2 x64; real blocking remains unimplemented. This is the first implementation slice of the [phased plan](../IMPLEMENTATION_PLAN.md). It creates useful, reviewable code without changing the contributor's Windows protection settings.

## Result to demonstrate

A contributor follows the [README](../../README.md), builds the .NET solution, runs the unit tests, reads a Windows capability report and simulates an exact-hash rule against a harmless synthetic file. The result clearly states that no application, download or connection has been blocked.

The capability report describes observations, not proof that App Control or WFP works on that machine. The simulator demonstrates catalog parsing and identity matching, not real remote-access detection. A file that does not match a synthetic rule is not thereby trusted or safe.

## Scope and implementation order

Complete each item as a small reviewable change. Check a box only when the corresponding code and evidence exist; an intended behavior alone is not completion.

### 1. Reproducible solution

- [x] Pin the exact .NET 10 SDK in `global.json` and document verified acquisition/bootstrap instructions.
- [x] Create the solution and `RefundScamBlocker.Core`, `RefundScamBlocker.Diagnostics` and `RefundScamBlocker.Cli` projects, plus `Core.Tests` and `Cli.Tests` projects.
- [x] Add shared build settings, nullable checking, analyzers, exact dependency versions and package lockfiles.
- [x] Exclude local SDK, build outputs and temporary artifacts from source control.

**Review check:** Core has no Windows mutation dependency. Diagnostics owns read-only machine observations. The CLI composes the two without acquiring elevation or installing a background component.

### 2. Strict synthetic catalog and pure evaluator

- [x] Define a small, bounded synthetic catalog format with versioning, explicit rule identities, fixed simulated-deny semantics and exact artifact SHA-256.
- [x] Reject malformed input, unknown/unsupported fields or actions, invalid hashes, duplicate/conflicting identifiers and out-of-bounds inputs as applicable to the format.
- [x] Make validation and rule results immutable and deterministic.
- [x] Add a harmless synthetic file and matching catalog clearly marked as fixtures, with no actual remote-support coverage claim.
- [x] Evaluate by exact content hash, independent of file name; report match, no match and invalid input distinctly.
- [x] Keep artifact hashes distinct from App Control image/Authenticode hashes. Do not emit deployable policies or pretend an arbitrary file hash is a compiled App Control rule.

**Review check:** filenames alone never produce a match, and a negative match cannot produce a claim that software is safe. Future catalog provenance and identity types belong to Phase 3 and cannot be implied by the Phase 1 schema.

The initial schema uses `schemaVersion: 1`, `catalogVersion` and `rules`; each rule has `id`, `product`, `artifactSha256` and `reason`. The initial sample is `examples/synthetic-catalog.json`, matching `tests/fixtures/harmless-demo.txt`. The only decision results are `WouldBlock` and `NoMatchingRule`; both are simulations. Additional production actions/identities are intentionally deferred.

### 3. Read-only diagnostics and CLI

- [x] Implement `capabilities [--json]` with bounded read-only observations and explicit unavailable/unsupported states.
- [x] Make capability output say that enforcement has not been validated or activated; finding a tool/API is not an enforcement test.
- [x] Implement `simulate --catalog <json> --file <file> [--json]` with a clear non-enforcing result.
- [x] Define helpful usage/error output and nonzero failure codes for invalid commands or input; keep structured output machine-readable.
- [x] Handle missing/inaccessible files and unsupported diagnostic environments without reporting fabricated success.

**Review check:** no service registration, startup task, policy deployment, firewall/WFP modification, browser policy change, credential enrollment, remote-access artifact download or active-session termination. Reading a local input to calculate a hash is expected; modifying that input is not.

### 4. Contributor scripts and documentation

- [x] Add `bootstrap-dotnet.ps1` for the pinned developer SDK, with verified artifact integrity and a repository-local destination.
- [x] Add `verify-environment.ps1` for read-only developer prerequisites. It reports missing tools without modifying protection settings.
- [x] Add `build.ps1` with locked restore and a deterministic native build. `Browser` and `All` requests fail explicitly until those components exist.
- [x] Add `test.ps1` with ordinary unit tests by default. Integration selection fails explicitly until a separate suite with explicit target selection and recovery requirements exists.
- [x] Add `run.ps1` for the CLI and document exact build/test/demo commands in the README.
- [x] Mark the repository as a development foundation; state that no consumer installer or protection is available. Packaging stays in Phase 8.
- [x] Keep contribution guidance and phase links consistent with the actual files and prerequisites.

**Review check:** no placeholder packaging or test command reports a feature as passed. Normal outputs and the local developer SDK are allowed development artifacts; the machine's protection configuration remains unchanged.

### 5. Automated checks and manual smoke verification

- [x] Unit-test positive exact-hash matching and negative controls using synthetic fixtures.
- [x] Unit-test malformed/unsupported/oversized catalog rejection and identity validation.
- [x] Test missing or inaccessible inputs and CLI usage failures where reproducible.
- [x] Test human and JSON output so both retain the non-enforcing distinction and unavailable/unsupported states.
- [x] Run the pinned restore/build and unit suite; record the actual outcome below.
- [x] Run `capabilities` and `simulate` using the README instructions; record their actual outcomes below.
- [x] Verify that a renamed copy has the same content-identity result and changed content no longer matches the exact-hash fixture.
- [x] Inspect the final changes for accidental host-mutation code, secrets, proprietary fixtures, generated build outputs and untruthful capability claims.

Avoid implementation-mirroring tests: prioritize malformed input boundaries, independent expected hashes, stable CLI contracts and negative controls that can catch incorrect behavior.

## Validation record

The following records actual local runs. A skipped command is recorded as not run, with the reason. Automated Windows integration, installed service behavior and real prevention tests belong to later phases and cannot count as passes here.

| Check | Status | Evidence / notes |
| --- | --- | --- |
| Pinned SDK acquisition/version | Passed | Official Windows x64 SDK 10.0.401 archive verified against the pinned SHA-512 before extraction into ignored `.tools/dotnet`; version check passed. |
| Locked restore and native build | Passed | `scripts/build.ps1`: Release build, locked restore, zero warnings and zero errors on Windows 10. |
| Unit test suite | Passed | `scripts/test.ps1`: 66 passed (52 Core, 14 CLI), zero failed, zero skipped. TRX files are local ignored artifacts and may contain local machine/user metadata. |
| Read-only capabilities smoke run | Passed | CLI reports Windows 10 Home (`Core`) 22H2, build 19045.6466, x64; CiTool Missing, PowerShell 5.1 Present, elevation Unknown under the sandbox, enforcement NotImplemented. Ran through PowerShell 7.6.5 and Windows PowerShell 5.1.19041.6456. No enforcement inferred. |
| Synthetic matching demo | Passed | README JSON command returned WouldBlock, SimulationOnly and protectionActive=false for the committed harmless fixture. |
| Renamed-copy and changed-content controls | Passed | Core/CLI tests verify rename stability and changed-byte NoMatchingRule; an additional README-file smoke run produced NoMatchingRule with the explicit non-enforcing explanation. |
| Unsupported component/integration requests | Passed rejection checks | Browser and All builds, and Integration tests, fail explicitly as unimplemented. Their actual features/suites were not run or passed. |
| Documentation and final diff review | Passed | Source review, documentation/link checks and whitespace checks; developer output, SDK caches and TRX files excluded from source control. No Windows protection-changing implementation. |

The GitHub workflow is authored with a pinned checkout action and no signing credentials. Its local build/test/smoke/format steps are verified; it has not run on GitHub in this task. Laptop and real policy/network/recovery tests are not run.

## Exit gate and next task

Phase 1 is complete when its deliverables exist and the actual build/test/demo results above support them. It does not satisfy any product prevention test T01-T26 or prove Windows edition compatibility for enforcement.

The next task is **Phase 2: read-only inventory of the owner's Windows 10 desktop and laptop, followed by recovery preparation and one narrow deny/apply/remove proof**. Record each machine's exact edition/build and runtime/OS servicing state. Before policy activation, identify the designated target, local console access, account separation, restorable backup or snapshot and recovery method. Disposable VMs are recommended for early enforcement and interruption tests, but not required for the read-only investigation or all testing. Use a Windows 10-specific deployment/removal path; CiTool is not a prerequisite. Windows 11 becomes an additional target only after its own tests. Phase 1 still makes no live protection changes.

Do not implement password-protected uninstall, an auto-starting service, persistent network filters or NSIS installation merely to make Phase 1 look like a finished product. Those features have explicit later phases with recovery and validation gates.
