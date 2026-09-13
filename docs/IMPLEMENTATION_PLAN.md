# RefundScamBlocker: phased implementation and validation plan

Status: **Phase 1 complete for developer tooling; product enforcement remains unimplemented and unproven**. This plan implements the scoped behavior and security boundaries in [ARCHITECTURE.md](ARCHITECTURE.md). The [Phase 1 checklist](phases/PHASE_1.md) records its deliverables and validation; the [README](../README.md) contains commands for the code currently available. A completed developer milestone does not mean the computer is protected.

## Starting point and delivery rules

**Start with Phase 1: a non-enforcing developer foundation.** It gives contributors a reproducible build, a small validated rule model, synthetic tests and a read-only Windows capability report. It requires no Windows policy changes, service installation or elevation. It can be completed without a VM or owner password decision.

**Phase 2 is the mandatory gate before product enforcement.** Prove App Control, network persistence and owned-state recovery on the designated Windows 10 test machines before implementing or distributing an enforcing application. The initial targets are the owner's Windows 10 desktop and laptop; provisionally validate Windows 10 22H2 x64 Home/Pro, then refine the matrix from their actual editions/builds. Windows 11 becomes an additional target after its own tests. Phase 1's pure logic and host observations may precede that proof; they cannot establish it. A process watcher, capability report or synthetic hash match is not a security MVP.

The first public version requires functioning pre-execution blocking, persistent network enforcement, owner authentication, recovery, updates and honest browser coverage reporting. Start the owner's machines with read-only inventory and offline policy review. Disposable VMs with snapshots are recommended for initial enforcement and interruption tests; an explicitly designated owner-controlled machine can be used with local console access and prepared recovery. Use small reviewable changes and independently review security-critical code and policies. A phase closes only with its stated evidence; unknown, blocked, failed and passed are distinct statuses.

The desktop has been identified as **Windows 10 Home 22H2, build 19045.6466**; the laptop's exact build remains to be recorded. Retain .NET 10 for the Phase 1 developer foundation provisionally. Microsoft's [.NET 10 supported-OS table](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md) does not currently list consumer Windows 10 22H2. A successful build/run on this desktop demonstrates that specific result, not vendor runtime support. Phase 2 must settle the production service runtime or a suitable native fallback while preserving Windows 10 as the product goal. See the architecture for OS/ESU lifecycle scope; 22H2 is the first validation baseline, not proof that older Windows 10 versions cannot work.

| Phase | Reviewable result | Dependency | Current status |
| --- | --- | --- | --- |
| 1. Developer foundation | Buildable .NET solution, synthetic rule evaluator, read-only capability CLI and unit tests | Architecture | Complete; 66 tests passed, see checklist |
| 2. Windows feasibility lab | Windows 10 inventory, enforcement and recovery evidence; selected network backend and browser scope | 1 | Pending; blocks enforcement development |
| 3. Execution engine | Reviewed catalog-to-policy compilation and owned App Control deployment | 2 | Pending |
| 4. Network containment | Persistent application filtering, inventory and built-in host adapters | 2; integrate 3 | Pending |
| 5. Background service and health | Automatic startup, SCM recovery, accurate component health and read-only status | 3 and 4 | Pending |
| 6. Owner authentication | Password/recovery lifecycle, secured maintenance IPC and local settings UI | 5 | Pending |
| 7. Browser protection | Consumer-store extensions, constrained bridge and per-profile coverage | 2 and 6 | Pending |
| 8. NSIS installation lifecycle | Install, upgrade, repair, authenticated uninstall and interrupted-operation recovery | 3-7 | Pending |
| 9. Trusted updates and catalog operation | Verified updates, rollback protection and reviewed rule publication | 8 | Pending |
| 10. Release qualification | Full evidence matrix, accessible experience and signed release | 1-9 | Pending |
| 11. Optional strict mode | Separate owner-approved allowlisting profile | Stable standard-mode release | Deferred |

Phases 3 and 4 can proceed in parallel once Phase 2 resolves their platform contracts. Browser design can proceed alongside native work after the Phase 2 scope decision; completion requires the secured service contract. Every later phase should begin with a small task checklist and evidence location like [Phase 1](phases/PHASE_1.md), then close with a reviewable change and its tests.

## Phase 1: developer foundation

**Objective:** create the smallest useful implementation that is safe to run on a contributor's ordinary computer.

**Deliverables:**

