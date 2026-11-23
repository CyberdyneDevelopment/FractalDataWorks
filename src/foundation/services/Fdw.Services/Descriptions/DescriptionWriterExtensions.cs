using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Descriptions.Abstractions;

namespace Fdw.Services.Descriptions;

/// <summary>The write a record makes of a description it never fills on load (FDW-6208).</summary>
/// <remarks>
/// A record whose provider does not fill <c>Description</c> when it loads the record (the settings, themes, authentication flows and token managers) carries a null
/// text when a caller changes another field and saves it. That null is "nothing was said", not "remove it": the stored text is kept. A text the caller states is written,
/// and a stated blank removes the description.
/// </remarks>
public static class DescriptionWriterExtensions
{
    /// <summary>Writes the text a save states: null keeps what is stored, a non-blank text is written, a blank text removes the description.</summary>
    /// <param name="writer">The description writer.</param>
    /// <param name="type">The name of a description type whose things have no owner.</param>
    /// <param name="subType">The name of a description sub-type.</param>
    /// <param name="thingId">The logical Id of the thing.</param>
    /// <param name="thingName">The name of the thing.</param>
    /// <param name="text">The text the save carries; null states nothing.</param>
    /// <param name="unit">The unit of the write of the thing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success when the save states no text or the write succeeded; otherwise the writer's failure.</returns>
    public static Task<IGenericResult> DescribeWhenStated(
        this IDescriptionWriter writer, string type, string subType, Guid thingId, string thingName, string? text, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => text is null
            ? Task.FromResult<IGenericResult>(GenericResult.Success())
            : writer.Describe(type, subType, thingId, thingName, text, unit, cancellationToken);

    /// <summary>Writes the text a save states in a language that was resolved BEFORE the unit opened (FDW-7256): null keeps what is stored, a non-blank text is saved, a blank one removes it.</summary>
    /// <remarks>
    /// Why the language is a parameter: <see cref="IDescriptionWriter.Describe"/> reads it from the settings, and a provider that writes the settings tables reads them on another
    /// connection while its own unit holds locks on them. The key is made here and the unit's <c>Save</c> and <c>Delete</c> are given it, so nothing in the unit reads a setting.
    /// </remarks>
    /// <param name="writer">The description writer.</param>
    /// <param name="language">The language the provider got from <see cref="IDescriptionWriter.Language"/> before its unit opened.</param>
    /// <param name="type">The name of a description type whose things have no owner.</param>
    /// <param name="subType">The name of a description sub-type.</param>
    /// <param name="thingId">The logical Id of the thing.</param>
    /// <param name="thingName">The name of the thing.</param>
    /// <param name="text">The text the save carries; null states nothing.</param>
    /// <param name="unit">The unit of the write of the thing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success when the save states no text or the write succeeded; otherwise the writer's failure.</returns>
    public static Task<IGenericResult> DescribeInWhenStated(
        this IDescriptionWriter writer, string language, string type, string subType, Guid thingId, string thingName, string? text, ConfigurationWriteScope unit,
        CancellationToken cancellationToken = default)
        => text is null
            ? Task.FromResult<IGenericResult>(GenericResult.Success())
            : string.IsNullOrWhiteSpace(text)
                ? writer.Delete(new DescriptionKey(type, subType, thingId, language), unit, cancellationToken)
                : writer.Save(new DescriptionKey(type, subType, thingId, language), thingName, null, text, unit, cancellationToken);
}
