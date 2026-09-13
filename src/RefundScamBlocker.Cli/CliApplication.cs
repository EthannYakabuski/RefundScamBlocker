using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using RefundScamBlocker.Core;
using RefundScamBlocker.Diagnostics;

namespace RefundScamBlocker.Cli;

public static class CliApplication
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (args.Length == 0 || (args.Length == 1 && args[0] is "help" or "--help" or "-h"))
        {
            output.WriteLine("RefundScamBlocker developer tools — Phase 1");
            output.WriteLine("Read-only diagnostics and simulation. Protection is NOT IMPLEMENTED.");
            output.WriteLine("  capabilities [--json]");
            output.WriteLine("  simulate --catalog <json-file> --file <file> [--json]");
            output.WriteLine("No command installs policies, runs the inspected file, or blocks a connection.");
            return 0;
        }

        try
        {
            if (args[0] == "capabilities" && (args.Length == 1 || (args.Length == 2 && args[1] == "--json")))
            {
                var report = CapabilityCollector.Collect();
                if (args.Length == 2)
                {
                    output.WriteLine(JsonSerializer.Serialize(report, JsonOptions));
                }
                else
                {
                    output.WriteLine("Read-only capability report. Presence of an API does not prove enforcement works.");
                    output.WriteLine(JsonSerializer.Serialize(report, JsonOptions));
                }

                return 0;
            }

            if (args[0] == "simulate" && TryParseSimulation(args, out var catalogPath, out var filePath, out var json))
            {
                return Simulate(catalogPath, filePath, json, output, error);
            }
        }
        catch (CatalogValidationException)
        {
            error.WriteLine("Invalid catalog. Use schema version 1 with bounded, unique rules and exact SHA-256 identities.");
            return 3;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            error.WriteLine("Could not read the selected input. Check that it is a readable local file.");
            return 3;
        }

        error.WriteLine("Invalid arguments. Run with --help for supported read-only commands.");
        return 2;
    }

    private static bool TryParseSimulation(string[] args, out string catalog, out string file, out bool json)
    {
        catalog = string.Empty;
        file = string.Empty;
        json = false;
        for (var index = 1; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--json" when !json:
                    json = true;
                    break;
                case "--catalog" when catalog.Length == 0 && index + 1 < args.Length:
                    catalog = args[++index];
                    break;
                case "--file" when file.Length == 0 && index + 1 < args.Length:
                    file = args[++index];
                    break;
                default:
                    return false;
            }
        }

        return !string.IsNullOrWhiteSpace(catalog) && !string.IsNullOrWhiteSpace(file)
            && !catalog.StartsWith("--", StringComparison.Ordinal) && !file.StartsWith("--", StringComparison.Ordinal);
    }

    private static int Simulate(string catalogPath, string filePath, bool json, TextWriter output, TextWriter error)
    {
        catalogPath = ValidateLocalFile(catalogPath);
        filePath = ValidateLocalFile(filePath);
        // Bound both allocation and actual reads, even if the input grows during inspection.
        using var catalogStream = File.OpenRead(catalogPath);
        var buffer = new byte[CatalogParser.MaximumCatalogBytes + 1];
        var length = catalogStream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
        if (length > CatalogParser.MaximumCatalogBytes)
        {
            error.WriteLine("Catalog exceeds the 1 MiB input limit.");
            return 3;
        }

        var catalog = CatalogParser.Parse(buffer.AsSpan(0, length));
        // The selected file is read as bytes only; it is never launched or modified.
        using var fileStream = File.OpenRead(filePath);
        var hash = Convert.ToHexString(SHA256.HashData(fileStream));
        var decision = RuleEvaluator.Evaluate(catalog, hash);
        var result = new
        {
            mode = "SimulationOnly",
            protectionActive = false,
            catalog.CatalogVersion,
            artifactSha256 = hash,
            decision.Outcome,
            decision.Matches,
            explanation = "This is a local hash comparison, not an authenticated policy or Windows enforcement. NoMatchingRule does not mean safe."
        };

        if (!json) { output.WriteLine("SIMULATION ONLY — no file or connection was blocked."); }
        output.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
        return 0;
    }

    private static string ValidateLocalFile(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (OperatingSystem.IsWindows())
        {
            // Refuse UNC shares and Win32 device namespaces before probing them.
            if (fullPath.StartsWith(@"\\", StringComparison.Ordinal)) { throw new ArgumentException("Network/device paths are not supported."); }
            var root = Path.GetPathRoot(fullPath)!;
            if (fullPath.AsSpan(root.Length).Contains(':') || new DriveInfo(root).DriveType == DriveType.Network)
            {
                throw new ArgumentException("Network paths and alternate streams are not supported.");
            }
        }

        if (!File.Exists(fullPath)) { throw new IOException("Input is not a regular file."); }
        var attributes = File.GetAttributes(fullPath);
        if ((attributes & (FileAttributes.Directory | FileAttributes.Device | FileAttributes.ReparsePoint)) != 0)
        {
            throw new ArgumentException("Input is not a regular local file.");
        }

        return fullPath;
    }
}
