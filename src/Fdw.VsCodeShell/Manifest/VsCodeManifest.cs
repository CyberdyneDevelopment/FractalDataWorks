using System.Collections.Generic;
using Fdw.VsCodeShell.Abstractions;

namespace Fdw.VsCodeShell.Manifest;

internal sealed record VsCodeManifest(
    string ExtensionId,
    string DisplayName,
    IReadOnlyList<VsCodeCommandDescriptor> Commands,
    IReadOnlyList<VsCodeWebviewDescriptor> Webviews) : IVsCodeManifest
{
    IReadOnlyList<IVsCodeCommandDescriptor> IVsCodeManifest.Commands => Commands;
    IReadOnlyList<IVsCodeWebviewDescriptor> IVsCodeManifest.Webviews => Webviews;
}
