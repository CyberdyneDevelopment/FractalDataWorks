using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Results;
using Fdw.Services.Connections.Endpoints.Logging;
using Fdw.Services.Connections.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// The members of an update body that change a saved connection, held in one place so the rule that an update must state
/// something, its refusal message and the list of members that message names cannot drift apart (FDW-1177).
/// </summary>
/// <remarks>
/// <para>
/// Why the list is data: the "at least one field" rule was written by hand in the live validator and listed every member except
/// the credentials and the default-value policy, so a body that stated only new credentials was refused as stating nothing and
/// a password could not be rotated alone. The same hand-written list counted an empty string, an empty dictionary and a null
/// configuration as stated, although the body builder drops all three, so such a body was accepted and changed nothing. Every
/// member an update may change is a row here with the way it reaches the stored connection, and what counts as "stated" is
/// what that way actually applies.
/// </para>
/// <para>
/// Not a member: the connection's name (the route's). <c>IsActive</c> is stated by the request but a connection stores no such
/// setting, so it is on <see cref="Unchangeable"/> and refused by <see cref="CheckUnchangeable"/> instead of being accepted and
/// ignored.
/// </para>
/// </remarks>
public static class UpdateConnectionChanges
{
    /// <summary>The error code the update validator gives a body that states nothing to change.</summary>
    public const string NothingStatedCode = "NothingToUpdate";

    /// <summary>The way a member reaches the stored connection: the JSON the connection type's endpoint behavior merges (<see cref="ConnectionRequestBody.ForUpdate"/>).</summary>
    public const string ReachesByMerge = "Merge";

    /// <summary>The way a member reaches the stored connection: the health-check settings the update endpoint copies onto it.</summary>
    public const string ReachesByHealthCheck = "HealthCheck";

    /// <summary>The way a member reaches the stored connection: the schema discovery option the update endpoint chooses.</summary>
    public const string ReachesBySchemaDiscovery = "SchemaDiscovery";

    /// <summary>The way a member reaches the stored connection: the credential store writes the values and points the connection at them.</summary>
    public const string ReachesByCredentialStore = "CredentialStore";

    private static readonly Member[] Members =
    [
        new(nameof(UpdateConnectionRequest.Server), ReachesByMerge, request => IsText(request.Server)),
        new(nameof(UpdateConnectionRequest.Port), ReachesByMerge, request => request.Port.HasValue),
        new(nameof(UpdateConnectionRequest.Database), ReachesByMerge, request => IsText(request.Database)),
        new(nameof(UpdateConnectionRequest.AuthenticationType), ReachesByMerge, request => IsText(request.AuthenticationType)),
        new(nameof(UpdateConnectionRequest.Authentication), ReachesByMerge, request => request.Authentication is { Count: > 0 }),
        new(nameof(UpdateConnectionRequest.TrustServerCertificate), ReachesByMerge, request => request.TrustServerCertificate.HasValue),
        new(nameof(UpdateConnectionRequest.Encrypt), ReachesByMerge, request => request.Encrypt.HasValue),
        new(nameof(UpdateConnectionRequest.BaseUrl), ReachesByMerge, request => IsText(request.BaseUrl)),
        new(nameof(UpdateConnectionRequest.Protocol), ReachesByMerge, request => IsText(request.Protocol)),
        new(nameof(UpdateConnectionRequest.TimeoutSeconds), ReachesByMerge, request => request.TimeoutSeconds.HasValue),
        new(nameof(UpdateConnectionRequest.Headers), ReachesByMerge, request => request.Headers is { Count: > 0 }),
        new(nameof(UpdateConnectionRequest.SecurityType), ReachesByMerge, request => IsText(request.SecurityType)),
        new(nameof(UpdateConnectionRequest.Security), ReachesByMerge, request => request.Security is { Count: > 0 }),
        new(nameof(UpdateConnectionRequest.UseMtls), ReachesByMerge, request => request.UseMtls.HasValue),
        new(nameof(UpdateConnectionRequest.DefaultValuePolicy), ReachesByMerge, request => IsText(request.DefaultValuePolicy)),
        new(nameof(UpdateConnectionRequest.Configuration), ReachesByMerge,
            request => request.Configuration is { ValueKind: not JsonValueKind.Null and not JsonValueKind.Undefined }),

        new(nameof(UpdateConnectionRequest.HealthCheckEnabled), ReachesByHealthCheck, request => request.HealthCheckEnabled.HasValue),
        new(nameof(UpdateConnectionRequest.HealthCheckOnStartup), ReachesByHealthCheck, request => request.HealthCheckOnStartup.HasValue),
        new(nameof(UpdateConnectionRequest.HealthCheckIntervalSeconds), ReachesByHealthCheck, request => request.HealthCheckIntervalSeconds.HasValue),

        // An empty string is a name the endpoint judges against the options the type offers, so it is stated and refused there.
        new(nameof(UpdateConnectionRequest.SchemaDiscovery), ReachesBySchemaDiscovery, request => request.SchemaDiscovery is not null),

        // A supplied set replaces the stored credentials and must name every one the method uses, so an empty set is a statement
        // too: the store refuses it for the credentials it leaves out.
        new(nameof(UpdateConnectionRequest.Credentials), ReachesByCredentialStore, request => request.Credentials is not null),
    ];

