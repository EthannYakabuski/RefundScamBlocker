namespace RefundScamBlocker.Core;

/// <summary>A validated catalog of exact artifact hashes for offline simulation.</summary>
public sealed class RuleCatalog
{
    internal RuleCatalog(string catalogVersion, IReadOnlyList<HashRule> rules)
    {
        CatalogVersion = catalogVersion;
        Rules = rules;
    }

    public int SchemaVersion => 1;

    public string CatalogVersion { get; }

    public IReadOnlyList<HashRule> Rules { get; }
}

/// <summary>A synthetic artifact identity. A filename is never a match criterion.</summary>
public sealed class HashRule
{
    internal HashRule(string id, string product, string artifactSha256, string reason)
    {
        Id = id;
        Product = product;
        ArtifactSha256 = artifactSha256;
        Reason = reason;
    }

    public string Id { get; }

    public string Product { get; }

    public string ArtifactSha256 { get; }

    public string Reason { get; }
}
