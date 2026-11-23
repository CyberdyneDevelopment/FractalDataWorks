using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution.Answering;

/// <summary>A row of <c>pipe.AnswerPolicy</c> (FDW-6142), as the configuration gateway reads it. It carries no tenant: the row-level security of the tenant filters what is read.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class AnswerPolicyRecord
{
    /// <summary>Gets or sets the policy's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the policy's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets how long a request under the policy stays open, in seconds.</summary>
    public int ExpiresAfterSeconds { get; set; }

    /// <summary>Gets or sets a value indicating whether this is the current version of the row.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the row is retired.</summary>
    public bool IsDeleted { get; set; }
}
