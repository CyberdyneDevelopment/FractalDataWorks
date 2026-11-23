using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>
/// Reads the descriptions of a whole set of things at once, in the language of today, for the provider that fills a member from them (FDW-6208).
/// </summary>
/// <remarks>
/// Why a set: a page of 580 data sets is one read of the descriptions, never 580. The things are named in chunks of the configuration gateway's key chunk size, so
/// no list of keys is cut and none is refused. A thing with no description has no entry; the member it fills stays empty, and nothing stands in for it: no other
/// language's text and no other column. The language is the setting <c>DescriptionLanguage</c> for the caller's tenant, and a read fails with the coded
/// message of <see cref="IDescriptionService.Language"/> when nobody stated one.
/// </remarks>
public interface IDescriptionReader
{
    /// <summary>Reads the descriptions of things of one type.</summary>
    /// <param name="type">The name of a <see cref="DescriptionTypes"/> option.</param>
    /// <param name="subType">The name of a <see cref="DescriptionSubTypes"/> option.</param>
    /// <param name="thingIds">The logical Ids of the things; none answers nothing.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The descriptions found, by the Id of the thing described; or the reason the type or sub-type was refused, the language not stated or the read failed.</returns>
    Task<IGenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>> ReadMany(
        string type, string subType, IReadOnlyCollection<Guid> thingIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fills the description of every thing of a read with one <see cref="ReadMany"/>, for the read observer of a domain whose things have no owner (FDW-6208).
    /// </summary>
    /// <typeparam name="T">The type of the things.</typeparam>
    /// <param name="type">The name of a <see cref="DescriptionTypes"/> option.</param>
    /// <param name="subType">The name of a <see cref="DescriptionSubTypes"/> option.</param>
    /// <param name="things">The things the read composed.</param>
    /// <param name="idOf">The logical Id of a thing.</param>
    /// <param name="fill">Sets a thing's description.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success once every thing that has a description holds it (a thing with none keeps what it has; nothing stands in for it), or the reason the read failed.</returns>
    Task<IGenericResult> Fill<T>(
        string type, string subType, IReadOnlyList<T> things, Func<T, Guid> idOf, Action<T, string> fill, CancellationToken cancellationToken = default);

    /// <summary>Finds the things of one type whose description contains a text.</summary>
    /// <param name="type">The name of a <see cref="DescriptionTypes"/> option.</param>
    /// <param name="subType">The name of a <see cref="DescriptionSubTypes"/> option.</param>
    /// <param name="text">The text to find; a text that is empty or blank finds nothing.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The logical Ids of the things found, whole and not cut at a size; or the reason the type or sub-type was refused, the language not stated or the read failed.</returns>
    Task<IGenericResult<IReadOnlyCollection<Guid>>> Find(string type, string subType, string text, CancellationToken cancellationToken = default);

    /// <summary>Reads every stored version of the description of one thing in the language of today, oldest first, for a history that shows how the text changed.</summary>
    /// <param name="type">The name of a <see cref="DescriptionTypes"/> option.</param>
    /// <param name="subType">The name of a <see cref="DescriptionSubTypes"/> option.</param>
    /// <param name="thingId">The logical Id of the thing.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The versions, none for a thing that never had a description; or the reason the type or sub-type was refused, the language not stated or the read failed.</returns>
    Task<IGenericResult<IReadOnlyList<DescriptionVersion>>> Versions(string type, string subType, Guid thingId, CancellationToken cancellationToken = default);

    /// <summary>Reads the descriptions of the things that belong to the owners named, such as the fields of a set of data sets.</summary>
    /// <param name="type">The name of a <see cref="DescriptionTypes"/> option whose things have an owner.</param>
    /// <param name="subType">The name of a <see cref="DescriptionSubTypes"/> option.</param>
    /// <param name="ownerIds">The logical Ids of the owners; none answers nothing.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The descriptions found, by the Id of the thing described; or the reason the type or sub-type was refused, the language not stated or the read failed.</returns>
    Task<IGenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>> ReadOwnedBy(
        string type, string subType, IReadOnlyCollection<Guid> ownerIds, CancellationToken cancellationToken = default);
}
