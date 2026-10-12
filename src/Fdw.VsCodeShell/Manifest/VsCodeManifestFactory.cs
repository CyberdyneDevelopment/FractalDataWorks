using System.Collections.Generic;
using System.Linq;
using Fdw.VsCodeShell.Hosting;
using Fdw.VsCodeShell.Abstractions;

namespace Fdw.VsCodeShell.Manifest;

/// <summary>
/// Projects the registered <see cref="VsCodeCommandTypes"/> options into the wire manifest the
/// bootstrap reads at <c>GET /vscode/manifest</c>.
/// </summary>
/// <remarks>
/// The wire format keeps commands and webviews as two flat arrays joined by <c>OpenCommandId</c>, which is
/// what <c>extension.js</c> already consumes — so no JavaScript changes. The join is reconstructed here from
/// the owning command rather than authored by hand, which is the point: a webview can no longer name a
/// command that does not exist.
/// </remarks>
internal static class VsCodeManifestFactory
{
    /// <summary>
    /// Builds the manifest from every command option registered in the collection.
    /// </summary>
    public static VsCodeManifest Create(VsCodeShellOptions options)
        => Create(options, VsCodeCommandTypes.All().Values);

    /// <summary>
    /// Builds the manifest from an explicit command set.
    /// </summary>
    /// <remarks>Separate overload so the projection can be tested without registering into the collection.</remarks>
    public static VsCodeManifest Create(VsCodeShellOptions options, IEnumerable<IVsCodeCommandType> commandTypes)
    {
        var commands = new List<VsCodeCommandDescriptor>();
        var webviews = new List<VsCodeWebviewDescriptor>();

        foreach (var command in commandTypes.OrderBy(c => c.CommandId, System.StringComparer.Ordinal))
        {
            commands.Add(new VsCodeCommandDescriptor(
                command.CommandId,
                command.Title,
                command.PaletteCategory,
                command.ContextKind));

            if (command.Webview is not null)
            {
                webviews.Add(new VsCodeWebviewDescriptor(
                    command.Webview.ViewType,
                    command.Webview.Title,
                    command.CommandId,
                    command.Webview.Path,
                    command.Webview.RetainContextWhenHidden));
            }
        }

        return new VsCodeManifest(options.ExtensionId, options.DisplayName, commands, webviews);
    }
}
