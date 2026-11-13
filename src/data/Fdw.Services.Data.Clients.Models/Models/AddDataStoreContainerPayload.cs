using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Request body for adding a container to an existing data store path.
/// </summary>
public sealed class AddDataStoreContainerPayload
{
    /// <summary>Gets or sets the path name within the data store to add the container to.</summary>
    public string PathName { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name for the new container.</summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the container type discriminator (e.g., "Table", "View").</summary>
    public string? ContainerType { get; set; }

    /// <summary>
    /// Gets or sets a JSONPath-style selector to the array of row objects in the response payload.
    /// Example: "features" for <c>response.features[*]</c>. Null means rows are at the JSON root.
    /// Only meaningful for REST containers.
    /// </summary>
    public string? RecordSelector { get; set; }

    /// <summary>
    /// Gets or sets whether nested JSON objects should be flattened into dot-notation field names.
    /// Null means the container inherits the library default (false).
    /// </summary>
    public bool? FlattenNestedObjects { get; set; }

    /// <summary>
    /// Gets or sets the separator character for flattened field names (e.g., ".").
    /// Null means the container inherits the library default (".").
    /// </summary>
    public string? FlattenSeparator { get; set; }

    /// <summary>
    /// Gets or sets whether the first row of the file is a header row. Only for a Delimited or Xlsx container; refused
    /// for any other format. Left out, the first row is read as data.
    /// </summary>
    public bool? HasHeader { get; set; }

    /// <summary>
    /// Gets or sets the separator of the file's fields. Only for a Delimited container, 1 to 10 characters; refused for
    /// any other format. It is not <c>FlattenSeparator</c>, which joins the names of flattened nested objects.
    /// </summary>
    public string? Separator { get; set; }

    /// <summary>
    /// Gets or sets the name of the worksheet to read. Only for an Xlsx container, at most 31 characters and none of
    /// <c>[ ] : * ? / \</c>; refused for any other format. Left out, a workbook with several worksheets is refused when read.
    /// </summary>
    public string? SheetName { get; set; }

    /// <summary>Gets or sets whether a reader of the container goes on after a row it cannot read, for any format. A choice: required where the format reads or writes it, never defaulted (FDW-3823).</summary>
    public bool? ContinueOnError { get; set; }

    /// <summary>Gets or sets whether a JSON reader matches property names without regard to case, for a Json container. A choice: required where the format reads or writes it, never defaulted (FDW-3823).</summary>
    public bool? PropertyNameCaseInsensitive { get; set; }

    /// <summary>Gets or sets whether a JSON writer skips its structure validation, for a Json container. A choice: required where the format reads or writes it, never defaulted (FDW-3823).</summary>
    public bool? SkipValidation { get; set; }

    /// <summary>Gets or sets whether an XML reader reads child elements as fields, for a Xml container. A choice: required where the format reads or writes it, never defaulted (FDW-3823).</summary>
    public bool? UseElementContent { get; set; }

    /// <summary>Gets or sets whether an XML reader reads attributes as fields, for a Xml container. A choice: required where the format reads or writes it, never defaulted (FDW-3823).</summary>
    public bool? IncludeAttributes { get; set; }

    /// <summary>Gets or sets the root element an XML writer writes, for a Xml container. A choice: required where the format reads or writes it, never defaulted (FDW-3823).</summary>
    public string? RootElementName { get; set; }

    /// <summary>Gets or sets the element of each row an XML reader reads and an XML writer writes, for a Xml container. A choice: required where the format reads or writes it, never defaulted (FDW-3823).</summary>
    public string? RowElementName { get; set; }

    /// <summary>Gets or sets whether an XML writer leaves out the XML declaration, for a Xml container. A choice: required where the format reads or writes it, never defaulted (FDW-3823).</summary>
    public bool? OmitXmlDeclaration { get; set; }

    /// <summary>Gets or sets whether a delimited reader reads quoted fields, for a Delimited container. A choice: required where the format reads or writes it, never defaulted (FDW-3823).</summary>
    public bool? ContainsQuotedFields { get; set; }

    /// <summary>Gets or sets whether a delimited or fixed-width reader trims its fields, for a Delimited or FixedWidth container. A choice: required where the format reads or writes it, never defaulted (FDW-3823).</summary>
    public bool? Trim { get; set; }

    /// <summary>Gets or sets the one character a delimited writer quotes a field with, for a Delimited container. A choice: required where the format reads or writes it, never defaulted (FDW-3823).</summary>
    public string? QuoteChar { get; set; }

    /// <summary>Gets or sets whether a delimited writer starts the file with a byte order mark, for a Delimited container. A choice: required where the format reads or writes it, never defaulted (FDW-3823).</summary>
    public bool? WriteByteOrderMark { get; set; }

    /// <summary>Gets or sets whether a delimited writer prefixes a text value that starts like a formula with an apostrophe, for a Delimited container. A choice: required where the format reads or writes it, never defaulted (FDW-3823).</summary>
    public bool? NeutralizeFormulas { get; set; }

    /// <summary>
    /// Gets or sets the path, after the connection's address, that the rows are sent to when this container is the destination of
    /// an HTTP connection, for example <c>/api/v1/ingest</c>. A full web address is refused. Left out, a destination that needs it
    /// fails naming it when it runs.
    /// </summary>
    public string? WriteEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method the rows are sent with: <c>POST</c> or <c>PUT</c>; anything else is refused. Left out, a
    /// destination that needs it fails naming it when it runs.
    /// </summary>
    public string? WriteMethod { get; set; }

    /// <summary>
    /// Gets or sets the content type the rows are sent as, for example <c>application/json</c>; a value that is not a media type is
    /// refused. Left out, a destination that needs it fails naming it when it runs.
    /// </summary>
    public string? WriteContentType { get; set; }

    /// <summary>
    /// Gets or sets the paging style the container pages in when it pages differently from its connection (FDW-5562): the name of an <c>HttpPagingStyles</c>
    /// option, which may be its connection's own. Left out, the container takes its connection's paging.
    /// </summary>
    public string? PagingStyle { get; set; }

    /// <summary>
    /// Gets or sets the paging settings the container states, by name, for the style it names (FDW-5562). A setting left out, or sent blank, is taken from the
    /// connection when the container pages in its connection's style, and from the style's declared default otherwise.
    /// </summary>
    public IDictionary<string, string?> PagingSettings { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}
