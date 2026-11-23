using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.DataConfigurationRules;

/// <summary>A GUID; the empty GUID stands for no id.</summary>
[TypeOption(typeof(StoredIdKinds), "Guid")]
[ExcludeFromCodeCoverage]
public sealed class GuidStoredIdKind : StoredIdKindBase
{
    /// <summary>Initializes a new instance of the <see cref="GuidStoredIdKind"/> class.</summary>
    public GuidStoredIdKind()
        : base("Guid")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is Guid;

    /// <inheritdoc />
    public override Guid? Read(object value) => (Guid)value == default ? null : (Guid)value;
}
