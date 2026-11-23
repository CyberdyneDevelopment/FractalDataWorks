using System;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>One stored version of the description of a thing: the text it held, when it was written and whether the version is the removal of the description.</summary>
/// <param name="Text">The text the version held.</param>
/// <param name="WrittenAt">When the version was written.</param>
/// <param name="IsDeleted">True when the version marks the description removed.</param>
public sealed record DescriptionVersion(string Text, DateTimeOffset WrittenAt, bool IsDeleted);
