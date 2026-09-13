namespace RefundScamBlocker.Core;

internal static class Sha256Identity
{
    internal static bool IsValid(string? value) =>
        value is { Length: 64 } && value.All(char.IsAsciiHexDigit);
}
