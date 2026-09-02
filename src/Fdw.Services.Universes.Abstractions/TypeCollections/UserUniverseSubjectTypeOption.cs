using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>An individual person holds the membership.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseSubjectTypes), "User")]
public sealed class UserUniverseSubjectTypeOption : UniverseSubjectTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="UserUniverseSubjectTypeOption"/> class.</summary>
    public UserUniverseSubjectTypeOption() : base("User")
    {
    }
}
