using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Configuration;

/// <summary>
/// Row shape for property-collection (KVP) child loads. The provider's child-composition reads child
/// rows that have a <c>Name</c> column and a <c>Value</c> column and inflates them into the parent
/// POCO's <c>IDictionary&lt;string, string?&gt;</c> property bound by the generated descriptor.
/// </summary>
/// <remarks>
/// Why a dedicated POCO with <c>[GenerateMapper]</c>: the source generator emits the reader mapping, so
/// the provider can call <c>Execute&lt;IEnumerable&lt;KeyValueRow&gt;&gt;</c> and let
/// <c>PocoMapperCollection.ByName("KeyValueRow")</c> materialise — no inline type dispatch.
/// Why it lives here (Fdw.Services) rather than Fdw.Services.Data: the
/// child-composition that consumes it is in <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/>,
/// and Services cannot reference up into Services.Data.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class KeyValueRow
{
    /// <summary>Gets or sets the KVP name (column <c>Name</c>).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the KVP value (column <c>Value</c>, nullable).</summary>
    public string? Value { get; set; }
}
