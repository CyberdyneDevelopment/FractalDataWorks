using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Detailed DTO for a data container, including field information.
/// </summary>
public class DataStoreContainerDetailDto
{
    /// <summary>Gets or sets the container identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the container name (table/collection name).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the container type (e.g., Table, View).</summary>
    public string ContainerType { get; set; } = string.Empty;

    /// <summary>Gets or sets the container description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of the container's data classification level; <c>NotYetClassified</c> until someone picks a level.
    /// </summary>
    public int ClassificationId { get; set; }

    /// <summary>Gets or sets the container's serialization format, or null when it states none.</summary>
    public string? Format { get; set; }

    /// <summary>Gets or sets whether the file's first row is a header row, or null when not stated.</summary>
    public bool? HasHeader { get; set; }

    /// <summary>Gets or sets the separator of the file's fields, or null when not stated.</summary>
    public string? Separator { get; set; }

    /// <summary>Gets or sets the worksheet to read, or null when not stated.</summary>
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

    /// <summary>Gets or sets the path the rows are sent to when this container is an HTTP destination, or null when not stated.</summary>
    public string? WriteEndpoint { get; set; }

    /// <summary>Gets or sets the HTTP method the rows are sent with, or null when not stated.</summary>
    public string? WriteMethod { get; set; }

    /// <summary>Gets or sets the content type the rows are sent as, or null when not stated.</summary>
    public string? WriteContentType { get; set; }

    /// <summary>Gets or sets whether the container states paging of its own: true when it names a paging style (FDW-5562).</summary>
    public bool HasPagingOverride { get; set; }

    /// <summary>Gets or sets the paging style the container names, or null when it takes its connection's.</summary>
    public string? PagingStyle { get; set; }

    /// <summary>
    /// Gets or sets the paging the container reads with, its own style and settings laid over its connection's, each value with where it came from
    /// (<c>container</c>, <c>connection</c> or <c>style</c>). Null when its store does not page its containers, or no style is stated at either level.
    /// </summary>
    public ContainerPagingDto? Paging { get; set; }

    /// <summary>Gets or sets the fields within this container.</summary>
    public IList<DataStoreFieldResponse> Fields { get; set; } = [];
}
