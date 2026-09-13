# RefundScamBlocker: implementation and validation plan

Status: **planning only; every implementation milestone below is pending**. Prepared 2026-09-12. No application, installer, extension, build pipeline or prevention test currently exists. This plan implements the scoped behavior and security boundaries in [ARCHITECTURE.md](ARCHITECTURE.md); it does not promise universal remote-access detection.

## 1. Delivery strategy

Prove enforcement and recovery on real Windows Home and Pro installations before investing in a polished installer. A process-watcher demo is not a security MVP. The first public version requires functioning pre-execution blocking, persistent network enforcement, owner authentication, recovery, updates and honest browser coverage reporting.

Use small reviewable changes, with security-critical code/policies independently reviewed. Service/Windows enforcement, browser integration, and installer/UI can proceed in parallel after agreeing on the Core data model and IPC contract. Integrate them in VM tests throughout development. Do not replace validation gates with an arbitrary shipping date.

| Milestone | Concrete deliverables | Depends on | Exit gate |
| --- | --- | --- | --- |
| M0: feasibility and recovery | Recorded Windows capability matrix, minimal lab enforcement probes, working owned-policy removal, browser policy/store proof | This design | All mandatory mechanisms work on target Home and Pro builds; blockers change scope explicitly |
| M1: reproducible foundation | Solution/projects, pinned tools/dependencies, build scripts, CI, pure rule model, simulated adapters | M0 decisions | A new contributor follows README and builds/tests without changing host protection |
| M2: execution and networking | Compiled catalog policies, persistent WFP backend, inventory, built-in host adapters, effective-state verification | M1 | Positive/negative enforcement tests pass including portable and preexisting tools |
| M3: service and owner control | Automatic service, SCM recovery, secure IPC, credentials/recovery, status UI | M1; integrate with M2 | Standard-user boundary, termination recovery and authenticated maintenance tests pass |
| M4: browser protection | Store-ready MV3 extensions, bridge, tested policy setup, per-profile health | M1; M3 IPC | Known-URL prevention demonstrated; unsupported contexts reported accurately |
| M5: NSIS lifecycle | Install, upgrade, repair, uninstall, crash journal, offline recovery runbook | M2/M3; integrate M4 | All entry points authenticated; clean recovery through interruption and reboot |
| M6: trusted updates and catalog operation | Signed metadata/artifacts, update verifier, staged rollout/rollback, rule-maintenance workflow | M2/M3/M5 | Tampered, stale and downgraded updates rejected; known-good protection retained |
| M7: release qualification | Full VM/hardware/browser matrix, accessibility review, signed release, operational readiness | M2-M6 | Public release checklist complete; coverage claims match evidence |
| M8: optional strict mode | Curated allowlisting, owner app-approval lifecycle, compatibility/recovery assessment | Stable standard mode | Separate opt-in profile passes its own tests; no universal protection claim |

## 2. M0: answer the risky questions first

Run only in disposable Windows VMs with snapshots, local console access and separate standard-user/administrator accounts. Retain a bootable recovery method and encryption recovery material where applicable. Never experiment with enforcing policies on a family's everyday computer.

Record the exact OS edition/build/patch level, architecture, Secure Boot state, baseline policies, browser version/profile type and test artifact identities for every result.

1. **Home application control:** on 25H2 Home and Pro, deploy a lab-authored narrow deny policy. Check UMCI and relevant Store enforcement with a benign test executable plus a separately obtained known remote-support artifact. Verify renamed/copied binaries, installed/portable forms, MSI/MSIX and script behavior. Confirm compiler requirements on Pro and deployment without ConfigCI on Home.
2. **Policy ownership/removal:** prove stable owned GUIDs, coexistence with another restrictive base policy, rejection of malformed policies, effective-state checks, refresh/restart behavior and removal. Run at least two reboots. Preserve unrelated policies and record exactly which settings are changed.
3. **WFP:** install persistent filters through the user-mode API. Kill the configuring process, restart Windows, and test TCP/UDP over IPv4/IPv6. Demonstrate outbound relay prevention and existing-flow reauthorization with controlled peers. Compare native Windows Firewall APIs if WFP integration adds unacceptable complexity; retain one selected backend.
4. **Consumer browsers:** measure store-extension installation, local/personal profiles, private/guest modes, policy override precedence and offline startup. An unpacked developer extension is not proof that a consumer installer can deploy it. Resolve stable store identity/publication requirements before promising download coverage.
5. **Scope decision:** write a short result record for each probe with pass/fail, logs, source links, observed gaps and implementation implications. Keep unknown-tool controls separate from known-tool results.

