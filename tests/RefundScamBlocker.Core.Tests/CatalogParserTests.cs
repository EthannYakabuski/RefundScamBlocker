using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RefundScamBlocker.Core.Tests;

[TestClass]
public sealed class CatalogParserTests
{
    internal const string TestHash = "FA40E9651CB108276A79E6E374AA4044B0A17493D578D50E54A035D40D1D9012";

    internal static string Rule(string id = "synthetic.test", string hash = TestHash) => JsonSerializer.Serialize(new
    {
        id,
        product = "Harmless synthetic fixture",
        artifactSha256 = hash,
        reason = "Test only; no protection is installed.",
    });

    internal static string Catalog(params string[] rules) =>
        "{\"schemaVersion\":1,\"catalogVersion\":\"synthetic-1\",\"rules\":[" + string.Join(',', rules) + "]}";

    internal static RuleCatalog Parse(string json) => CatalogParser.Parse(Encoding.UTF8.GetBytes(json));

    [TestMethod]
    public void ValidCatalogExposesValidatedMetadata()
    {
        RuleCatalog catalog = Parse(Catalog(Rule(hash: TestHash.ToLowerInvariant())));

        Assert.AreEqual(1, catalog.SchemaVersion);
        Assert.AreEqual("synthetic-1", catalog.CatalogVersion);
        Assert.HasCount(1, catalog.Rules);
        Assert.AreEqual(TestHash, catalog.Rules[0].ArtifactSha256);
    }

    [TestMethod]
    public void EmptyRuleArrayIsValidWithoutImplyingCoverage()
    {
        RuleCatalog catalog = Parse(Catalog());

        Assert.IsEmpty(catalog.Rules);
        Assert.AreEqual(EvaluationOutcome.NoMatchingRule, RuleEvaluator.Evaluate(catalog, TestHash).Outcome);
    }

