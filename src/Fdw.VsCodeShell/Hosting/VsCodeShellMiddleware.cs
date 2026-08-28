using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.VsCodeShell.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.VsCodeShell.Abstractions;

namespace Fdw.VsCodeShell.Hosting;

/// <summary>
/// Middleware that serves the manifest at <c>GET /vscode/manifest</c> and dispatches
/// command invocations at <c>POST /vscode/commands/{id}</c>. Every other path passes
/// through to the host's own pipeline (Razor, Blazor, static files, etc).
/// </summary>
internal sealed class VsCodeShellMiddleware
{
    private const string ManifestPath = "/vscode/manifest";
    private const string CommandPathPrefix = "/vscode/commands/";
    private const string HealthPath = "/vscode/health";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    private readonly RequestDelegate _next;
    private readonly IVsCodeManifest _manifest;
    private readonly HashSet<string> _declaredCommandIds;
    private readonly ILogger<VsCodeShellMiddleware> _logger;

    public VsCodeShellMiddleware(RequestDelegate next, IVsCodeManifest manifest, ILogger<VsCodeShellMiddleware>? logger = null)
    {
        _next = next;
        _manifest = manifest;
        _logger = logger ?? NullLogger<VsCodeShellMiddleware>.Instance;

        _declaredCommandIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var command in manifest.Commands)
        {
            _declaredCommandIds.Add(command.Id);
        }
    }

    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (HttpMethods.IsGet(context.Request.Method) && string.Equals(path, ManifestPath, StringComparison.OrdinalIgnoreCase))
        {
            await WriteJson(context, _manifest, context.RequestAborted).ConfigureAwait(false);
            return;
        }

        if (HttpMethods.IsGet(context.Request.Method) && string.Equals(path, HealthPath, StringComparison.OrdinalIgnoreCase))
        {
            await WriteJson(context, new { status = "ok" }, context.RequestAborted).ConfigureAwait(false);
            return;
        }

        if (HttpMethods.IsPost(context.Request.Method) && path.StartsWith(CommandPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var commandId = path.Substring(CommandPathPrefix.Length);
            await DispatchCommand(context, commandId, context.RequestAborted).ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    private async Task DispatchCommand(HttpContext context, string commandId, CancellationToken cancellationToken)
    {
        if (!_declaredCommandIds.Contains(commandId))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await WriteJson(
                context,
                new { error = "unknown_command", commandId, message = VsCodeShellLog.UnknownCommand(_logger, commandId).Message },
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var handler = context.RequestServices.GetKeyedService<IVsCodeCommandHandler>(commandId);
        if (handler is null)
        {
            context.Response.StatusCode = StatusCodes.Status501NotImplemented;
            await WriteJson(
                context,
                new { error = "no_handler_registered", commandId, message = VsCodeShellLog.HandlerNotRegistered(_logger, commandId).Message },
                cancellationToken).ConfigureAwait(false);
            return;
        }

        EditorContext editor;
        if (context.Request.ContentLength is > 0)
        {
            using var stream = new StreamReader(context.Request.Body);
            var body = await stream.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            editor = JsonSerializer.Deserialize<EditorContext>(body, JsonOptions)
                ?? new EditorContext(null, null, null, null, null, null);
        }
        else
        {
            editor = new EditorContext(null, null, null, null, null, null);
        }

        var result = await handler.Invoke(editor, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            VsCodeShellLog.HandlerFailed(_logger, commandId);
            await WriteJson(context, new { error = "handler_failed", commandId, message = result.CurrentMessage }, cancellationToken).ConfigureAwait(false);
            return;
        }

        await WriteJson(context, new { ok = true, value = result.Value }, cancellationToken).ConfigureAwait(false);
    }

    private static Task WriteJson<T>(HttpContext context, T payload, CancellationToken cancellationToken)
    {
        context.Response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        return context.Response.WriteAsync(json, cancellationToken);
    }
}