If native enforcement fails on a target edition, the result is an explicit support limitation or a redesigned mechanism. Do not silently substitute process killing and preserve the same protection claim. If broad browser coverage is unavailable, ship only an accurately labeled partial browser layer after the rest of the release gates pass.

## 3. M1: repository and development foundation

The [README](../README.md) contains tooling installation and contribution requirements. The following is the **planned layout**, not files that already exist:

```text
RefundScamBlocker.slnx
global.json
Directory.Build.props
Directory.Packages.props
.editorconfig
src/
  RefundScamBlocker.Core/
  RefundScamBlocker.Windows/
  RefundScamBlocker.Service/
  RefundScamBlocker.Setup/
  RefundScamBlocker.UI/
  RefundScamBlocker.BrowserBridge/
  RefundScamBlocker.UpdateFetcher/
extensions/chromium/
installer/nsis/
policies/
  schema/
  catalog/
  templates/
tests/
  RefundScamBlocker.UnitTests/
  RefundScamBlocker.WindowsIntegrationTests/
  RefundScamBlocker.InstallerTests/
  browser/
  fixtures/
scripts/
  build.ps1
  test.ps1
  package.ps1
  verify-environment.ps1
docs/
  ARCHITECTURE.md
  IMPLEMENTATION_PLAN.md
  recovery/                 # added and validated before release
  validation/               # exact lab evidence, with secrets redacted
.github/workflows/
```

Select C#/.NET 10, WPF for the UI, TypeScript/MV3 for browsers and NSIS 3.x Unicode. Pin a supported exact .NET SDK in `global.json` (research baseline 10.0.401), central NuGet versions plus lockfiles, a tested NSIS version (baseline 3.12), and Node 24 LTS with a package lock for extension development. Record Windows SDK/tool versions needed for policy compilation and signing. Resolve versions again when scaffolding; these pins do not yet exist.

Keep `Core` independent from Windows side effects, with typed interfaces for clock, signature verification, policy storage, deployment, credential verifier and inventory. Use immutable validated models, cancellation/timeouts and bounded queues. Turn on nullable reference types and relevant analyzers. Define native error mapping and dispose Win32 handles reliably. Choose an established .NET test framework and pin its runner/adapter compatibility.

### Planned build interface

M1 must deliver and document these script contracts before asking contributors to run them:

| Script | Default behavior / requirements |
| --- | --- |
| `verify-environment.ps1` | Read-only tool/version/API checks; distinguishes authoring host from protected/test target |
| `build.ps1` | Explicit `Native`, `Browser` or `All` component selection, default `Native`; locked restore and deterministic Release compile for selected components; no service or policy installation |
| `test.ps1` | Ordinary unit/component tests by default; privileged integration tests require an explicit suite selection and designated VM |
| `package.ps1` | Require a successful `All` build, publish self-contained Windows payloads, assemble NSIS artifact and checksums; development output unmistakably labeled unsigned |

Publish logs that distinguish build, unit-test, integration-test and packaging outcomes. A selected component fails if its required tools are missing; a native-only build explicitly reports browser work as not selected, never passed. Release packaging requires all components and their tooling. Document exact commands and artifact paths in the README once these scripts exist. Avoid root-level scaffolding that pretends protection is implemented before it is.

CI initially runs formatting, analyzers, pure tests, catalog schema checks, extension lint/type-check/tests and unsigned packaging. Isolate privileged Windows tests in resettable VMs. Untrusted pull requests must not run elevated on a persistent maintainer machine or access release keys. Protect release workflows and pin external actions to reviewed immutable revisions.

## 4. M2-M4: functional implementation work

### Execution, inventory and network workstream

- Implement rule-schema validation and explicit compile-time mapping from catalog identities to App Control rules. Preserve provenance and test artifact hashes. Reject unknown fields/actions, invalid ranges and unsafe broad publisher/domain scopes.
- Generate standard-mode policies with AllowAll in both scenarios and narrow denies; require UMCI and tested Store enforcement for claimed package coverage. Retain script enforcement and the explicit COM allowance described in the architecture; test host-specific behavior and compatibility effects, including audit mode. Test updates against existing rules, policy IDs and other vendors' policies. Never automatically trust every installed executable.
- Create a WFP adapter with owned provider/sublayer/filter IDs, security descriptors, persistence and transactions. Test native memory ownership and partial failures. Query actual installed objects to reconcile state.
- Inventory installed applications, packages, services and running executable identities. Process events and file notifications trigger bounded rescans; they supplement execution enforcement. Guard against PID reuse, image replacement, reparse points, inaccessible files and unsupported signatures.
- For a verified running match, block its network identity and contain only that component. Store separate timestamps for discovery, rule activation and observed session interruption. Race conditions are recorded, not hidden by a final “process stopped” result.
- Add separate adapters for Quick Assist/Remote Assistance/RDP and optional WinRM/OpenSSH restrictions. Preserve prior state. Capability-detect Home differences and decline conflicting enterprise policy ownership.

