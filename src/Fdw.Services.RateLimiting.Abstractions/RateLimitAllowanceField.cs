using System;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// One setting a rate limit policy's allowance is read from: its name beside the policy's, the kind of value it holds and what
/// it is for.
/// </summary>
public sealed class RateLimitAllowanceField
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitAllowanceField"/> class.
    /// </summary>
    /// <param name="name">The field's name, the last part of its setting's name.</param>
    /// <param name="dataType">The kind of value it holds: Int32, Boolean or String.</param>
    /// <param name="description">What it is for, in plain words.</param>
    public RateLimitAllowanceField(string name, string dataType, string description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    /// <summary>
    /// Gets the field's name, the last part of its setting's name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the kind of value it holds: Int32, Boolean or String.
    /// </summary>
    public string DataType { get; }

    /// <summary>
    /// Gets what it is for, in plain words.
    /// </summary>
    public string Description { get; }
}
