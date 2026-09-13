# RefundScamBlocker

RefundScamBlocker is a proposed free, open-source Windows application designed to reduce phone scams involving remote access to a computer. It is intended for a trusted family member or caregiver to install and manage, with the owner's consent. Anyone can be targeted by a scam.

**Current status: research and design only. There is no working application, installer, browser extension, or protection provided by this repository yet.** The installation and feature descriptions below are plans for implementation, not available functionality.

Read the [architecture and research](docs/ARCHITECTURE.md) for the threat model, evidence, enforcement choices and limitations, and the [implementation plan](docs/IMPLEMENTATION_PLAN.md) for milestones, acceptance criteria and release gates. Research and dependency versions were checked on September 12, 2026.

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

The initial target is **Windows 11 Home and Pro, x64, version 25H2**. Version 24H2 is eligible only while Microsoft supports the relevant edition and our compatibility tests pass; Home and Pro support ends October 13, 2026. ARM64 is a later target. Windows 10, S mode, Windows Server and centrally managed enterprise devices are outside the initial support baseline. An existing organization's security policy must never be replaced by this installer. [Windows lifecycle](https://learn.microsoft.com/en-us/lifecycle/products/windows-11-home-and-pro).

App Control policies can enforce on Home, but Microsoft's App Control PowerShell authoring commands are unavailable there. Policy development therefore needs a Pro development machine or VM, with separate Home compatibility testing. Policies affect the device, not only one user. [App Control feature availability](https://learn.microsoft.com/en-us/windows/security/application-security/application-control/app-control-for-business/feature-availability).

## Installation and removal — planned workflow

**There is currently nothing to install.** Do not apply experimental application-control or network policies to a family member's computer. A release must first pass the implementation plan's installation, recovery, compatibility and security checks.

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

Use an updated Windows 11 Pro x64 development environment and separate clean Windows 11 Home and Pro test VMs. Create a standard user and a caregiver administrator in each VM. Keep snapshots, console access and recovery information available before testing policies, startup behavior or uninstall flows. Run ordinary editing and builds without elevation; elevate only the specific integration step that needs it. Never develop enforcement on a computer currently relied on for remote recovery.

Install [Git for Windows](https://git-scm.com/downloads/win), then clone the repository into a development folder:

```powershell
git clone https://github.com/EthannYakabuski/RefundScamBlocker.git
Set-Location RefundScamBlocker
git status
```

Use your existing checkout if already cloned.

### 2. Install .NET and an editor

Install the Windows x64 **.NET 10 SDK** from the [official .NET download page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0). The checked SDK version is **10.0.401**, with runtime 10.0.12. The initial implementation milestone will pin the selected SDK in `global.json`; that file does not exist yet. Use a current serviced .NET 10 SDK until the repository supplies its exact pin.

For a full IDE, install Visual Studio 2026 **18.0 or later** and the **.NET desktop development** workload. Alternatively, use an editor with the standalone .NET CLI. The SDK includes the runtime needed for development. Microsoft's [Windows installation guide](https://learn.microsoft.com/en-us/dotnet/core/install/windows) also documents this installation command:

```powershell
winget install --exact --id Microsoft.DotNet.SDK.10
dotnet --info
dotnet --list-sdks
```

.NET 10 LTS is supported through November 14, 2028. [.NET support dates](https://dotnet.microsoft.com/en-us/platform/support/policy).

### 3. Install tooling for your component

- **Installer:** install [NSIS 3.12](https://nsis.sourceforge.io/Download). Add its installation directory to your user `PATH`, or invoke `makensis.exe` by its full installed path. Verify with `makensis /VERSION` in a new terminal.
- **Policy authoring:** use the Windows PowerShell 5.1 included with Windows 11 Pro. Open `powershell.exe -NoProfile`, then inspect `$PSVersionTable.PSVersion` and `Get-Module -ListAvailable ConfigCI`. Do not assume PowerShell 7 or Home provides the same authoring environment. This check does not deploy a policy.
- **Browser extension:** install [Node.js 24 LTS](https://nodejs.org/en/download), including npm; verify with `node --version` and `npm --version`. Node is needed for extension development only. The implementation milestone will pin TypeScript and other packages in the extension lockfile. [Node release status](https://nodejs.org/en/about/previous-releases).
- **Signing or native development:** install the [Windows SDK](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/). Add C++ build tools only if working on a selected native component. A WDK installation is not a baseline prerequisite. Release-signing credentials belong in protected release infrastructure, never a developer checkout.

### 4. Build and test status

There is currently **no solution, project file, package manifest, build script, test suite or NSIS script** to run. Tool-version checks above are usable now; `dotnet build`, `dotnet test`, `npm ci` and installer compilation are not yet repository workflows.

Milestone M1 in the [implementation plan](docs/IMPLEMENTATION_PLAN.md) must introduce the source layout, dependency pins and restore, build, test, publish and package interfaces. These must support locked restoration, reproducible builds, unprivileged unit tests, separately selected VM integration tests and protected signing. Update this README with executable commands when those interfaces exist.

## Contributing

Contributions are welcome under the existing [Apache License 2.0](LICENSE). Preserve license notices and document the licenses and provenance of new dependencies, policy data and redistributed assets.

- Start with a focused issue or proposal and link the affected requirement and milestone. Keep pull requests scoped and explain the user-visible behavior, relevant evidence, validation and remaining limitations.
- Follow the documented threat model. Changes to privileged IPC, authentication, uninstall/recovery, updates, application-control policies or network enforcement require independent security review before release. Do not add hidden persistence, unsafe command execution or an administrator-proof claim.
- For detection rules, provide official vendor evidence, precise identity/matching criteria, an expiry/review rationale where relevant, expected blocks and legitimate-use exceptions. Include regression cases for renamed/portable variants and false positives. Do not block broad shared infrastructure without evidence and impact testing.
- Add tests proportionate to the change. Security boundaries and enforcement behavior need meaningful positive and negative cases. Document Windows edition/build, account privilege, browser version and recovery results for integration tests. Documentation-only changes need accurate links and consistent claims.
- Never commit passwords, recovery codes, signing keys, access tokens or real victim data. Use synthetic fixtures and redact identifying paths, usernames, session codes and personal browsing information from diagnostics. Signatures must be verified before applying distributed rules or updates.
- Keep accessibility and caregiver usability central: readable status, keyboard navigation, useful failure explanations and a tested recovery path. Update the design documents whenever implementation changes a guarantee or limitation.

For a suspected vulnerability, use GitHub's private vulnerability reporting on this repository **if enabled**. Its availability has not been assumed or configured here. If no private channel is available, open a sanitized issue asking the maintainer to establish one; withhold exploit details, secrets and personal information from the public issue until a suitable channel is agreed.