### Service, credentials and UI workstream

- Implement Automatic SCM startup, bounded initialization, effective-policy health checks and 5/15/60-second crash recovery. Ensure fatal worker exceptions produce failure status; authorized maintenance and OS shutdown stop cleanly.
- Define read-only and privileged IPC operations. Enforce local-only ACLs, caller token validation, replay prevention, bounded message parsing and a single maintenance transaction. Fuzz malformed/oversized input.
- Implement install-time password enrollment, Argon2id verification, durable rate limiting, password change and one-time recovery-code rotation. Clear sensitive buffers under control and exclude secrets from logs/dumps. Test missing/corrupted records and concurrent attempts.
- Implement a small accessible WPF status/settings application. Show application, network and browser protection separately, catalog age, restart requirement and useful corrective steps. No dashboard implies all remote access is impossible.
- Gate weakening changes/removal within the privileged code, including service-down repair. Keep the browser bridge unable to mutate privileged policy.

### Browser workstream

- Use stable store extension IDs, MV3 declarative URL rules and a minimal native-messaging host allowlist. Treat browser messages as untrusted; schema-check and bound native message sizes.
- Keep network-request blocking separate from asynchronous download cancellation. Test cached/service-worker responses, redirects, blob/data URLs, alternate download mechanisms and rule quota exhaustion.
- Verify each policy on each profile; record cases unsupported by Edge/Chrome, including personal accounts and private/guest contexts. Store availability is an installation dependency, not proof that every context is covered.
- Provide a calm, accessible block explanation and local help. Do not collect a browsing history or accept browser-originated requests to disable the engine. Extension updates and rules follow store constraints; no remote executable code.

## 5. M5-M6: installation, maintenance and updates

Implement the NSIS transaction sequence in architecture section 9. The setup helper owns the sensitive work; installer script callbacks alone are not authorization. Test every public entry point: Installed apps, direct uninstaller, `/S`, repair, reinstall, upgrade, downgrade, helper invocation and service-unavailable fallback.

Create a journal format before writing rollback logic. Each operation records its owned object/path, expected previous/current state, intended change and completed phase. Validate paths remain inside intended product-owned locations before recursive cleanup. Handle power loss between recording and applying, and between applying and recording completion. Resume idempotently; retain recovery tools until removal is confirmed.

Test updating the service while its persistent filters and execution policy remain active. Verify that an older signed installer cannot reset the password or relax policy. Preserve credential parameters, rate limits and installation identity across upgrades; schema migration failures preserve recoverable old state.

Select a maintained trusted-update implementation after a dependency/license review and .NET integration spike. Test signed metadata expiration, version rollback, hash/length mismatch, wrong key, key rotation, truncated downloads, replay, clock problems and offline use. Failing to validate an update must not erase the working catalog. Authentication of a binary signer alone is insufficient authorization to run any signed program as SYSTEM.

Add a rule-operation workflow: collect legitimate vendor artifacts in an isolated lab; record identities; design a narrow rule; run positive and unrelated-software tests; independently review; sign; stage rollout; monitor opt-in/redacted failures; withdraw faulty generations using trusted recovery metadata. Do not scrape executable filenames into an automatically shipped denylist.

Maintain a tested known-good generation, public release notes, software bill of materials, checksums, signature verification instructions and third-party notices. The project must be able to ship runtime security updates, rotate compromised distribution keys and publish emergency recovery instructions before encouraging installation on vulnerable users' machines.

## 6. Acceptance tests

Test outcomes independently: **download prevented**, **installation prevented**, **execution prevented**, **new session prevented**, and **existing session interrupted**. Passing one does not imply the others. Negative controls confirm the computer remains useful.

