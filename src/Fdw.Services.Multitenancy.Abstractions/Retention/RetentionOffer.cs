using System.Collections.Generic;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// What an application's documents let a retention policy name (FDW-2906).
/// </summary>
/// <param name="StartFields">The names of the dates a document carries that a period can count from (for example <c>ModifiedDate</c>).</param>
/// <param name="EndActions">The actions the application can take on a document whose period is over (for example <c>Delete</c> or <c>Archive</c>).</param>
public sealed record RetentionOffer(IReadOnlyList<string> StartFields, IReadOnlyList<string> EndActions);
