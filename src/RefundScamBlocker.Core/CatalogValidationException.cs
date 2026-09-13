namespace RefundScamBlocker.Core;

/// <summary>Indicates that a catalog could not be accepted for simulation.</summary>
public sealed class CatalogValidationException : Exception
{
    public CatalogValidationException(string message)
        : base(message)
    {
    }

    public CatalogValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
