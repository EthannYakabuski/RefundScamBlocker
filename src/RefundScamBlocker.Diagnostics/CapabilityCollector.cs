using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Principal;
using Microsoft.Win32;

namespace RefundScamBlocker.Diagnostics;

/// <summary>Collects prerequisite observations without changing machine state or running tools.</summary>
public static class CapabilityCollector
{
    private const string WindowsVersionKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
    private const string PowerShellEngineKey = @"SOFTWARE\Microsoft\PowerShell\3\PowerShellEngine";

    public static CapabilityReport Collect()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new CapabilityReport(
                IsWindows: false,
                RuntimeInformation.OSArchitecture.ToString(),
                RuntimeInformation.ProcessArchitecture.ToString(),
                ObservedValue.NotApplicable,
                ObservedValue.NotApplicable,
                ObservedValue.NotApplicable,
                ObservedValue.NotApplicable,
                Availability.NotApplicable,
                Availability.NotApplicable,
                ElevationStatus.NotApplicable);
        }

        return new CapabilityReport(
            IsWindows: true,
            RuntimeInformation.OSArchitecture.ToString(),
            RuntimeInformation.ProcessArchitecture.ToString(),
            ReadRegistryValue(WindowsVersionKey, "DisplayVersion"),
            ReadRegistryValue(WindowsVersionKey, "CurrentBuildNumber"),
            ReadRegistryValue(WindowsVersionKey, "UBR"),
            ReadRegistryValue(WindowsVersionKey, "EditionID"),
            ProbeSystemFile("CiTool.exe"),
            ProbeWindowsPowerShell51(),
            ObserveElevation());
    }

    [SupportedOSPlatform("windows")]
    private static ObservedValue ReadRegistryValue(string subkey, string valueName)
    {
        try
        {
            using RegistryKey machine = RegistryKey.OpenBaseKey(
                RegistryHive.LocalMachine,
                Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            using RegistryKey? key = machine.OpenSubKey(subkey, writable: false);
            object? raw = key?.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            string? value = raw switch
            {
                string text => text,
                int number when number >= 0 => number.ToString(CultureInfo.InvariantCulture),
                _ => null,
            };

            return string.IsNullOrWhiteSpace(value)
                ? ObservedValue.Unknown
                : new ObservedValue(ObservationStatus.Known, value);
        }
        catch (Exception exception) when (IsReadFailure(exception))
        {
            return ObservedValue.Unknown;
        }
    }

    [SupportedOSPlatform("windows")]
    private static Availability ProbeWindowsPowerShell51()
    {
        Availability executable = ProbeSystemFile("WindowsPowerShell", "v1.0", "powershell.exe");
        if (executable != Availability.Present)
        {
            return executable;
        }

        ObservedValue version = ReadRegistryValue(PowerShellEngineKey, "PowerShellVersion");
        if (version.Status != ObservationStatus.Known || !Version.TryParse(version.Value, out Version? parsed))
        {
            return Availability.Unknown;
        }

        return parsed.Major == 5 && parsed.Minor == 1
            ? Availability.Present
            : Availability.Missing;
    }

    [SupportedOSPlatform("windows")]
    private static Availability ProbeSystemFile(params string[] relativeSegments)
    {
        try
        {
            string systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
            if (string.IsNullOrEmpty(systemDirectory))
            {
                return Availability.Unknown;
            }

            // Cross-architecture redirection needs validation before claiming tool presence.
            if (RuntimeInformation.ProcessArchitecture != RuntimeInformation.OSArchitecture)
            {
                return Availability.Unknown;
            }

            string file = Path.Combine(systemDirectory, Path.Combine(relativeSegments));
            FileAttributes attributes = File.GetAttributes(file);
            return attributes.HasFlag(FileAttributes.Directory) ? Availability.Missing : Availability.Present;
        }
        catch (FileNotFoundException)
        {
            return Availability.Missing;
        }
        catch (DirectoryNotFoundException)
        {
            return Availability.Missing;
        }
        catch (Exception exception) when (IsReadFailure(exception))
        {
            return Availability.Unknown;
        }
    }

    [SupportedOSPlatform("windows")]
    private static ElevationStatus ObserveElevation()
    {
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent(TokenAccessLevels.Query);
            WindowsPrincipal principal = new(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator)
                ? ElevationStatus.Elevated
                : ElevationStatus.NotElevated;
        }
        catch (Exception exception) when (IsReadFailure(exception))
        {
            return ElevationStatus.Unknown;
        }
    }

    private static bool IsReadFailure(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or SecurityException or Win32Exception;
}
