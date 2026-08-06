using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Connections.Http;

/// <summary>
/// Data record for the <c>conn.HttpConnectionSoap</c> table.
/// Represents SOAP-specific settings for an HTTP connection.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class HttpConnectionSoapRecord
{

    /// <summary>Gets or sets the parent HTTP connection identifier.</summary>
    public Guid HttpConnectionId { get; set; }


    /// <summary>Gets or sets the default SOAP namespace.</summary>
    public string? DefaultNamespace { get; set; }

    /// <summary>Gets or sets the SOAPAction header pattern.</summary>
    public string? SoapActionPattern { get; set; }

    /// <summary>Gets or sets the source identifier for service-specific headers.</summary>
    public string? Source { get; set; }

    /// <summary>Gets or sets the user ID for service-specific headers.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets whether this is the current version.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether this record is soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the source system create date.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the create date.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets who created this record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets who this record was created on behalf of.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the last modified date.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets who last modified this record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets who this record was modified on behalf of.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
