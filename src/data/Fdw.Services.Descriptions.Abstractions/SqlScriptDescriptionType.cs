using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A stored SQL script's description: what running it does, in the words of whoever wrote it (FDW-7323). Read by whoever holds <c>scripts:read</c>, the permission the script routes ask. The script is the thing described, so the type has no owner: a script is removed with its own descriptions.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class SqlScriptDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the script provider writes and reads its descriptions under.</summary>
    public const string OptionName = "SqlScript";

    /// <summary>Initializes a new instance of the <see cref="SqlScriptDescriptionType"/> class.</summary>
    public SqlScriptDescriptionType()
        : base(OptionName, "scripts:read", hasOwner: false)
    {
    }
}
