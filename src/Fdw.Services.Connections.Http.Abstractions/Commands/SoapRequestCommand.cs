using System.Collections.Generic;
using System.Xml.Linq;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Services.Connections.Http.Abstractions.Commands;

/// <summary>
/// Data command for sending a pre-built SOAP request body.
/// </summary>
/// <remarks>
/// <para>
/// Unlike protocol-derived commands where <c>SoapProtocolBase.BuildSoapBody</c>
/// constructs the body from the command, this command carries a fully-formed
/// <see cref="XElement"/> body that the protocol passes through unchanged.
/// </para>
/// <para>
/// The caller supplies the SOAPAction header value and any additional HTTP headers
/// required by the target service. Addressing (connection, container, datastore, path)
/// lives in <c>DataStoreTarget</c> — not on the command.
/// </para>
/// <para>
/// Why: unlike the generic DataCommands (Insert/Update/...), this command lives outside
/// Fdw.Commands.Data, so it can only be discovered by the cross-assembly TypeOption module
/// initializer, which instantiates every option via a bare <c>new()</c> call and requires a
/// public constructor of exactly zero declared parameters (FDW027). Properties are therefore
/// settable, populated by the caller after construction — the same pattern every Roslyn command
/// (e.g. RenameCommand) already uses for this exact reason.
/// </para>
/// </remarks>
[TypeOption(typeof(DataCommands), "SoapRequest")]
public sealed class SoapRequestCommand : DataCommandBase<XElement>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SoapRequestCommand"/> class.
    /// </summary>
    public SoapRequestCommand()
        : base("SoapRequest")
    {
    }

    /// <summary>
    /// Gets or sets the pre-built SOAP body content.
    /// </summary>
    public XElement Body { get; set; } = new("Empty");

    /// <summary>
    /// Gets or sets the SOAPAction header value.
    /// </summary>
    public string SoapAction { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional HTTP headers to include in the request.
    /// </summary>
    public IReadOnlyDictionary<string, string?> AdditionalHeaders { get; set; } =
        new Dictionary<string, string?>(System.StringComparer.OrdinalIgnoreCase);
}
