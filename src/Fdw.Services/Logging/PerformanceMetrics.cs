namespace Fdw.Services.Logging;

/// <summary>
/// Record for capturing performance metrics with Serilog destructuring support.
/// Serilog will automatically destructure this record for structured logging.
/// </summary>
public record PerformanceMetrics(
    double Duration,
    int ItemsProcessed,
    string OperationType,
    string? SensitiveData = null)
{
    /// <summary>
    /// Override ToString to provide clean string representation while preserving structured data.
    /// Serilog will still capture all properties when using @ destructuring.
    /// </summary>
    public override string ToString() =>
        $"Duration: {Duration}ms, Items: {ItemsProcessed}, Type: {OperationType}";
}
