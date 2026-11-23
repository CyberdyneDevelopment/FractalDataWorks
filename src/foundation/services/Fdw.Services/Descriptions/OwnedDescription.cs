using System;

namespace Fdw.Services.Descriptions;

/// <summary>
/// One thing an owner holds, with the text its owner's save carries for it, handed to <see cref="IDescriptionWriter.DescribeOwned"/> (FDW-6589): a data store's
/// field, say, with the description the store's save gives it.
/// </summary>
/// <param name="ThingId">The logical Id of the thing.</param>
/// <param name="ThingName">The name of the thing.</param>
/// <param name="Text">The text the save carries; empty or blank removes the description the thing had.</param>
public sealed record OwnedDescription(Guid ThingId, string ThingName, string? Text);
