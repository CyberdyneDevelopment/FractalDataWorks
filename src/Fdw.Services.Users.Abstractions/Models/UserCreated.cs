using System;
using System.Collections.Generic;

namespace Fdw.Services.Users.Clients.Models;

/// <summary>
/// What a create-user request actually produced: the account, the sign-in methods that landed,
/// and the key, when one was minted.
/// </summary>
/// <remarks>
/// The account lives in ConfigurationDb and its credentials live in AuthDb, so the write is not one
/// transaction. Reporting which methods landed is what lets a caller finish the job through the
/// per-method routes instead of creating the account twice.
/// </remarks>
public class UserCreated
{
    /// <summary>Gets or sets the new account's id.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the steps that completed, in the order they ran.</summary>
    public IList<string> Created { get; set; } = new List<string>();

    /// <summary>Gets or sets the minted key, when the request asked for one.</summary>
    public ApiKeyIssued? ApiKey { get; set; }
}
