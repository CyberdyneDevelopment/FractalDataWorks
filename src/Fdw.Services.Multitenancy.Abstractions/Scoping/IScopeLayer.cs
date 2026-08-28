using Fdw.Collections;
using Fdw.Services.Authentication.Abstractions;

namespace Fdw.Services.Multitenancy.Abstractions.Scoping;

/// <summary>
/// One dimension a row is scoped by.
/// </summary>
/// <remarks>
/// Tenant and visibility group are the two that exist. A deployment needing a third — a region, a
/// division — adds an option here, a column, and a predicate argument, rather than editing every
/// consumer that currently names two layers in its own signature.
/// <para>
/// A layer names the claim definition it reads rather than restating the claim's name. Two places
/// deciding what a tenant claim is called is how a token comes to carry one name while the session
/// context looks for another.
/// </para>
/// </remarks>
public interface IScopeLayer : ITypeOption<int, IScopeLayer>
{
    /// <summary>Gets the claim this layer's value travels in.</summary>
    /// <remarks>Null on the Empty sentinel, which names no claim and scopes nothing.</remarks>
    IClaimDefinition? Claim { get; }

    /// <summary>Gets the column carrying this layer on a scoped table.</summary>
    /// <remarks>
    /// Named here and matched by the RLS predicate, which stays explicit in DDL. A predicate
    /// assembled at runtime from this collection would be a security boundary you cannot read off
    /// the schema — the column name travels so callers agree, not so the database becomes dynamic.
    /// </remarks>
    string ColumnName { get; }

    /// <summary>Gets the key this layer is stamped under in SQL Server's session context.</summary>
    string SessionContextKey { get; }
}
