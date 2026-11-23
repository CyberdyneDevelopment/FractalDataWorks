using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>
/// Saves and deletes the descriptions held in the descriptions table.
/// </summary>
/// <remarks>
/// The owning domain's provider is the one writer: it calls <see cref="Save"/> and <see cref="Delete"/> in the same write as its own save. For a domain whose
/// <c>Description</c> column is gone (FDW-6208) the table is the only home of the text and the record's member is filled from it on every read; for a domain not yet
/// moved it is a copy that cannot drift from the column. Nothing here is defaulted: a missing type, sub-type, thing, language or text is a
/// failed result that names it.
/// A description longer than the part length is stored as parts (FDW-6123), and a save replaces all of them in one unit.
/// </remarks>
public interface IDescriptionService
{
    /// <summary>Gets the language tag the descriptions of today are written in: the setting <c>DescriptionLanguage</c> as it resolves for the caller's tenant.</summary>
    /// <returns>The language tag, or a failure naming the setting when nobody stated one.</returns>
    IGenericResult<string> Language();

    /// <summary>Saves the text of one description, replacing the text it holds under the same key.</summary>
    /// <param name="key">What names the description.</param>
    /// <param name="thingName">The name of the thing described, which a search shows beside the text. Never empty.</param>
    /// <param name="owner">The thing's owner (Id and name) when the type has one (<see cref="IDescriptionType.HasOwner"/>), and nothing when it has not.</param>
    /// <param name="text">The text, which is never empty.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or the reason the key, the names or the text was refused or the save failed.</returns>
    Task<IGenericResult> Save(DescriptionKey key, string thingName, DescriptionOwner? owner, string text, CancellationToken cancellationToken = default);

    /// <summary>Deletes one description.</summary>
    /// <param name="key">What names the description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or the reason the key was refused or no such description exists.</returns>
    Task<IGenericResult> Delete(DescriptionKey key, CancellationToken cancellationToken = default);
}
