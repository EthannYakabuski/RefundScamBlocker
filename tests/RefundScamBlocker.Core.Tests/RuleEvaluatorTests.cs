using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RefundScamBlocker.Core.Tests;

[TestClass]
public sealed class RuleEvaluatorTests
{
    [TestMethod]
    public void CommittedHarmlessFixtureMatchesExampleCatalog()
    {
        RuleCatalog catalog = CatalogParser.Parse(File.ReadAllBytes(FixturePath("synthetic-catalog.json")));
        string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(FixturePath("harmless-demo.txt"))));

        RuleEvaluation result = RuleEvaluator.Evaluate(catalog, hash);

        Assert.AreEqual(EvaluationOutcome.WouldBlock, result.Outcome);
        Assert.HasCount(1, result.Matches);
        Assert.AreEqual("synthetic.harmless-demo", result.Matches[0].Id);
    }

    [TestMethod]
    public void ChangedContentDoesNotMatchDespiteSameFilename()
    {
        RuleCatalog catalog = CatalogParser.Parse(File.ReadAllBytes(FixturePath("synthetic-catalog.json")));
        byte[] fixture = File.ReadAllBytes(FixturePath("harmless-demo.txt"));
        fixture[0] ^= 1;

        RuleEvaluation result = RuleEvaluator.Evaluate(catalog, Convert.ToHexString(SHA256.HashData(fixture)));

        Assert.AreEqual(EvaluationOutcome.NoMatchingRule, result.Outcome);
        Assert.IsEmpty(result.Matches);
    }

    [TestMethod]
    public void RenamedIdenticalFileStillMatches()
    {
        RuleCatalog catalog = CatalogParser.Parse(File.ReadAllBytes(FixturePath("synthetic-catalog.json")));
        string renamedPath = Path.Combine(Path.GetTempPath(), $"rsb-test-renamed-{Guid.NewGuid():N}.txt");
        try
        {
            File.Copy(FixturePath("harmless-demo.txt"), renamedPath);
            using FileStream input = File.OpenRead(renamedPath);

            Assert.AreEqual(EvaluationOutcome.WouldBlock,
                RuleEvaluator.Evaluate(catalog, Convert.ToHexString(SHA256.HashData(input))).Outcome);
        }
        finally
        {
            File.Delete(renamedPath);
        }
    }

    [TestMethod]
    public void HashComparisonIgnoresCase()
    {
        RuleCatalog catalog = CatalogParserTests.Parse(CatalogParserTests.Catalog(CatalogParserTests.Rule()));

        Assert.AreEqual(EvaluationOutcome.WouldBlock,
            RuleEvaluator.Evaluate(catalog, CatalogParserTests.TestHash.ToLowerInvariant()).Outcome);
    }

    [TestMethod]
    public void MatchesPreserveCatalogOrderAndDoNotChangeCatalog()
    {
        RuleCatalog catalog = CatalogParserTests.Parse(CatalogParserTests.Catalog(
            CatalogParserTests.Rule("synthetic.first"),
            CatalogParserTests.Rule("synthetic.unrelated", new string('0', 64)),
            CatalogParserTests.Rule("synthetic.second")));

        RuleEvaluation result = RuleEvaluator.Evaluate(catalog, CatalogParserTests.TestHash);

        CollectionAssert.AreEqual(new[] { "synthetic.first", "synthetic.second" }, result.Matches.Select(rule => rule.Id).ToArray());
        Assert.HasCount(3, catalog.Rules);
        Assert.AreEqual(EvaluationOutcome.NoMatchingRule, RuleEvaluator.Evaluate(catalog, new string('1', 64)).Outcome);
    }

    [TestMethod]
    public void InvalidIdentityAndNullCatalogAreRejected()
    {
        RuleCatalog catalog = CatalogParserTests.Parse(CatalogParserTests.Catalog());

        Assert.ThrowsExactly<ArgumentException>(() => RuleEvaluator.Evaluate(catalog, "remote-support.exe"));
        Assert.ThrowsExactly<ArgumentException>(() => RuleEvaluator.Evaluate(catalog, new string('Z', 64)));
        Assert.ThrowsExactly<ArgumentNullException>(() => RuleEvaluator.Evaluate(catalog, null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => RuleEvaluator.Evaluate(null!, CatalogParserTests.TestHash));
    }

    private static string FixturePath(string filename) => Path.Combine(AppContext.BaseDirectory, "fixtures", filename);
}