| ID / request | Scenario | Required evidence / result |
| --- | --- | --- |
| T01 / i | Cold boot before login; no internet | Service starts automatically without a window; embedded rules active; native protection does not wait for network or GUI |
| T02 / v | Close settings UI; try ordinary-user Task Manager termination | UI closure leaves protection active; standard user cannot change/stop service under documented permissions |
| T03 / v | Elevated lab termination of the service process; fatal worker exception | SCM recovery occurs, rules remain enforced during downtime; show elapsed time and no visible restart window |
| T04 / boundary | Elevated administrator successfully stops/disables service | Record documented boundary; do not pretend ordinary SCM recovery defeats administrative disable |
| T05 / ii | Wrong/empty password, replayed grant, direct helper and silent uninstall | Supported weakening/removal paths refuse unauthorized operation; no credential reset or partial removal |
| T06 / ii | Correct-password removal; forgotten-password reset with recovery code, online and offline | Authenticated operation succeeds; reset rotates the code and rejects its predecessor; only owned state removed/restored |
| T07 / iii | Known official download URL and redirects in supported browser/profile | Request-time block proven before transfer where supported; cancellation result tracked separately |
| T08 / iii | USB/email/SMB/archive/Store/package-manager delivery bypass | Covered code still denied at execution; download allowed or installation partly completed is reported honestly |
| T09 / iii-iv | Known portable tool copied, renamed or run from another location | Identity rule prevents execution; do not pass using only a filename match |
| T10 / iii-iv | New vendor version/certificate and unsigned/custom build | Show which rules still match and which do not; unknown standard-mode gaps become catalog work, not fabricated passes |
| T11 / iv | Tool uses outbound 443/80 fallback, direct LAN or self-hosted relay | Covered identity blocked regardless of common endpoint assumptions; normal HTTPS remains usable |
| T12 / iv | IPv4/IPv6, TCP/UDP, Wi-Fi/Ethernet changes, VPN/proxy and DNS changes | Path/identity rules remain effective on supported paths; distinguish DNS/URL bypass from code/network enforcement |
| T13 / iv | Preinstalled and already-connected known tool at activation/update | Observe flow reauthorization and safe containment; measure time to interruption; no claim of retroactive prevention |
| T14 / iv | Quick Assist reinstall; legacy Remote Assistance invitation; RDP host re-enable attempt | Tested components remain blocked for standard user; no blanket block of unrelated Microsoft software |
| T15 / browser | Personal/local profiles, private/guest, missing extension and portable alternate browser | Effective coverage agrees with status; no green universal-download claim when protection is absent |
| T16 / browser | Browser screen share, allowed conference app, arbitrary cobrowsing site | Supported hardening behavior verified; allowed/unsupported cases clearly recorded as limitations |
| T17 / recovery | Reboot twice, BFE restart, service crash, update interrupted | Persistent owned state reloads/reconciles; no unintended policy deletion, network blackout or false healthy state |
| T18 / installer | Power loss/cancel/disk full/locked files during install, upgrade and uninstall | Journal recovers idempotently; correct pending-restart status; unrelated security settings unchanged |
| T19 / updates | Bad signature/hash, replay/rollback/expired metadata, offline and clock skew | Working policy retained; rejected updates cannot trigger arbitrary privileged actions; recoverable status |
| T20 / coexistence | Existing restrictive App Control policy, Defender, VPN and browser policy | No loosening/reset of third-party policy; incompatible setups declined or accurately documented |
| T21 / permissions | IPC squatting, spoofed caller, reparse path, corrupt state, concurrent auth flood | No unauthorized privileged operation, secret disclosure, unbounded resource use or insecure credential reset |
| T22 / usability | Banking, browser/Windows updates, email, video calls, screen reader, password manager, printer | Ordinary functions pass; intentional sharing restrictions documented; no unrelated signer/domain false positive |
| T23 / removal | Install over existing settings, external setting changes, uninstall/reboot | Only RSB-owned changes restored; newer external changes preserved; no resurrection after authorized removal |
| T24 / strict later | Unknown executable, allowed interpreter/extension, needed app update | Unknown code denied as designed; interpreter/browser gaps tested; owner approval and rollback work |
| T25 / scripts and compatibility | PowerShell, WSH, batch/third-party interpreters, MSHTA/MSXML and COM-dependent apps | Observe actual script behavior rather than treating a log as prevention; disclose restrictions and verify unrelated app compatibility in audit and enforcement modes |
| T26 / owner recovery boundary | Both secrets lost, or credential record absent/corrupt | Normal product paths refuse access/re-enrollment; separate documented Windows-administrator recovery works only with OS authority; standard users cannot use it |

