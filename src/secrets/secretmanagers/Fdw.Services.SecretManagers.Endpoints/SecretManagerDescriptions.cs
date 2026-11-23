using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Puts a secret manager's stored description into the settings an answer shows (FDW-6208). The secret manager domain never fills a record on load, so the route that shows the
/// text reads it, with one read of the descriptions table for the answer.
/// </summary>
/// <remarks>
/// What decides whether the text is shown is what the configuration is, not what kind it is called: it must carry a description (<see cref="IDescribedConfiguration"/>) and
/// the settings its kind takes must declare the entry. A kind that holds no text, or whose settings do not name one, is answered exactly as it was. No default text is put in
/// place of a missing one: a secret manager with no description row keeps the empty entry the settings already show. No secret value is read or shown by any of it.
/// </remarks>
public static class SecretManagerDescriptions
{
    private const string SettingName = nameof(IDescribedConfiguration.Description);

    /// <summary>Sets the description entry of the shown settings from the descriptions table, when the configuration carries one and the settings declare it.</summary>
    /// <param name="shown">The settings the answer shows, as <see cref="SecretManagerSettings.Shown"/> made them; the entry is set in place.</param>
    /// <param name="stored">The configuration the settings were made from, with the Id it is stored under.</param>
    /// <param name="reader">Reads the description.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or the failure of the read, which fails the route.</returns>
    public static async Task<IGenericResult> Fill(JsonObject shown, ISecretManagerImplementationConfiguration stored, IDescriptionReader reader, CancellationToken cancellationToken = default)
    {
        if (stored is not IDescribedConfiguration || !shown.ContainsKey(SettingName))
        {
            return GenericResult.Success();
        }

        var read = await reader.ReadMany(SecretManagerDescriptionType.OptionName, DescriptionTextSubType.OptionName, [stored.Id], cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is null)
        {
            return read;
        }

        if (read.Value.TryGetValue(stored.Id, out var entry))
        {
            shown[SettingName] = JsonValue.Create(entry.Text);
        }

        return GenericResult.Success();
    }
}
