using System;
using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Collections.Attributes;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Fdw.VsCodeShell.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Hosting;

namespace Fdw.VsCodeShell;

/// <summary>
/// Base class for VS Code command options. One option per command, declared in the package that owns
/// the command and registered at assembly load by the generated module initializer.
/// </summary>
/// <typeparam name="THandler">The concrete handler implementing this command's behaviour. Each command
/// must supply a distinct <typeparamref name="THandler"/>, which is what gives it a distinct
/// <c>ServiceTypeBase.Id</c> — see <see cref="IVsCodeCommandFactory{THandler}"/>.</typeparam>
/// <remarks>
/// <para>
/// The type parameter is deliberately left open here. Per the TypeCollection guidance, a base that keeps
/// its parameters open lets each concrete option resolve them to unique types, which is what makes every
/// option's Id unique. Closing <typeparamref name="THandler"/> on this base would give every command the
/// same Id and silently drop all but the first.
/// </para>
/// <para>
/// The command's webview (if any) is a property of the command rather than a separate declaration joined
/// by command id, so a webview cannot reference a command that does not exist.
/// </para>
/// </remarks>
public abstract class VsCodeCommandTypeBase<THandler>
    : ServiceTypeBase<IGenericService, IVsCodeCommandFactory<THandler>, IServiceConfiguration>, IVsCodeCommandType
    where THandler : class, IVsCodeCommandHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VsCodeCommandTypeBase{THandler}"/> class.
    /// </summary>
    /// <param name="name">The <c>[ServiceTypeOption]</c> option name. Must be a valid C# identifier, so it
    /// is NOT the dotted command id — pass that as <paramref name="commandId"/>.</param>
    /// <param name="commandId">The id VS Code invokes, e.g. <c>pidgin.openCanvas</c>.</param>
    /// <param name="title">Palette title WITHOUT a category prefix — VS Code renders <c>Category: Title</c>,
    /// so a prefixed title would display twice.</param>
    /// <param name="paletteCategory">Optional VS Code palette grouping.</param>
    /// <param name="contextKind">Editor context to capture before dispatch: none, cursor, selection, or document.</param>
    /// <param name="webview">The webview this command opens, or null.</param>
    protected VsCodeCommandTypeBase(
        string name,
        string commandId,
        string title,
        string? paletteCategory = null,
        string contextKind = "none",
        VsCodeWebview? webview = null)
        : base(name, "VsCodeCommands", title, $"VS Code command '{commandId}'", "VsCodeCommand")
    {
        CommandId = commandId;
        Title = title;
        PaletteCategory = paletteCategory;
        ContextKind = contextKind;
        Webview = webview;

        // Why this constructor contributes nothing to a phase: a phase holds one body and the option
        // that declares it owns it. Every command registers against the same IVsCodeCommandHandler keyed
        // by its own CommandId, differing only by CommandId and HandlerType - both already exposed here -
        // so the act belongs to the domain. VsCodeCommandTypes.Register does it once over the option set.
    }

    /// <inheritdoc />
    /// <remarks>
    /// Deliberately NOT marked <c>[TypeLookup]</c>. The generator gathers lookups from the
    /// <c>[ServiceTypeOption]</c> types it discovers in the collection's own compilation, and every command
    /// is declared downstream of this assembly — so a lookup declared here would emit nothing. Verified:
    /// <c>ApiClientTypes</c>, in the same position, generates no lookup methods either. The command-id index
    /// is therefore built once from <c>All()</c> when the shell is registered.
    /// </remarks>
    public string CommandId { get; }

    /// <inheritdoc />
    public string Title { get; }

    /// <inheritdoc />
    public string? PaletteCategory { get; }

    /// <inheritdoc />
    public string ContextKind { get; }

    /// <inheritdoc />
    public VsCodeWebview? Webview { get; }

    /// <inheritdoc />
    public Type HandlerType => typeof(THandler);

}