- Pin an exact .NET 10 SDK and test dependency versions; add solution/projects, common build settings, nullable checking, analyzers and package lockfiles.
- Keep Core free of Windows side effects. Introduce a bounded, strictly validated synthetic catalog and exact artifact-SHA-256 evaluator with immutable validated models.
- Add a read-only Windows capability adapter and CLI. Report observed information, unavailable checks and unsupported conditions without declaring enforcement active or proved. Do not prompt for elevation or change security settings.
- Provide a clearly synthetic catalog/fixture and a dry-run result. A match means only that the fixture matches a rule; a non-match makes no claim that software is safe. Plain artifact SHA-256 is not an App Control image hash.
- Add build, test and read-only environment scripts; document exact commands and outputs in the README. Add automated tests for parser rejection, identity matching and health/capability reporting.

**Acceptance:** a fresh contributor can restore/build/test and run the documented synthetic demo. Invalid or unsupported input is rejected explicitly; changes to identity prevent an exact-hash match; renamed copies retain the same identity result. The CLI identifies itself as non-enforcing. Checks create only normal development outputs; no service, startup task, browser setting, firewall rule, App Control policy or password store is installed. Test and build status must reflect actual runs.

**Dependencies:** the existing architecture and access to the pinned SDK/dependencies. No VM, installer, Node, Windows policy compiler, code-signing certificate or real remote-access artifact is needed for this phase.

**Next action:** review the completed [PHASE_1.md](phases/PHASE_1.md) evidence, inventory the laptop and prepare Phase 2 recovery for the designated Windows 10 targets. Do not widen the synthetic evaluator into an unreviewed production blocklist.

## Phase 2: prove Windows enforcement and recovery in a lab

**Objective:** resolve the mechanisms that can make the product ineffective or leave a computer unusable before building product enforcement around them.

**Deliverables:** begin with read-only inventory of the owner's Windows 10 desktop and laptop. Record exact edition/build/patch, architecture, runtime/OS servicing status, Secure Boot state, baseline policies, browser/profile versions and artifact identities. Prepare local console access, separate standard-user/administrator accounts, a restorable backup or VM snapshot, bootable recovery and encryption recovery material where applicable before applying a policy. Disposable VMs remain the recommended place for destructive interruption tests, but they are not a prerequisite for read-only investigation or all owner-machine testing.

1. **Windows 10 application control:** examine the inventoried builds first; author and inspect a narrow benign executable policy before any activation. Treat Windows audit-policy deployment as a system change, not a read-only step. After recovery preparation, test the smallest deny on a designated target, provisionally 22H2 Home/Pro x64. Check UMCI and relevant Store enforcement, then add a separately obtained known remote-support artifact. Verify copied/renamed binaries, portable/installed forms, MSI/MSIX and scripts. Confirm authoring requirements on Pro and deployment without ConfigCI on Home.
2. **Windows 10 policy deployment/removal:** define a Windows 10-specific adapter and documented manual recovery procedure. CiTool availability is not a prerequisite; do not assume Windows 11 deployment/removal commands or reboot-free removal work on Windows 10. Prove stable owned GUIDs, effective-state checks, malformed-policy rejection, coexistence with another restrictive base policy, refresh/restart behavior and removal across at least two reboots. Preserve unrelated policies and record all changes. Account for any required reboot before calling removal complete.
3. **Network enforcement:** probe persistent user-mode WFP filters with TCP/UDP over IPv4/IPv6, configuring-process termination and reboot. Demonstrate outbound relay prevention and existing-flow reauthorization with controlled peers. Compare native Windows Firewall APIs if WFP complexity is excessive, then select one backend.
4. **Consumer browsers:** measure store-extension installation, local/personal profiles, private/guest modes, policy precedence and offline startup. An unpacked developer extension does not prove consumer deployment. Resolve store identity/publication dependencies before promising coverage.
5. **Decision records:** publish pass/fail/unknown, redacted logs, primary source links, observed gaps and design consequences for each probe. Separate known-tool results from unknown-tool limitations.

**Acceptance:** mandatory native mechanisms and owned-state removal work on the actual Windows 10 target builds; rollback preserves other security policies and ordinary connectivity. Resolve a maintainable production runtime/native implementation path for these targets and record its support/lifecycle separately from observed API availability and real enforcement results. Browser limits have an explicit scope decision. Where a mandatory mechanism fails, revise support or architecture and revalidate before proceeding; process killing cannot replace pre-execution prevention under the same claim. Windows 11 requires independent adapter and recovery tests before adding a support claim.