    [TestMethod]
    [DataRow("null")]
    [DataRow("[]")]
    [DataRow("{}")]
    [DataRow("{\"schemaVersion\":1,\"catalogVersion\":\"v1\"}")]
    [DataRow("{\"schemaVersion\":1,\"catalogVersion\":\"v1\",\"rules\":null}")]
    [DataRow("{\"schemaVersion\":1,\"catalogVersion\":\"v1\",\"rules\":[null]}")]
    [DataRow("{\"schemaVersion\":1,\"catalogVersion\":\"v1\",\"rules\":[]/*comment*/}")]
    [DataRow("{\"schemaVersion\":1,\"catalogVersion\":\"v1\",\"rules\":[],}")]
    public void InvalidStructureIsRejected(string json)
    {
        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(json));
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("2")]
    [DataRow("1.0")]
    [DataRow("\"1\"")]
    [DataRow("null")]
    [DataRow("true")]
    public void UnsupportedOrNonIntegerSchemaVersionIsRejected(string version)
    {
        string json = Catalog().Replace("\"schemaVersion\":1", "\"schemaVersion\":" + version, StringComparison.Ordinal);

        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(json));
    }

    [TestMethod]
    [DataRow("", false)]
    [DataRow("1234", false)]
    [DataRow("GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG", false)]
    [DataRow("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", false)]
    [DataRow("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)]
    public void HashMustBeExactly64HexadecimalCharacters(string hash, bool isValid)
    {
        if (isValid)
        {
            Assert.HasCount(1, Parse(Catalog(Rule(hash: hash))).Rules);
        }
        else
        {
            Assert.ThrowsExactly<CatalogValidationException>(() => Parse(Catalog(Rule(hash: hash))));
        }
    }

    [TestMethod]
    [DataRow("synthetic.test")]
    [DataRow("SYNTHETIC.TEST")]
    public void DuplicateRuleIdsAreRejectedRegardlessOfCase(string secondId)
    {
        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(Catalog(Rule(), Rule(secondId))));
    }

    [TestMethod]
    [DataRow("schemaVersion")]
    [DataRow("catalogVersion")]
    [DataRow("rules")]
    public void DuplicateRootPropertiesAreRejected(string property)
    {
        string json = Catalog().Insert(1, JsonSerializer.Serialize(property) + ":null,");

        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(json));
    }

    [TestMethod]
    public void DuplicateRulePropertiesAreRejected()
    {
        string rule = Rule().Insert(1, "\"id\":\"another.id\",");

        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(Catalog(rule)));
    }

    [TestMethod]
    public void UnknownRootAndRulePropertiesAreRejected()
    {
        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(Catalog().Insert(1, "\"disableProtection\":true,")));
        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(Catalog(Rule().Insert(1, "\"filename\":\"anything.exe\","))));
    }

    [TestMethod]
    public void PropertyNamesAreCaseSensitive()
    {
        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(Catalog().Replace("schemaVersion", "SchemaVersion", StringComparison.Ordinal)));
    }

    [TestMethod]
    [DataRow("id")]
    [DataRow("product")]
    [DataRow("artifactSha256")]
    [DataRow("reason")]
    public void NullOrMissingRuleFieldsAreRejected(string field)
    {
        Dictionary<string, object?> rule = new()
        {
            ["id"] = "synthetic.test",
            ["product"] = "Harmless fixture",
            ["artifactSha256"] = TestHash,
            ["reason"] = "Simulation only",
        };
        rule[field] = null;
        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(Catalog(JsonSerializer.Serialize(rule))));
        rule.Remove(field);
        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(Catalog(JsonSerializer.Serialize(rule))));
    }

    [TestMethod]
    [DataRow(" ")]
    [DataRow(" padded")]
    [DataRow("padded ")]
    [DataRow("has space")]
    [DataRow("path/like")]
    [DataRow("nonascii-é")]
    [DataRow("line\nbreak")]
    public void UnsafeOrAmbiguousRuleIdsAreRejected(string id)
    {
        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(Catalog(Rule(id))));
    }

    [TestMethod]
    public void StringLengthLimitsAreEnforced()
    {
        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(Catalog(Rule(new string('a', CatalogParser.MaximumRuleIdLength + 1)))));
        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(Catalog().Replace("synthetic-1", new string('a', CatalogParser.MaximumCatalogVersionLength + 1), StringComparison.Ordinal)));
        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(Catalog(Rule().Replace("Harmless synthetic fixture", new string('a', CatalogParser.MaximumProductLength + 1), StringComparison.Ordinal))));
        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(Catalog(Rule().Replace("Test only; no protection is installed.", new string('a', CatalogParser.MaximumReasonLength + 1), StringComparison.Ordinal))));
    }

    [TestMethod]
    public void CatalogByteLimitIsCheckedBeforeParsing()
    {
        byte[] atLimit = new byte[CatalogParser.MaximumCatalogBytes];
        Array.Fill(atLimit, (byte)' ');
        Encoding.UTF8.GetBytes(Catalog()).CopyTo(atLimit, 0);
        Assert.IsEmpty(CatalogParser.Parse(atLimit).Rules);

        byte[] overLimit = new byte[CatalogParser.MaximumCatalogBytes + 1];
        CatalogValidationException exception = Assert.ThrowsExactly<CatalogValidationException>(() => CatalogParser.Parse(overLimit));
        StringAssert.Contains(exception.Message, "UTF-8 bytes");
    }

    [TestMethod]
    public void EmptyBytesAndInvalidUtf8AreRejected()
    {
        Assert.ThrowsExactly<CatalogValidationException>(() => CatalogParser.Parse([]));
        byte[] invalidUtf8 = Encoding.UTF8.GetBytes(Catalog());
        invalidUtf8[Array.IndexOf(invalidUtf8, (byte)'s')] = 0xff;
        Assert.ThrowsExactly<CatalogValidationException>(() => CatalogParser.Parse(invalidUtf8));
    }

    [TestMethod]
    public void UnpairedEscapedSurrogatesAreReportedAsCatalogValidationErrors()
    {
        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(Catalog().Replace("synthetic-1", "\\uD800", StringComparison.Ordinal)));
        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(Catalog(Rule().Replace("Harmless synthetic fixture", "\\uDC00", StringComparison.Ordinal))));
        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(Catalog().Insert(1, "\"\\uD800\":1,")));
    }

    [TestMethod]
    public void ExcessiveNestingIsRejected()
    {
        string nested = new string('[', 17) + "0" + new string(']', 17);

        Assert.ThrowsExactly<CatalogValidationException>(() => Parse(nested));
    }

    [TestMethod]
    public void RuleCountLimitIsEnforcedWithinByteLimit()
    {
        string json = Catalog(Enumerable.Repeat("{}", CatalogParser.MaximumRules + 1).ToArray());
        CatalogValidationException exception = Assert.ThrowsExactly<CatalogValidationException>(() => Parse(json));

        StringAssert.Contains(exception.Message, "entries");
    }
}
