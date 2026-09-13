# RefundScamBlocker

RefundScamBlocker is a proposed free, open-source Windows application designed to reduce phone scams involving remote access to a computer. It is intended for a trusted family member or caregiver to install and manage, with the owner's consent. Anyone can be targeted by a scam.

**Current status: Phase 1 developer tools.** The repository now contains a read-only Windows capability report, a synthetic blocking-rule simulator, and automated tests. It does **not** install a service, change Windows policies, or block any software or connection. Consumer protection, installer and browser extension work are still ahead.

Read the [architecture and research](docs/ARCHITECTURE.md) for the threat model, evidence, enforcement choices and limitations, and the [implementation plan](docs/IMPLEMENTATION_PLAN.md) for milestones, acceptance criteria and release gates. Research and dependency versions were checked on September 12, 2026.

## Start here: Phase 1

From this checkout in PowerShell, run:

```powershell
# Optional: download the exact SDK to .tools, verify its SHA-512, and leave system installation alone.
.\scripts\bootstrap-dotnet.ps1
.\scripts\verify-environment.ps1
.\scripts\build.ps1
.\scripts\test.ps1

# Read-only Windows information; no usernames or computer names are included.
.\scripts\run.ps1 capabilities --json

# Compare an innocuous text file against its synthetic sample rule. Nothing is executed or blocked.
.\scripts\run.ps1 simulate --catalog .\examples\synthetic-catalog.json --file .\tests\fixtures\harmless-demo.txt --json
```

The sample returns `WouldBlock` with `protectionActive: false`. Different bytes return `NoMatchingRule`, which does not mean safe. The sample is not a production catalog; it has no real remote-access product identities. Simulation accepts regular local files and rejects direct network/device paths.

These tools run without administrator rights. Only the SDK and test-package downloads need internet access. The bootstrap and dependency caches stay under ignored `.tools`; test results go to ignored `artifacts/test-results`. An already installed SDK matching [global.json](global.json) also works. Windows PowerShell 5.1 and PowerShell 7 are supported script targets. Building does not require NSIS or Node yet.

See the [Phase 1 checklist](docs/phases/PHASE_1.md). **Phase 2** starts with read-only inventory of the desktop/laptop and Windows 10-specific policy/recovery validation, before live blocking.

## Planned protection

Remote support applications often connect outward to relay services, so blocking unsolicited incoming connections alone is insufficient. The proposed design combines reviewed remote-access software rules with Windows application execution controls, network controls, supported-browser download restrictions and restrictions on built-in remote assistance features.