**Dependencies:** Phase 1; designated owner-controlled Windows 10 targets, recovery preparation and licensed test artifacts. The owner's interest in using their machines does not turn Phase 1 commands into live enforcement; the first activation is a distinct, documented Phase 2 step.

**Next action:** collect read-only capability reports from both machines and complete a recovery/evidence record for the first target. Review a narrow benign executable policy offline, then prove that it can be applied, observed and removed on that target. Integration scripts require explicit suite and target selection; ordinary build/test commands never activate policies.

## Phase 3: reviewed catalog and execution engine

**Objective:** prevent covered code from executing using Windows enforcement, including when delivery/download controls were bypassed.

**Deliverables:**

- Evolve the synthetic schema into a production catalog: stable rule IDs, provenance, artifact identities, tested version/architecture, rationale, review deadline, negative controls and rollback references. Reject unknown actions/fields, invalid ranges and broad unsafe scopes.
- Compile narrow App Control identities explicitly. Keep artifact SHA-256 separate from derived App Control image/Authenticode hashes; use real artifact evidence, never fabricated hashes or signer identifiers.
- Generate standard-mode policies with AllowAll in both scenarios, narrow denies, UMCI, tested Store enforcement, retained script enforcement and the architecture's explicit COM allowance. Do not automatically trust installed executables.
- Deploy/reconcile only owned policy IDs through the Phase 2-verified Windows 10 adapter, using trusted compiled artifacts on Home. Preserve previous effective policy on failure and honor restart requirements. Keep any later Windows 11/CiTool path separately capability-detected and tested.
- Add Windows integration fixtures and a build-time validation path for production catalog changes. Establish formatting/analyzer/unit/catalog CI with reviewed immutable action pins; untrusted PRs receive no privileged host or release keys.

**Acceptance:** relevant T08-T10, T20 and T25 execution cases pass in the lab, including copied/renamed and portable artifacts, package/script distinctions, certificate/version changes and unrelated software controls. Audit logs alone cannot satisfy prevention. Removal/coexistence remain proven after updates.

**Dependencies:** Phase 2 App Control and recovery gates, Phase 1 Core/build foundation.

**Next action:** compile and validate a single reviewed narrow identity end to end before expanding the catalog. Record tested product/version coverage beside its evidence.

## Phase 4: network containment and host inventory

**Objective:** deny covered network activity, including covered tools that were already present or connected at activation.

**Deliverables:**

- Implement the Phase 2-selected backend. For WFP, own provider/sublayer/filter IDs, explicit security descriptors, persistent objects and transactions. Test SafeHandle/native-memory ownership and partial failure; query effective state rather than trusting successful configuration calls.
- Inventory applications, packages, services and executable identities. Use bounded rescans triggered by process/file events. Handle PID reuse, replacement, reparse points, inaccessible files and unsupported signatures.
- Match verified running components, activate their network identities and contain only those components. Record discovery, rule activation and observed interruption separately; do not hide the race with a final process-stopped result.
- Add separately tested Quick Assist, Remote Assistance and RDP adapters, with optional WinRM/OpenSSH restrictions. Preserve prior state, detect edition support and decline conflicting enterprise ownership.

**Acceptance:** T11-T14, network portions of T17/T20 and relevant T22 cases pass across TCP/UDP, IPv4/IPv6, relay/LAN, interface changes and VPN/proxy contexts. Persistent state survives coordinator termination and reboot. Existing-session interruption is measured; unrelated HTTPS/OS services remain usable.

**Dependencies:** Phase 2 network/rollback gates; integrate with Phase 3 identity and execution policies before claiming combined protection.

**Next action:** add one owned persistent application block and a controlled-peer test that proves enforcement continues after the configuring process exits, then verify complete owned-state removal.

## Phase 5: background service and component health

**Objective:** coordinate established enforcement silently across startup, crashes and offline operation.

**Deliverables:**

- Implement Automatic SCM startup with bounded initialization and 5/15/60-second crash recovery. Fatal worker errors produce failure status; authorized maintenance and OS shutdown stop cleanly.
- Keep enforcement in the OS during service downtime. Implement reconciliation, bounded logging and explicit service/execution/network/built-in/restart health states.
- Add a versioned read-only status channel with local-only ACLs, server identity checks, bounded messages/timeouts and cancellation. Prepare a separate secured maintenance contract for Phase 6.
- Keep the UI separate from the service. Ordinary GUI closure cannot stop protection; avoid recurring popups and do not hide installation/processes.

