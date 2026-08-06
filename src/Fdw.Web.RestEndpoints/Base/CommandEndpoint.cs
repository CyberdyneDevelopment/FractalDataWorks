using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Web.RestEndpoints.Base;

/// <summary>
/// Base class for CQRS command operations with RBAC and validation.
/// Commands are operations that modify state and typically require strict authorization.
/// </summary>
/// <typeparam name="TCommand">The command request type.</typeparam>
/// <typeparam name="TResult">The command result type.</typeparam>
public abstract class CommandEndpoint<TCommand, TResult> : GenericEndpoint<TCommand, TResult>
    where TCommand : notnull
{
    /// <summary>
    /// Configures the command endpoint with authentication, authorization, and rate limiting.
    /// </summary>
    public override void Configure()
    {
        ConfigureAsPostEndpoint();

        // Commands require authentication and specific permissions
        Policies("RequireAuthentication");
        Roles(GetRequiredRoles());

        // Stricter rate limiting for commands
        Throttle(hitLimit: 50, durationSeconds: 60);

        // Request validation
        // Validator will be configured by derived classes if needed

        ConfigureEndpoint();
    }

    /// <summary>
    /// Enhanced authorization check for commands with role-based access control.
    /// </summary>
    protected override async Task<IGenericResult> CheckAuthorization(TCommand request, CancellationToken ct)
    {
        // Base authorization check
        var baseResult = await base.CheckAuthorization(request, ct).ConfigureAwait(false);
        if (!baseResult.IsSuccess)
            return baseResult;

        // Command-specific authorization
        return await CheckCommandAuthorization(request, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the command logic. Override this to implement your command.
    /// </summary>
    /// <param name="command">The validated command request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the command execution.</returns>
    protected abstract Task<IGenericResult<TResult>> ExecuteCommand(TCommand command, CancellationToken ct);

    /// <summary>
    /// Executes the command with additional error handling for command-specific scenarios.
    /// </summary>
    public override async Task<object> Execute(TCommand request, CancellationToken ct)
    {
        try
        {
            return await ExecuteCommand(request, ct).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return GenericResult<TResult>.Failure(
                EndpointLogger.InvalidOperation(Logger, ex, typeof(TCommand).Name));
        }
        catch (ArgumentException ex)
        {
            return GenericResult<TResult>.Failure(
                EndpointLogger.InvalidArgument(Logger, ex, typeof(TCommand).Name));
        }
    }

    /// <summary>
    /// Performs command-specific authorization checks.
    /// Override this to implement business-specific authorization rules.
    /// </summary>
    protected virtual Task<IGenericResult> CheckCommandAuthorization(TCommand command, CancellationToken ct)
        => Task.FromResult<IGenericResult>(GenericResult.Success());

    /// <summary>
    /// Gets the roles required to execute this command.
    /// Override to specify specific roles like "Admin", "Manager", etc.
    /// </summary>
    protected virtual string[] GetRequiredRoles() => ["User"];

    /// <summary>
    /// Configures this endpoint as a POST endpoint. Override to customize HTTP method or routing.
    /// </summary>
    protected virtual void ConfigureAsPostEndpoint()
    {
        // Override in derived classes to set route: Post("/api/endpoint");
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
        // Override in derived classes for additional configuration
    }
}

/// <summary>
/// Command endpoint for commands that don't return specific data (void commands).
/// Returns a success/failure result without a specific value.
/// </summary>
/// <typeparam name="TCommand">The command request type.</typeparam>
public abstract class CommandEndpoint<TCommand> : CommandEndpoint<TCommand, object>
    where TCommand : notnull
{
    /// <summary>
    /// Executes a void command that returns success/failure without data.
    /// </summary>
    protected abstract Task<IGenericResult> ExecuteVoidCommand(TCommand command, CancellationToken ct);

    /// <summary>
    /// Wraps the void command execution to match the base class signature.
    /// </summary>
    protected override async Task<IGenericResult<object>> ExecuteCommand(TCommand command, CancellationToken ct)
    {
        var result = await ExecuteVoidCommand(command, ct).ConfigureAwait(false);
        return result.IsSuccess
            ? GenericResult<object>.Success(new { Success = true })
            : result.Messages.Any()
                ? result.ToNewResult<object>()
                : GenericResult<object>.Failure(EndpointLogger.CommandExecutionFailed(Logger, typeof(TCommand).Name));
    }
}