namespace RefundScamBlocker.Diagnostics;

/// <summary>Whether an observation was obtained, unavailable, or inapplicable.</summary>
public enum ObservationStatus
{
    Unknown,
    Known,
    NotApplicable,
}

/// <summary>A value whose absence never implies a negative capability result.</summary>
public sealed record ObservedValue(ObservationStatus Status, string? Value)
{
    public static ObservedValue Unknown { get; } = new(ObservationStatus.Unknown, null);

    public static ObservedValue NotApplicable { get; } = new(ObservationStatus.NotApplicable, null);
}

/// <summary>Presence of a prerequisite, without implying that it can enforce rules.</summary>
public enum Availability
{
    Unknown,
    Present,
    Missing,
    NotApplicable,
}

/// <summary>Best-effort observation of the current process token.</summary>
public enum ElevationStatus
{
    Unknown,
    Elevated,
    NotElevated,
    NotApplicable,
}

/// <summary>This development phase does not implement any enforcement.</summary>
public enum EnforcementStatus
{
    NotImplemented,
}

/// <summary>
/// Read-only prerequisite observations. Tool presence does not establish Windows
/// support, policy compatibility, or protection. No host or user identifiers are included.
/// </summary>
public sealed record CapabilityReport(
    bool IsWindows,
    string OsArchitecture,
    string ProcessArchitecture,
    ObservedValue WindowsDisplayVersion,
    ObservedValue WindowsBuild,
    ObservedValue WindowsRevision,
    ObservedValue WindowsEdition,
    Availability CiTool,
    Availability WindowsPowerShell51,
    ElevationStatus Elevation)
{
    public EnforcementStatus Enforcement => EnforcementStatus.NotImplemented;
}
