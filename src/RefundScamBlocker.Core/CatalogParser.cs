using System.Text;
using System.Text.Json;

namespace RefundScamBlocker.Core;

/// <summary>Parses a bounded, strict version 1 JSON catalog without external access.</summary>
public static class CatalogParser
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public const int MaximumCatalogBytes = 1_048_576;
    public const int MaximumRules = 10_000;
    public const int MaximumCatalogVersionLength = 64;
    public const int MaximumRuleIdLength = 80;
    public const int MaximumProductLength = 200;
    public const int MaximumReasonLength = 1_000;

    /// <summary>
    /// Validates untrusted UTF-8 JSON. Unknown and duplicate properties are rejected.
    /// No policy is installed and no protection state is inferred from the catalog.
    /// </summary>
    public static RuleCatalog Parse(ReadOnlySpan<byte> utf8Json)
    {
        if (utf8Json.IsEmpty || utf8Json.Length > MaximumCatalogBytes)
        {
            throw new CatalogValidationException($"Catalog must contain 1 to {MaximumCatalogBytes} UTF-8 bytes.");
        }

        try
        {
            _ = StrictUtf8.GetCharCount(utf8Json);
            using JsonDocument document = JsonDocument.Parse(utf8Json.ToArray(), new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 16,
            });

            JsonElement root = document.RootElement;
            RequireProperties(root, "catalog", "schemaVersion", "catalogVersion", "rules");

            JsonElement schemaVersion = root.GetProperty("schemaVersion");
            if (schemaVersion.ValueKind != JsonValueKind.Number ||
                !schemaVersion.TryGetInt32(out int version) || version != 1)
            {
                throw new CatalogValidationException("schemaVersion must be the integer 1.");
            }

            string catalogVersion = ReadString(root, "catalogVersion", MaximumCatalogVersionLength);
            JsonElement ruleArray = root.GetProperty("rules");
            if (ruleArray.ValueKind != JsonValueKind.Array || ruleArray.GetArrayLength() > MaximumRules)
            {
                throw new CatalogValidationException($"rules must be an array containing at most {MaximumRules} entries.");
            }

            List<HashRule> rules = new(ruleArray.GetArrayLength());
            HashSet<string> ruleIds = new(StringComparer.OrdinalIgnoreCase);
            foreach (JsonElement rule in ruleArray.EnumerateArray())
            {
                RequireProperties(rule, "rule", "id", "product", "artifactSha256", "reason");
                string id = ReadString(rule, "id", MaximumRuleIdLength);
                if (!id.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-'))
                {
                    throw new CatalogValidationException("Rule id may contain only ASCII letters, digits, '.', '_' and '-'.");
                }

                if (!ruleIds.Add(id))
                {
                    throw new CatalogValidationException("Rule ids must be unique (case-insensitive).");
                }

                string product = ReadString(rule, "product", MaximumProductLength);
                string hash = ReadString(rule, "artifactSha256", 64);
                if (!Sha256Identity.IsValid(hash))
                {
                    throw new CatalogValidationException("artifactSha256 must contain exactly 64 hexadecimal characters.");
                }

                string reason = ReadString(rule, "reason", MaximumReasonLength);
                rules.Add(new HashRule(id, product, hash.ToUpperInvariant(), reason));
            }

            return new RuleCatalog(catalogVersion, rules.AsReadOnly());
        }
        catch (JsonException exception)
        {
            throw new CatalogValidationException("Catalog must be valid JSON with a maximum nesting depth of 16.", exception);
        }
        catch (DecoderFallbackException exception)
        {
            throw new CatalogValidationException("Catalog must contain valid UTF-8.", exception);
        }
        catch (InvalidOperationException exception)
        {
            // JsonDocument defers decoding strings; an unpaired escaped UTF-16
            // surrogate can fail here even when the original bytes were valid UTF-8.
            throw new CatalogValidationException("Catalog contains an invalid JSON string.", exception);
        }
    }

    private static void RequireProperties(JsonElement element, string context, params string[] expected)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new CatalogValidationException($"Each {context} must be a JSON object.");
        }

        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!expected.Contains(property.Name, StringComparer.Ordinal))
            {
                throw new CatalogValidationException($"Unknown property in {context}.");
            }

            if (!seen.Add(property.Name))
            {
                throw new CatalogValidationException($"Duplicate property in {context}.");
            }
        }

        if (seen.Count != expected.Length)
        {
            throw new CatalogValidationException($"Missing required property in {context}.");
        }
    }

    private static string ReadString(JsonElement element, string name, int maximumLength)
    {
        JsonElement value = element.GetProperty(name);
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new CatalogValidationException($"{name} must be a string.");
        }

        string text = value.GetString()!;
        if (string.IsNullOrWhiteSpace(text) || text.Length > maximumLength ||
            text != text.Trim() || text.Any(char.IsControl))
        {
            throw new CatalogValidationException($"{name} must be nonempty, at most {maximumLength} characters, with no control characters or surrounding whitespace.");
        }

        return text;
    }
}
