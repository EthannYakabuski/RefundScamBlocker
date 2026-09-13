using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefundScamBlocker.Cli;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace RefundScamBlocker.Cli.Tests;

[TestClass]
public sealed class CliApplicationTests
{
    [TestMethod]
    public void HelpMakesAbsenceOfProtectionExplicit()
    {
        var result = Run("--help");
        Assert.AreEqual(0, result.Code);
        StringAssert.Contains(result.Output, "NOT IMPLEMENTED");
        Assert.AreEqual(string.Empty, result.Error);
    }

    [TestMethod]
    [DataRow("install")]
    [DataRow("uninstall")]
    [DataRow("enable")]
    [DataRow("disable")]
    public void MutatingVerbsAreNotAccepted(string verb)
    {
        var result = Run(verb);
        Assert.AreEqual(2, result.Code);
        Assert.AreEqual(string.Empty, result.Output);
    }

    [TestMethod]
    public void CapabilityJsonAlwaysSaysEnforcementIsNotImplemented()
    {
        var result = Run("capabilities", "--json");
        Assert.AreEqual(0, result.Code);
        using var document = JsonDocument.Parse(result.Output);
        Assert.AreEqual("NotImplemented", document.RootElement.GetProperty("enforcement").GetString());
        Assert.IsFalse(document.RootElement.TryGetProperty("userName", out _));
        Assert.IsFalse(document.RootElement.TryGetProperty("computerName", out _));
        Assert.IsFalse(document.RootElement.TryGetProperty("userProfile", out _));
    }

    [TestMethod]
    public void MatchingSimulationNeverChangesOrRunsItsInput()
    {
        using var files = new InputFiles();
        var result = Run("simulate", "--catalog", files.Catalog, "--file", files.Payload, "--json");
        Assert.AreEqual(0, result.Code);
        using var document = JsonDocument.Parse(result.Output);
        Assert.AreEqual("WouldBlock", document.RootElement.GetProperty("outcome").GetString());
        Assert.IsFalse(document.RootElement.GetProperty("protectionActive").GetBoolean());
        Assert.AreEqual("SimulationOnly", document.RootElement.GetProperty("mode").GetString());
        CollectionAssert.AreEqual(files.OriginalBytes, File.ReadAllBytes(files.Payload));
        Assert.AreEqual(2, Directory.GetFiles(files.DirectoryPath).Length);
    }

    [TestMethod]
    public void RenamedInputStillMatchesItsBytes()
    {
        using var files = new InputFiles();
        var renamed = Path.Combine(files.DirectoryPath, "different name.exe");
        File.Move(files.Payload, renamed);
        var result = Run("simulate", "--catalog", files.Catalog, "--file", renamed, "--json");
        Assert.AreEqual(0, result.Code);
        using var document = JsonDocument.Parse(result.Output);
        Assert.AreEqual("WouldBlock", document.RootElement.GetProperty("outcome").GetString());
    }

    [TestMethod]
    public void DifferentBytesAreNoMatchingRuleInsteadOfSafe()
    {
        using var files = new InputFiles();
        File.WriteAllText(files.Payload, "Different harmless bytes");
        var result = Run("simulate", "--catalog", files.Catalog, "--file", files.Payload, "--json");
        using var document = JsonDocument.Parse(result.Output);
        Assert.AreEqual(0, result.Code);
        Assert.AreEqual("NoMatchingRule", document.RootElement.GetProperty("outcome").GetString());
        StringAssert.Contains(document.RootElement.GetProperty("explanation").GetString()!, "does not mean safe");
    }

    [TestMethod]
    public void OversizedCatalogFailsWithoutAResult()
    {
        using var files = new InputFiles();
        File.WriteAllBytes(files.Catalog, new byte[1_048_577]);
        var result = Run("simulate", "--catalog", files.Catalog, "--file", files.Payload);
        Assert.AreEqual(3, result.Code);
        Assert.AreEqual(string.Empty, result.Output);
        StringAssert.Contains(result.Error, "1 MiB");
    }

    [TestMethod]
    public void MalformedCatalogDoesNotEchoInputInDiagnostics()
    {
        using var files = new InputFiles();
        File.WriteAllText(files.Catalog, "private input that is not JSON");
        var result = Run("simulate", "--catalog", files.Catalog, "--file", files.Payload);
        Assert.AreEqual(3, result.Code);
        Assert.IsFalse(result.Error.Contains("private input", StringComparison.Ordinal));
        Assert.AreEqual(string.Empty, result.Output);
    }

    [TestMethod]
    public void MissingFileFailsWithoutClaimingProtection()
    {
        using var files = new InputFiles();
        File.Delete(files.Payload);
        var result = Run("simulate", "--catalog", files.Catalog, "--file", files.Payload);
        Assert.AreEqual(3, result.Code);
        Assert.AreEqual(string.Empty, result.Output);
        Assert.IsFalse(result.Error.Contains(files.DirectoryPath, StringComparison.Ordinal));
    }

    [TestMethod]
    public void DuplicateOptionsAndUnexpectedArgumentsAreUsageErrors()
    {
        Assert.AreEqual(2, Run("capabilities", "--json", "extra").Code);
        Assert.AreEqual(2, Run("simulate", "--file", "a", "--file", "b", "--catalog", "c").Code);
        Assert.AreEqual(2, Run("simulate", "--file", "a", "--catalog").Code);
        Assert.AreEqual(2, Run("simulate", "--file", "a", "--catalog", "c", "--json", "--json").Code);
    }

    [TestMethod]
    public void WindowsNetworkAndDevicePathsAreRejectedBeforeReading()
    {
        if (!OperatingSystem.IsWindows()) { return; }
        using var files = new InputFiles();
        foreach (var input in new[] { @"\\example.invalid\share\file.exe", @"\\.\pipe\RefundScamBlocker.Test", @"\\?\C:\device.txt" })
        {
            Assert.AreEqual(3, Run("simulate", "--catalog", files.Catalog, "--file", input).Code);
        }
    }

    private static (int Code, string Output, string Error) Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = CliApplication.Run(args, output, error);
        return (code, output.ToString(), error.ToString());
    }

    private sealed class InputFiles : IDisposable
    {
        public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "RefundScamBlocker.Tests", Guid.NewGuid().ToString("N"));
        public string Payload => Path.Combine(DirectoryPath, "harmless.txt");
        public string Catalog => Path.Combine(DirectoryPath, "catalog.json");
        public byte[] OriginalBytes { get; } = "Harmless simulation fixture. Never execute.\n"u8.ToArray();

        public InputFiles()
        {
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllBytes(Payload, OriginalBytes);
            File.WriteAllText(Catalog, JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                catalogVersion = "test-1",
                rules = new[] { new { id = "synthetic", product = "Test fixture", artifactSha256 = Convert.ToHexString(SHA256.HashData(OriginalBytes)), reason = "Simulation test only" } }
            }));
        }

        public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);
    }
}
