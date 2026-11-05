using System;

namespace Fdw.Services.Tags;

/// <summary>One owner a tag can be given to: its kind (the name of a <c>TagOwnerKinds</c> option), its Id and its name.</summary>
/// <param name="Kind">The owner kind, exactly as a tag stores it.</param>
/// <param name="Id">The owner's Id.</param>
/// <param name="Name">The owner's name as a person reads it.</param>
public sealed record TagOwner(string Kind, Guid Id, string Name);
