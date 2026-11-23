using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Descriptions.Abstractions;

namespace Fdw.Services.Descriptions;

/// <summary>
/// Writes descriptions inside a unit of work that the owning domain's provider opened, so the copy in the descriptions table commits or rolls back with the
/// write of the thing it describes (FDW-6121). Every read it makes is a read on the unit.
/// </summary>
/// <remarks>
/// Why a second contract beside <see cref="IDescriptionService"/>: the service contract lives in a package every domain can reference, and a unit of work is a
/// type of <c>Fdw.Services</c>, which that package does not reference. The same class implements both.
/// </remarks>
public interface IDescriptionWriter
{
    /// <summary>Saves one description into the unit, replacing the text it holds under the same key.</summary>
    /// <param name="key">What names the description.</param>
    /// <param name="thingName">The name of the thing described.</param>
    /// <param name="owner">The thing's owner when its type has one.</param>
    /// <param name="text">The text, which is never empty.</param>
    /// <param name="unit">The unit the write runs in.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or the reason the key, the names or the text was refused or the save failed.</returns>
    Task<IGenericResult> Save(
        DescriptionKey key, string thingName, DescriptionOwner? owner, string text, ConfigurationWriteScope unit, CancellationToken cancellationToken = default);

    /// <summary>Removes every description of one thing, in every sub-type and language, from the unit. A thing that has none is not a failure.</summary>
    /// <param name="type">The name of a <see cref="DescriptionTypes"/> option.</param>
    /// <param name="thingId">The logical Id of the thing described.</param>
    /// <param name="unit">The unit the removal runs in.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or the reason the type or the thing was refused or a removal failed.</returns>
    Task<IGenericResult> DeleteAbout(string type, Guid thingId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default);

    /// <summary>Removes one description from the unit. A key that holds nothing is not a failure: a thing that never had a text has nothing to remove.</summary>
    /// <param name="key">What names the description.</param>
    /// <param name="unit">The unit the removal runs in.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or the reason the key was refused or a removal failed.</returns>
    Task<IGenericResult> Delete(DescriptionKey key, ConfigurationWriteScope unit, CancellationToken cancellationToken = default);

    /// <summary>Removes every description of one owner's children, in every sub-type and language, from the unit: the owner is going.</summary>
    /// <param name="type">The name of the child <see cref="DescriptionTypes"/> option.</param>
    /// <param name="ownerId">The owner's logical Id.</param>
    /// <param name="unit">The unit the removal runs in.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or the reason the type or the owner was refused or a removal failed.</returns>
    Task<IGenericResult> DeleteOwnedBy(string type, Guid ownerId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes, from the unit, the descriptions of one sub-type in one language of the owner's children that are not among the children named: the owner was saved
    /// again with fewer children, or a child's text was cleared.
    /// </summary>
    /// <param name="type">The name of the child <see cref="DescriptionTypes"/> option.</param>
    /// <param name="subType">The name of the <see cref="DescriptionSubTypes"/> option the save keeps in step.</param>
    /// <param name="language">The language the save keeps in step; the same child's text in another language stays.</param>
    /// <param name="ownerId">The owner's logical Id.</param>
    /// <param name="keepThingIds">The children whose descriptions stay.</param>
    /// <param name="unit">The unit the removal runs in.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or the reason a part of the key was refused or a removal failed.</returns>
    Task<IGenericResult> DeleteOwnedExcept(
        string type, string subType, string language, Guid ownerId, IReadOnlyCollection<Guid> keepThingIds, ConfigurationWriteScope unit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Keeps the description of one thing of a type that has no owner in step with a save of the thing, in the save's unit (FDW-6208): the text the save carries
    /// is saved in the language of today, and a save that carries none removes the description the thing had, so none is invented and none is left behind.
    /// </summary>
    /// <param name="type">The name of a <see cref="DescriptionTypes"/> option whose things have no owner.</param>
    /// <param name="subType">The name of a <see cref="DescriptionSubTypes"/> option.</param>
    /// <param name="thingId">The logical Id of the thing.</param>
    /// <param name="thingName">The name of the thing.</param>
    /// <param name="text">The text the save carries; empty or blank removes the description.</param>
    /// <param name="unit">The unit of the write of the thing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or the reason the language was not stated, a part of the key was refused or the write failed.</returns>
    Task<IGenericResult> Describe(
        string type, string subType, Guid thingId, string thingName, string? text, ConfigurationWriteScope unit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Keeps the descriptions of every thing of one type an owner holds in step with a save of the owner, in the save's unit (FDW-6589): one read of what the owner
    /// holds now, then a write only for a thing whose text, name or owner name changed, and a removal for a thing whose text is now empty or that the owner no
    /// longer holds. A save that changed no description writes no row.
    /// </summary>
    /// <remarks>
    /// Why this and not <see cref="Save(DescriptionKey, string, DescriptionOwner?, string, ConfigurationWriteScope, CancellationToken)"/> for each thing: a data store
    /// holds thousands of fields, and most of its saves (paging, classification, data types) change no description. The changed ones are still written one
    /// description at a time, because the configuration write has no set-based save.
    /// </remarks>
    /// <param name="type">The name of a <see cref="DescriptionTypes"/> option whose things have an owner.</param>
    /// <param name="subType">The name of a <see cref="DescriptionSubTypes"/> option.</param>
    /// <param name="owner">The owner, whose Id and name every description of its things carries.</param>
    /// <param name="things">Every thing of the type the owner holds after the save, with the text the save carries for it.</param>
    /// <param name="unit">The unit of the write of the owner.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or the reason the language was not stated, a part of the key or a name was refused, or the read or a write failed.</returns>
    Task<IGenericResult> DescribeOwned(
        string type, string subType, DescriptionOwner owner, IReadOnlyList<OwnedDescription> things, ConfigurationWriteScope unit, CancellationToken cancellationToken = default);

    /// <summary>Gets the language tag the descriptions of today are written in; the same answer as <see cref="IDescriptionService.Language"/>.</summary>
    /// <returns>The language tag, or a failure naming the setting when nobody stated one.</returns>
    IGenericResult<string> Language();
}
