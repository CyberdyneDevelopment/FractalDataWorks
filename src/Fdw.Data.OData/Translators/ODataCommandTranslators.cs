using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Data.OData;

/// <summary>
/// TypeCollection of REST/OData data command translators.
/// Discovered at compile-time via TypeCollection source generator.
/// </summary>
/// <remarks>
/// <para>
/// This collection provides all REST/OData-specific translators for converting universal
/// IDataCommand objects into HTTP requests with OData query conventions.
/// </para>
/// <para>
/// Source generator creates static properties for each [TypeOption] translator:
/// <list type="bullet">
/// <item>ODataCommandTranslators.ODataQuery - GET with OData query parameters</item>
/// <item>ODataCommandTranslators.ODataInsert - POST with JSON body</item>
/// <item>ODataCommandTranslators.ODataUpdate - PUT/PATCH with JSON body</item>
/// <item>ODataCommandTranslators.ODataDelete - DELETE request</item>
/// </list>
/// </para>
/// <para>
/// These translators are registered at connection type registration time and made
/// available to the DataCommandTranslators collection.
/// </para>
/// <para>
/// GraphQL would have a separate GraphQLDataCommandTranslators collection with its own
/// query/mutation translators.
/// </para>
/// </remarks>
[TypeCollection(typeof(ODataCommandTranslatorBase), typeof(IDataCommandTranslator<HttpRequestMessage>), typeof(ODataCommandTranslators))]
[ExcludeFromCodeCoverage]
public sealed partial class ODataCommandTranslators :
    TypeCollectionBase<ODataCommandTranslatorBase, IDataCommandTranslator<HttpRequestMessage>>
{
    // Source generator creates:
    // - Static constructor
    // - Static properties: ODataQuery, ODataInsert, Update, ODataDelete
    // - public static IReadOnlyList<IDataCommandTranslator<HttpRequestMessage>> All()
    // - public static IDataCommandTranslator<HttpRequestMessage> ByName(string name)
    // - public static IDataCommandTranslator<HttpRequestMessage> ById(int id)
}