Use controlled remote peers and consenting lab accounts only. Obtain proprietary tools from official sources under their licenses; use synthetic signed fixtures for repeatable unit tests, but validate representative real tools in integration tests. A synthetic executable named `AnyDesk.exe` does not prove AnyDesk coverage. Do not use a real victim's bank account, screen recording, call or device as a fixture.

### Matrix and recording

Minimum release matrix: Windows 11 25H2 Home/Pro x64, local standard-user/administrator accounts, local and personal-account browser profiles, current stable Edge/Chrome, IPv4/IPv6, offline startup and clean install/upgrade/uninstall. Include Windows 11 24H2 only while supported. Add at least one low-end physical PC and an accessibility session. ARM64 and newer Windows builds require new rows before support is claimed.

Each result includes test ID, OS/browser versions, catalog/policy/build identifiers, artifact identities, account privilege, initial state, observed outcome, timing and redacted diagnostic evidence. Publish a coverage table per release naming actually tested products/versions and their enforcement layer. “Unknown,” “not supported” and “failed” remain separate from “passed.”

### Proposed performance and reliability budgets

These are targets to validate and adjust with evidence, not existing measurements:

- First service crash: ready again within 10 seconds in the reference VM; repeated recovery within the configured 60-second backoff plus startup time. Record persistent enforcement separately from coordinator readiness.
- Idle service: below 1% average CPU over ten quiet minutes and below 150 MiB working set on the reference low-end device, with the WPF UI closed. Inventory and updates use bounded concurrency.
- Known execution denies: verify the blocked payload does not execute its test action or establish a support session. A delayed kill is failure for this criterion.
- Already-running known-session containment: target at most five seconds after verified rule activation in controlled TCP/UDP tests, while reporting the pre-activation exposure separately.
- Startup and normal browsing: no more than 5% regression against a repeatable baseline workload without RSB; no unrelated application blocks in the release negative-control corpus.
- Normal idle operation: no periodic popups; zero password/recovery values in captured logs and diagnostics.

## 7. Public release gate

All items are pending:

- [ ] M0-M6 evidence and relevant T01-T23/T25-T26 results reviewed on supported Home/Pro builds.
- [ ] No known unresolved bypass of the stated standard-user credential/IPC boundary; residual coverage gaps disclosed.
- [ ] Installer, executables and update artifacts verifiable with documented public identities; no production keys in source or CI logs.
- [ ] Browser store distribution works on ordinary consumer Windows; limitations and missing-extension states are understandable.
- [ ] Offline, interrupted-install and authenticated-uninstall recovery tested; active code policies removed safely.
- [ ] Rule provenance, licenses, false-positive review and tested product/version coverage published.
- [ ] Upgrade/downgrade, runtime patch delivery and emergency rule withdrawal rehearsed.
- [ ] Accessibility, owner consent and standard-user setup reviewed with representative users.
- [ ] Private vulnerability reporting channel actually configured and documented; do not assume GitHub reporting is enabled.
- [ ] Maintainers assigned for releases, signing keys, catalog updates and recovery support; hosting/store/signing costs accounted for.
- [ ] README installation/build instructions replaced with tested commands and real release links; design-only notices updated only when truthful.

## 8. Decisions still requiring implementation evidence

The architecture selects a direction now; these are bounded engineering checks rather than reasons to postpone all work:

| Decision | Resolve by | Evidence needed |
| --- | --- | --- |
| Exact Home package/script enforcement and policy deployment | M0 | Effective-state and real payload tests on specified builds |
| Native WFP adapter versus simpler Windows Firewall backend | M0 | Persistence, connection coverage, ownership/coexistence and implementation cost |
| Per-profile Edge/Chrome coverage and store policy deployment | M0/M4 | Consumer-profile matrix and store-distributed extension trial |
| Argon2id library and calibrated parameters | M3 | Maintained license-compatible dependency, known-answer tests, low-end benchmark and review |
| Maintained update-framework client for .NET | M6 | Supported implementation or reviewed interop, adversarial metadata tests and key-rotation rehearsal |
| Release signing and funding arrangements | M7 | Usable credentials/infrastructure and documented maintainer responsibilities |
| Strict mode, temporary support exceptions, signed tamper-resistant App Control policies | M8 / separate design | Demonstrated need, concrete owner workflow, compatibility and recovery evidence |

Further research findings belong beside the affected design decision with primary links and a review date. Revise both documents when evidence changes the architecture; do not preserve an inaccurate promise for consistency with an earlier plan.