**Acceptance:** T01-T04 and service portions of T17 pass, with elapsed recovery time recorded. An active process is never sufficient for healthy status. Elevated administrative stop/disable remains an explicit boundary. Failed backends retain other working layers and report the failure.

**Dependencies:** Phases 3 and 4; effective-state interfaces and native rollback.

**Next action:** build a lab-installed coordinator around one already tested enforcement generation and prove offline boot plus fatal-worker recovery. Do not present this development registration as a consumer installer.

## Phase 6: owner authentication and local maintenance

**Objective:** require owner authorization for every supported weakening/removal operation, with a usable recovery path.

**Deliverables:**

- Implement install-time password enrollment, reviewed Argon2id verification, durable rate limiting, password change and one-time recovery-code rotation. Select a maintained license-compatible library, use known-answer tests and calibrate parameters on low-end hardware.
- Enforce authorization inside privileged operations, including the service-down helper. Use local caller token validation, one-use operation-specific authorization, replay prevention and an exclusive maintenance transaction.
- Reject malformed/oversized IPC and spoofed callers; validate server identity and resist pipe squatting. Missing/corrupted credential state triggers explicit recovery, never empty-password acceptance or re-enrollment.
- Add a small accessible WPF status/settings UI with separate component coverage, catalog age, restart needs and corrective actions. Exclude secrets from logs/dumps and minimize sensitive-buffer lifetime.
- Specify and test the Windows-administrator recovery boundary when both secrets are lost. A browser bridge may not mutate privileged policy.

**Acceptance:** T05-T06, T21, T26 and relevant T22 cases pass. Concurrent requests cannot bypass the lockout or maintenance lock. Authorized removal restores only owned state; standard users cannot invoke OS-administrator recovery. No ordinary product endpoint bypasses the owner credential requirement.

**Dependencies:** Phase 5 service/IPC and Phases 3-4 ownership/rollback contracts.

**Next action:** review the privileged operation vocabulary and credential-state format, then implement enrollment and verification independently of installer UI.

## Phase 7: browser download protection

**Objective:** prevent known remote-support URL requests in tested consumer browser contexts and show the actual scope.

**Deliverables:**

- Pin Node/TypeScript tooling and package locks; create MV3 declarative request rules, stable store IDs and a minimal native-messaging allowlist.
- Validate and bound every native message. Limit the unprivileged bridge to approved rules and health; never accept browser requests to disable the engine or arbitrary privileged actions.
- Keep request-time prevention separate from asynchronous download cancellation. Test redirects, cached/service-worker responses, blob/data URLs, alternate mechanisms and rule quotas.
- Verify Edge/Chrome policy effectiveness per profile. Provide accessible local block explanations and no browsing-history collection or remote executable code.
- Extend CI with lint, type checking and browser unit tests; record store distribution prerequisites.

**Acceptance:** T07 and T15-T16 pass within the Phase 2-agreed scope. Missing extensions, private/guest modes, personal/local profiles and alternate browsers accurately report limited coverage. No universal download claim appears when a layer is absent.

**Dependencies:** Phase 2 consumer deployment evidence and Phase 6 secured bridge/service boundary.

**Next action:** prove one store-distributed extension request rule and its per-profile health report, then expand only within observed coverage.

## Phase 8: NSIS install, repair and removal

**Objective:** turn tested components into a recoverable consumer installation with authenticated maintenance.

**Deliverables:**

- Pin a tested NSIS 3.x Unicode release and implement the architecture section 9 sequence through a dedicated setup helper. Installer callbacks provide UI plumbing; privileged code owns authentication.
- Define a durable versioned journal before implementing rollback. Record owned object/path, expected previous/current state, intended mutation and completed phase; validate cleanup paths remain product-owned.
- Handle power loss between journal and mutation, disk full, locked files, cancellation and reboot. Resume idempotently and retain recovery tooling until removal is confirmed.
- Test Installed apps, direct uninstaller, `/S`, repair, reinstall, upgrade, downgrade, direct helper and service-unavailable fallback. Preserve credential parameters, lockout state and installation identity across upgrades.
- Add `package.ps1` only with real packageable components: require successful native/browser builds, publish self-contained Windows payloads, assemble NSIS artifact/checksums and plainly label unsigned development builds. Record policy compiler and signing tools.
- Produce an offline recovery runbook and unsigned packaging CI; keep privileged integration tests in resettable VMs.