The background service will start with Windows. Windows Service Control Manager recovery will restart it after an unexpected termination, including a tested Task Manager process termination. Closing the settings window will leave protection running. Normal operation should be quiet; blocked actions and a degraded protection state must remain understandable through the status interface. A successful administrative service stop is different from a crash and does not automatically trigger recovery. [Microsoft service recovery semantics](https://learn.microsoft.com/en-us/windows/win32/api/winsvc/ns-winsvc-service_failure_actions_flag).

The supported uninstall and protection-changing workflows will require the caregiver password set during installation, or a separately stored recovery credential. This protects against ordinary users following a scammer's instructions. It cannot prevent a determined administrator, kernel compromise, offline system modification or operating-system replacement. Use a standard Windows account for everyday activity and keep administrator credentials with the trusted caregiver.

The project cannot promise to recognize every remote-access application or prevent every download. Unknown tools, renamed files, encrypted archives, alternative browsers and browser-based assistance require different controls. Execution and connection blocking provide additional protection when a file arrives another way. Coverage and false-positive handling must be documented and tested before release.

## Selected technology and platform

| Component | Planned choice |
| --- | --- |
| Background protection and policy management | C# on .NET 10 LTS, hosted as a Windows service |
| Caregiver settings and status | Separate WPF desktop application |
| Execution control | Windows App Control for Business, with product-owned, reviewed policies |
| Network control | Windows Filtering Platform (WFP), including outbound connections |
| Supported-browser controls | TypeScript extension and browser policy integration, subject to browser-specific validation |
| Installer | NSIS 3.12, Unicode, with an elevated setup helper and explicit rollback |
| Native code | Only where a demonstrated Windows API requirement justifies it; no custom kernel driver in the initial baseline |

NSIS remains maintained; version 3.12 was released on April 19, 2026. The architecture compares it with WiX/MSI and MSIX. [NSIS downloads](https://nsis.sourceforge.io/Download).

**Windows 10 Home and Pro x64 are the primary targets. Windows 11 is not required.** Validation starts with Windows 10 22H2, including the owner's desktop and laptop; older builds need individual compatibility checks. Windows 11 will be tested as an additional target. ARM64/x86, S mode, Windows Server and centrally managed devices are outside initial qualification. An existing organization's security policy must never be replaced by this installer.

Windows compatibility is separate from OS servicing. Microsoft's [Windows 10 ESU information](https://www.microsoft.com/en-us/windows/extended-security-updates) explains continued security updates for eligible 22H2 devices. The blocker cannot replace those updates. Likewise, the [.NET 10 OS support table](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md) currently omits consumer Windows 10 22H2: Phase 1 uses .NET 10 as a developer toolchain, and Phase 2 must resolve production runtime support while preserving the Windows 10 target.

App Control policies can enforce on Home, but Microsoft's App Control PowerShell authoring commands are unavailable there. Policy development therefore needs a Pro development machine or VM, with separate Home compatibility testing. Policies affect the device, not only one user. [App Control feature availability](https://learn.microsoft.com/en-us/windows/security/application-security/application-control/app-control-for-business/feature-availability).

## Installation and removal — planned workflow

**There is currently no protection installer.** Phase 1 is safe to run as read-only developer tooling on the owner's machines. Live policy testing comes later, on explicitly designated devices with recovery prepared. A consumer release must first pass the implementation plan's installation, recovery, compatibility and security checks.

The intended consumer workflow is:

1. The owner and trusted caregiver review the protection scope and decide whether legitimate remote support is needed. Check the supported Windows version and identify existing assistance software before installation.
2. Obtain the released installer through this project's official distribution link when one is published. Verify the published signer and release verification information. Run setup with caregiver-held administrator credentials.
3. Review the compatibility check and proposed changes. Setup must report conflicts and provide a recoverable rollback path before enabling enforcement.
4. Set and confirm a unique caregiver password. Save the generated recovery code somewhere the caregiver controls, outside the protected computer. The installer must explain password recovery before completing setup.
5. Complete setup and any required restart. Use the intended daily **standard user** account. The caregiver retains the separate Windows administrator credentials.
6. Open RefundScamBlocker's status window and verify that all selected protection layers are active. Complete the provided harmless verification check; a missing extension, failed policy deployment or stale rules must not appear as full protection.
7. For legitimate maintenance or removal, use the authenticated management/uninstall workflow. Enter the caregiver password or recovery credential and approve any required Windows elevation. Removal must restore only this product's owned settings and verify cleanup. If the normal workflow cannot run, follow the documented owner recovery procedure supplied with the release.

The planned installer will bundle the application runtime, so end users should not need Visual Studio, Node.js, NSIS or a separately installed .NET runtime. The project will need to distribute runtime security updates with application releases. [Self-contained .NET deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/).

## Development environment

Documentation contributions need only Git and a text editor. Implementation contributors should prepare the following environment.

### 1. Prepare an isolated Windows workspace

Phase 1 can be developed on Windows 10 x64; Home is sufficient for the current diagnostics/simulation. Later policy authoring requires an appropriate Pro environment, while Home gets compiled policies. The owner intends to test on a Windows 10 desktop and laptop. Begin with read-only inventory; prepare backups, local console access and a tested recovery procedure before narrowly scoped live policy tests. Disposable Home/Pro VMs remain useful for repeated failure/reboot testing, but are not mandatory for running Phase 1. Run ordinary editing/builds without elevation.

Install [Git for Windows](https://git-scm.com/downloads/win), then clone the repository into a development folder:

```powershell
git clone https://github.com/EthannYakabuski/RefundScamBlocker.git
Set-Location RefundScamBlocker
git status
```

Use your existing checkout if already cloned.

### 2. Install .NET and an editor

The exact **.NET 10 SDK 10.0.401** is pinned in [global.json](global.json). The optional bootstrap above downloads Microsoft's Windows x64 archive with the checksum recorded in [SDK metadata](scripts/dotnet-sdk.json). Alternatively install that exact SDK from the [official .NET download page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0). Do not substitute a different SDK without updating and testing the repository pin.

For a full IDE, install Visual Studio 2026 **18.0 or later** and the **.NET desktop development** workload. Alternatively, use an editor with the standalone .NET CLI. The SDK includes the runtime needed for development. Microsoft's [Windows installation guide](https://learn.microsoft.com/en-us/dotnet/core/install/windows) also documents this installation command:

```powershell
winget install --exact --id Microsoft.DotNet.SDK.10 --version 10.0.401
dotnet --info
dotnet --list-sdks
```

.NET 10 LTS is supported through November 14, 2028. [.NET support dates](https://dotnet.microsoft.com/en-us/platform/support/policy).

### 3. Install tooling for your component

- **Installer:** install [NSIS 3.12](https://nsis.sourceforge.io/Download). Add its installation directory to your user `PATH`, or invoke `makensis.exe` by its full installed path. Verify with `makensis /VERSION` in a new terminal.
- **Policy authoring, later phases:** use Windows PowerShell 5.1 in the designated Pro authoring environment. Open `powershell.exe -NoProfile`, then inspect `$PSVersionTable.PSVersion` and `Get-Module -ListAvailable ConfigCI`. Home does not need authoring cmdlets for Phase 1. Missing CiTool on Windows 10 is expected and does not by itself mean App Control is unavailable.
- **Browser extension:** install [Node.js 24 LTS](https://nodejs.org/en/download), including npm; verify with `node --version` and `npm --version`. Node is needed for extension development only. The implementation milestone will pin TypeScript and other packages in the extension lockfile. [Node release status](https://nodejs.org/en/about/previous-releases).
- **Signing or native development:** install the [Windows SDK](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/). Add C++ build tools only if working on a selected native component. A WDK installation is not a baseline prerequisite. Release-signing credentials belong in protected release infrastructure, never a developer checkout.

### 4. Build and test behavior

The solution contains Core, Diagnostics and CLI projects plus Core/CLI test projects. Builds use central package versions, committed NuGet lockfiles, analyzers and warnings as errors. `build.ps1` defaults to `-Components Native`; `Browser` and `All` fail explicitly until those components exist. `test.ps1` defaults to `-Suite Unit`; `Integration` fails explicitly until Phase 2 tooling exists. Packaging and the NSIS installer are deferred to Phase 8; there is no placeholder installer.

The CLI uses exit code `0` for a successful report/simulation, `2` for invalid arguments and `3` for invalid/unreadable simulation input. A `WouldBlock` simulation is a successful comparison, not a real enforcement event. JSON output is machine-readable with enum names; ordinary errors omit full input paths and catalog contents.

Run the build/test commands in the quick start after each relevant change. If you intentionally update dependencies, regenerate lockfiles with the repository NuGet configuration, review the dependency diff and rerun tests. Do not use unlocked restore in routine CI/builds. CI checks developer tools; it cannot establish real Windows 10 prevention or recovery coverage.

## Contributing

Contributions are welcome under the existing [Apache License 2.0](LICENSE). Preserve license notices and document the licenses and provenance of new dependencies, policy data and redistributed assets.

- Start with a focused issue or proposal and link the affected requirement and milestone. Keep pull requests scoped and explain the user-visible behavior, relevant evidence, validation and remaining limitations.
- Follow the documented threat model. Changes to privileged IPC, authentication, uninstall/recovery, updates, application-control policies or network enforcement require independent security review before release. Do not add hidden persistence, unsafe command execution or an administrator-proof claim.
- For detection rules, provide official vendor evidence, precise identity/matching criteria, an expiry/review rationale where relevant, expected blocks and legitimate-use exceptions. Include regression cases for renamed/portable variants and false positives. Do not block broad shared infrastructure without evidence and impact testing.
- Add tests proportionate to the change. Security boundaries and enforcement behavior need meaningful positive and negative cases. Document Windows edition/build, account privilege, browser version and recovery results for integration tests. Documentation-only changes need accurate links and consistent claims.
- Never commit passwords, recovery codes, signing keys, access tokens or real victim data. Use synthetic fixtures and redact identifying paths, usernames, session codes and personal browsing information from diagnostics. Signatures must be verified before applying distributed rules or updates.
- Keep accessibility and caregiver usability central: readable status, keyboard navigation, useful failure explanations and a tested recovery path. Update the design documents whenever implementation changes a guarantee or limitation.

For a suspected vulnerability, use GitHub's private vulnerability reporting on this repository **if enabled**. Its availability has not been assumed or configured here. If no private channel is available, open a sanitized issue asking the maintainer to establish one; withhold exploit details, secrets and personal information from the public issue until a suitable channel is agreed.