    private static readonly Member[] NotStored =
    [
        new(nameof(UpdateConnectionRequest.IsActive), ReachesByMerge, request => request.IsActive.HasValue),
    ];

    /// <summary>Gets the names of the members an update may change, in the order the refusal lists them.</summary>
    public static IReadOnlyList<string> Names { get; } = [.. Members.Select(member => member.Name)];

    /// <summary>
    /// Gets the names of the members of the request that an update states but never changes, because a connection stores no such
    /// setting. <see cref="CheckUnchangeable"/> refuses them, so they are not on <see cref="Names"/> and do not count as a change.
    /// </summary>
    public static IReadOnlyList<string> Unchangeable { get; } = [.. NotStored.Select(member => member.Name)];

    /// <summary>Gets the refusal message for a body that states no member to change, naming every member that could be stated.</summary>
    public static string RefusalMessage { get; } =
        string.Concat(
            "At least one updateable field must be supplied (", string.Join(", ", Names), "); a connection's ",
            string.Join(", ", Unchangeable), " cannot be changed");

    /// <summary>Gets how the named member reaches the stored connection.</summary>
    /// <param name="name">A name from <see cref="Names"/>.</param>
    /// <returns>One of the <c>Reaches</c> constants.</returns>
    public static string ReachedBy(string name) =>
        Members.Single(member => string.Equals(member.Name, name, StringComparison.Ordinal)).Reaches;

    /// <summary>Gets whether the body states at least one member to change.</summary>
    /// <param name="request">The update body.</param>
    /// <returns><see langword="true"/> when the body states any member on the list.</returns>
    public static bool Stated(UpdateConnectionRequest request) => Members.Any(member => member.Stated(request));

    /// <summary>Gets whether the body states the one named member, whether it is changeable or not.</summary>
    /// <param name="request">The update body.</param>
    /// <param name="name">A name from <see cref="Names"/> or <see cref="Unchangeable"/>.</param>
    /// <returns><see langword="true"/> when the body states that member.</returns>
    public static bool Stated(UpdateConnectionRequest request, string name) =>
        Members.Concat(NotStored).Single(member => string.Equals(member.Name, name, StringComparison.Ordinal)).Stated(request);

    /// <summary>Refuses a body that states a member a connection does not store.</summary>
    /// <param name="request">The update body.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>Success when the body states none of them; otherwise the refusal naming the connection and the first member stated.</returns>
    public static IGenericResult CheckUnchangeable(UpdateConnectionRequest request, ILogger logger)
    {
        var stated = NotStored.FirstOrDefault(member => member.Stated(request));
        if (stated is null)
            return GenericResult.Success();

        ConnectionEndpointLog.ConnectionMemberNotSettable(logger, request.Name, stated.Name);
        return GenericResult.Failure(
            ConnectionsResultCodes.ByName("ConnectionMemberNotSettable"),
            ResultDetails.Create("name", request.Name, "property", stated.Name));
    }

    // Why not string.IsNullOrEmpty alone at each row: the body builder writes a text member only when it is not empty, so an empty
    // string reaches nothing and must not count as stated.
    private static bool IsText(string? value) => !string.IsNullOrEmpty(value);

    private sealed record Member(string Name, string Reaches, Func<UpdateConnectionRequest, bool> Stated);
}
