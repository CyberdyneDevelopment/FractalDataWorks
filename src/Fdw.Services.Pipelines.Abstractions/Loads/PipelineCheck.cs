namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A check of a Validate stage (an entry of <c>checks</c>): a name and the condition a record must meet.</summary>
public sealed class PipelineCheck
{
    /// <summary>Gets or sets the check's name, unique within its stage; a rejected record is reported with it.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the condition a record must meet, in the pipeline's condition language: comparisons joined by
    /// <c>and</c> / <c>or</c>, negated by <c>not</c>, grouped by parentheses. May reference <c>{{run parameters}}</c>.
    /// </summary>
    public string Expression { get; set; } = string.Empty;
}