**Acceptance:** T05-T06, T17-T18, T23 and T26 lifecycle cases pass. Older signed installers cannot reset credentials or relax protection. Persistent enforcement remains during service updates. Only owned state is restored, and externally changed values are preserved. No restart after authorized removal.

**Dependencies:** Phases 3-7. NSIS is retained unless concrete implementation evidence justifies revising the architecture.

**Next action:** review the transaction/journal schema and interruption cases, then package a single complete install-and-authenticated-remove loop in a VM.

## Phase 9: trusted updates and catalog operation

**Objective:** maintain protection safely as software identities, runtime vulnerabilities and signing keys change.

**Deliverables:**

- Select a maintained trusted-update implementation after a dependency/license review and .NET integration spike. Verify role-separated metadata, signatures, target hashes/lengths, expiry and rollback/freeze resistance.
- Separate bounded unprivileged fetching from privileged validation/apply. Rules are validated data, not scripts or arbitrary registry/command instructions. A trusted binary signer alone does not authorize arbitrary SYSTEM execution.
- Stage complete generations and reconcile backend transactions with the journal. Preserve the known-good catalog/enforcement on failed metadata, interrupted apply, offline operation or clock errors.
- Implement reviewed rule operations: official artifact acquisition in isolated labs, provenance, narrow identity design, positive/negative tests, independent review, signing, staged release and trusted emergency withdrawal.
- Publish release notes, SBOM, third-party notices, checksums and signature verification instructions. Rehearse key rotation and embedded-runtime security releases; collect diagnostics only with explicit opt-in/redaction.

**Acceptance:** T19 and update portions of T17-T18 pass for bad signature/hash/length, wrong keys, truncation, replay, rollback, expiry, clock skew and offline operation. A failed update leaves working protection intact. Key rotation and emergency rule recovery are rehearsed.

**Dependencies:** Phase 8 lifecycle/journal and established native/browser catalog contracts.

**Next action:** review the selected update client's threat model and prove rejection of a tampered target and stale metadata using test keys before any online distribution.

## Phase 10: release qualification

**Objective:** establish that standard mode meets its advertised scope on the supported machines and can be supported after release.

**Deliverables:** execute Appendix A's full release matrix, complete Appendix B, publish observed coverage by tested product/version/layer, and resolve or disclose residual gaps. Include low-end hardware, accessibility, recovery, coexistence, signing, store deployment and maintainer operations. Configure the actual private vulnerability reporting channel before documenting it as available.

**Acceptance:** Phases 1-9 evidence and relevant T01-T23/T25-T26 reviewed; all mandatory public release checkboxes complete. Installation/build instructions point to tested commands and real signed artifacts. Universal blocking and administrator-proof removal are not advertised.

**Dependencies:** Phases 1-9; signing/distribution infrastructure and maintainers assigned.

**Next action:** run a release-candidate rehearsal from a clean Home VM and then Pro, recording every installation/coverage/recovery outcome before expanding the hardware matrix.

## Phase 11: optional strict mode

**Objective:** offer a separately validated allowlisting profile for owners who accept a more restrictive computer.

**Deliverables:** curate allowed applications; design owner app approval, updates and rollback; examine interpreters, browser features, native-messaging hosts, DLLs, installer brokers and broad signer/path exceptions. Review any temporary support exception or signed App Control tamper-resistance as a separate recovery-sensitive design.

**Acceptance:** T24 and the relevant full standard-mode matrix pass for the stricter profile. Unknown-code denial, interpreter/browser gaps, legitimate updates and accessible owner recovery are demonstrated. There is explicit opt-in and no universal prevention claim.

**Dependencies:** stable standard mode, demonstrated demand and a dedicated compatibility/recovery review.

**Next action:** document one target household workflow and its needed applications before producing any allowlist. Do not enable strict mode during ordinary installation.

## Mapping from the original milestones

This map preserves earlier design references while making the work smaller. The old M0 feasibility gate remains mandatory for enforcement; only safe M1 groundwork moves ahead of it.

| Original milestone | Current phases |
| --- | --- |
| M0: feasibility and recovery | Phase 2 |
| M1: reproducible foundation | Phase 1, then production catalog/CI in 3, browser tooling in 7 and packaging in 8 |
| M2: execution and networking | Phases 3 and 4 |
| M3: service and owner control | Phases 5 and 6 |
| M4: browser protection | Phase 7 |
| M5: NSIS lifecycle | Phase 8 |
| M6: trusted updates and catalog operation | Phase 9 |
| M7: release qualification | Phase 10 |
| M8: optional strict mode | Phase 11 |

