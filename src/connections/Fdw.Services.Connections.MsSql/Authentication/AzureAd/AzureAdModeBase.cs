using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Authentication.AzureAd;

/// <summary>The type the Entra ID mode collection is built on: a mode whose fragment is fixed. A mode that reads the values overrides <see cref="Fragment"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class AzureAdModeBase : TypeOptionBase<AzureAdModeBase>, IAzureAdMode
{
    private readonly string _fragment;

    /// <summary>Initializes the mode with its name, the word it is found by and its fixed fragment.</summary>
    /// <param name="name">The mode's name.</param>
    /// <param name="word">The word that names the mode, in upper case.</param>
    /// <param name="fragment">The authentication fragment, for a mode whose fragment does not depend on the values.</param>
    protected AzureAdModeBase(string name, string word, string fragment)
        : base(name)
    {
        Word = word;
        _fragment = fragment;
    }

    /// <inheritdoc/>
    [TypeLookup("ByWord")]
    public string Word { get; }

    /// <inheritdoc/>
    public virtual IGenericResult<string> Fragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword)
        => GenericResult<string>.Success(_fragment);
}
