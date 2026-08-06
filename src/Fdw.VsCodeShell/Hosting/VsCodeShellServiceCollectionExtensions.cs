using System;
using Fdw.VsCodeShell.Manifest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.VsCodeShell.Abstractions;

namespace Fdw.VsCodeShell.Hosting;

/// <summary>DI extensions that register the VS Code shell into an application's services.</summary>
public static class VsCodeShellServiceCollectionExtensions
{
    /// <summary>
    /// Registers the VS Code shell and every command contributed by this host and its referenced packages.
    /// </summary>
    /// <remarks>
    /// Commands are not declared here. Each is a <c>[ServiceTypeOption]</c> on
    /// <see cref="VsCodeCommandTypes"/> in the package that owns it, registered at assembly load by the
    /// generated module initializer — so referencing such a package is itself the intent to contribute its
    /// commands. This call registers their handlers and projects the manifest from the collection.
    /// </remarks>
    public static IHostApplicationBuilder AddVsCodeShell(
        this IHostApplicationBuilder builder,
        Action<VsCodeShellOptions> configureOptions,
        ILoggerFactory? loggerFactory = null)
    {
        var options = new VsCodeShellOptions();
        configureOptions(options);

        if (string.IsNullOrWhiteSpace(options.ExtensionId))
        {
            throw new InvalidOperationException("VsCodeShellOptions.ExtensionId must be set — it appears in the bootstrap manifest and is required.");
        }

        if (string.IsNullOrWhiteSpace(options.DisplayName))
        {
            throw new InvalidOperationException("VsCodeShellOptions.DisplayName must be set — it appears in the bootstrap manifest and is required.");
        }

        // Why: fans out to every option's Register phase, which is where each command registers
        // its own handler keyed on its CommandId. Idempotent per IServiceCollection.
        VsCodeCommandTypes.Register(builder, loggerFactory);

        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IVsCodeManifest>(VsCodeManifestFactory.Create(options));

        return builder;
    }
}
