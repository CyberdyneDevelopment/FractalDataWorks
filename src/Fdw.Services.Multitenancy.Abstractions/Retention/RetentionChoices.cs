using System.Collections.Generic;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// What an application's documents offer an organization when it sets a retention policy (FDW-2906).
/// </summary>
/// <param name="StartFields">The names of the dates a document carries that a period can count from (for example <c>ModifiedDate</c>).</param>
/// <param name="EndActions">The actions the application can take on a document whose period is over (for example <c>Delete</c> or <c>Archive</c>).</param>
/// <param name="DocumentTypes">The document types the organization has documents of, as named in a retention policy.</param>
public sealed record RetentionChoices(
    IReadOnlyList<string> StartFields, IReadOnlyList<string> EndActions, IReadOnlyList<string> DocumentTypes);