### Build and development contracts across phases

The README describes what exists now; future phases must update it when commands become runnable. Native builds and pure unit tests must never install security policy. Where `Native`, `Browser` or `All` component selection is offered, missing selected tools fail the operation; unselected components are reported as not selected, never passed. Privileged integration tests always require explicit suite selection, a designated target and recovery preparation; disposable VMs are recommended, especially for interruption tests. Release packaging requires all components and their tools.

Keep Core independent of Windows effects, with typed interfaces as needed for clock, signature verification, storage, deployment, credentials and inventory. Use immutable validated models, bounded parsing/queues, cancellation/timeouts, reliable native handle disposal and explicit native-error mapping. Keep dependencies pinned with lockfiles. Do not add empty product projects or fake packaging outputs to imply a feature exists.

## Appendix A: product acceptance tests

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

Initial development matrix: the owner's Windows 10 desktop and laptop, with their exact editions/builds recorded. Provisional release target: Windows 10 22H2 Home/Pro x64, subject to the architecture's runtime/OS lifecycle findings and successful platform tests. Cover local standard-user/administrator accounts, local and personal-account browser profiles, compatible maintained Edge/Chrome versions, IPv4/IPv6, offline startup and clean install/upgrade/uninstall. Add Windows 11 as a separate tested target; neither its API behavior nor its lifecycle proves Windows 10 compatibility. Add at least one low-end physical PC and an accessibility session. ARM64 and any additional Windows builds require new rows before support is claimed.

Each result includes test ID, OS/browser versions, catalog/policy/build identifiers, artifact identities, account privilege, initial state, observed outcome, timing and redacted diagnostic evidence. Publish a coverage table per release naming actually tested products/versions and their enforcement layer. “Unknown,” “not supported” and “failed” remain separate from “passed.”

### Proposed performance and reliability budgets

These are targets to validate and adjust with evidence, not existing measurements:

- First service crash: ready again within 10 seconds in the reference VM; repeated recovery within the configured 60-second backoff plus startup time. Record persistent enforcement separately from coordinator readiness.
- Idle service: below 1% average CPU over ten quiet minutes and below 150 MiB working set on the reference low-end device, with the WPF UI closed. Inventory and updates use bounded concurrency.
- Known execution denies: verify the blocked payload does not execute its test action or establish a support session. A delayed kill is failure for this criterion.
- Already-running known-session containment: target at most five seconds after verified rule activation in controlled TCP/UDP tests, while reporting the pre-activation exposure separately.
- Startup and normal browsing: no more than 5% regression against a repeatable baseline workload without RSB; no unrelated application blocks in the release negative-control corpus.
- Normal idle operation: no periodic popups; zero password/recovery values in captured logs and diagnostics.

## Appendix B: public release gate

All items are pending:

- [ ] Phases 1-9 evidence and relevant T01-T23/T25-T26 results reviewed on supported Home/Pro builds.
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

## Appendix C: decisions still requiring implementation evidence

The architecture selects a direction now; these are bounded engineering checks rather than reasons to postpone all work:

| Decision | Resolve by | Evidence needed |
| --- | --- | --- |
| Production service runtime or native fallback for consumer Windows 10 | Phase 2 | Target build inventory, vendor support/lifecycle review, actual runtime/API tests and a maintainable security-update path |
| Exact Home package/script enforcement and policy deployment | Phase 2 | Effective-state and real payload tests on specified builds |
| Native WFP adapter versus simpler Windows Firewall backend | Phase 2 | Persistence, connection coverage, ownership/coexistence and implementation cost |
| Per-profile Edge/Chrome coverage and store policy deployment | Phases 2 and 7 | Consumer-profile matrix and store-distributed extension trial |
| Argon2id library and calibrated parameters | Phase 6 | Maintained license-compatible dependency, known-answer tests, low-end benchmark and review |
| Maintained update-framework client for .NET | Phase 9 | Supported implementation or reviewed interop, adversarial metadata tests and key-rotation rehearsal |
| Release signing and funding arrangements | Phase 10 | Usable credentials/infrastructure and documented maintainer responsibilities |
| Strict mode, temporary support exceptions, signed tamper-resistant App Control policies | Phase 11 / separate design | Demonstrated need, concrete owner workflow, compatibility and recovery evidence |

Further research findings belong beside the affected design decision with primary links and a review date. Revise both documents when evidence changes the architecture; do not preserve an inaccurate promise for consistency with an earlier plan.
