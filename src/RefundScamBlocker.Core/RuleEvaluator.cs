namespace RefundScamBlocker.Core;

/// <summary>Simulation outcomes; neither outcome reports actual Windows protection.</summary>
public enum EvaluationOutcome
{
    NoMatchingRule,
    WouldBlock,
}

public sealed class RuleEvaluation
{
    internal RuleEvaluation(EvaluationOutcome outcome, IReadOnlyList<HashRule> matches)
    {
        Outcome = outcome;
        Matches = matches;
    }

    public EvaluationOutcome Outcome { get; }

    public IReadOnlyList<HashRule> Matches { get; }
}

/// <summary>Compares artifact hashes locally. Does not execute or modify the artifact.</summary>
public static class RuleEvaluator
{
    public static RuleEvaluation Evaluate(RuleCatalog catalog, string artifactSha256)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrEmpty(artifactSha256);
        if (!Sha256Identity.IsValid(artifactSha256))
        {
            throw new ArgumentException("Identity must contain exactly 64 hexadecimal characters.", nameof(artifactSha256));
        }

        List<HashRule> matches = [];
        foreach (HashRule rule in catalog.Rules)
        {
            if (string.Equals(rule.ArtifactSha256, artifactSha256, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(rule);
            }
        }

        EvaluationOutcome outcome = matches.Count > 0 ? EvaluationOutcome.WouldBlock : EvaluationOutcome.NoMatchingRule;
        return new RuleEvaluation(outcome, matches.AsReadOnly());
    }
}
