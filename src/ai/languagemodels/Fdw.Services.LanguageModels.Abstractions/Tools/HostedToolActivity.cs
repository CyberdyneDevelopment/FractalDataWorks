
namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>What a tool the provider runs itself did, and what it found.</summary>
public sealed class HostedToolActivity
{
    /// <summary>Initializes a new instance of the <see cref="HostedToolActivity"/> class.</summary>
    /// <param name="kind">The kind of hosted tool.</param>
    /// <param name="description">What the tool did, for example the query it ran.</param>
    /// <param name="result">What it found, or <see langword="null"/> while it is still running.</param>
    public HostedToolActivity(IHostedToolKind kind, string description, string? result)
    {
        Kind = kind;
        Description = description;
        Result = result;
    }

    /// <summary>Gets the kind of hosted tool.</summary>
    public IHostedToolKind Kind { get; }

    /// <summary>Gets what the tool did.</summary>
    public string Description { get; }

    /// <summary>Gets what the tool found, or <see langword="null"/> while it is still running.</summary>
    public string? Result { get; }
}
