using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.Commands;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Fdw.Services.Connections.Http.Abstractions.Results;
using Fdw.Services.Connections.Http.Security;

namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// Base class for SOAP protocol implementations.
/// </summary>
/// <remarks>
/// <para>
/// This base class handles common SOAP concerns:
/// <list type="bullet">
/// <item><description>SOAP envelope construction</description></item>
/// <item><description>WS-Security header integration</description></item>
/// <item><description>Fault detection and error mapping</description></item>
/// <item><description>Response body extraction</description></item>
/// </list>
/// </para>
/// <para>
/// Derived classes override virtual methods to customize:
/// <list type="bullet">
/// <item><description><see cref="BuildSoapBody"/> - Build service-specific body content</description></item>
/// <item><description><see cref="GetSoapAction"/> - Determine the SOAPAction header value</description></item>
/// <item><description><see cref="ExtractResultFromBody"/> - Extract results from SOAP body</description></item>
/// </list>
/// </para>
/// <para>
/// The SOAP message creation sequence:
/// <code>
/// 1. BuildSoapBody()           → Service-specific body content (virtual)
/// 2. WrapInEnvelope()          → SOAP:Envelope with Body
/// 3. ApplySecurity()           → WS-Security header + signing (if configured)
/// 4. BuildHttpRequest()        → POST with SOAPAction header
/// </code>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage] // Abstract base requiring HTTP infrastructure - tested through integration tests
public abstract class SoapProtocolBase : HttpProtocolBase
{
    /// <summary>
    /// Gets the SOAP version for this protocol.
    /// </summary>
    public abstract SoapVersion Version { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SoapProtocolBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the protocol.</param>
    /// <param name="name">The name of the protocol.</param>
    /// <param name="description">The description of the protocol.</param>
    /// <param name="contentType">The content type for SOAP requests.</param>
    protected SoapProtocolBase(int id, string name, string description, string contentType)
        : base(id, name, description, contentType)
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<HttpRequestMessage>> Translate(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Build the SOAP body content (service-specific)
            var bodyResult = await BuildSoapBody(command, container, context, cancellationToken).ConfigureAwait(false);
            if (!bodyResult.IsSuccess)
            {
                return bodyResult.ToNewResult<HttpRequestMessage>();
            }

            // Step 2: Wrap in SOAP envelope
            var envelope = WrapInEnvelope(bodyResult.Value!, context);

            // Step 3: Apply WS-Security if configured
            var securityType = (context.Configuration as HttpConnectionConfigurationBase)?.AuthenticationType;
            if (!string.IsNullOrEmpty(securityType) && !string.Equals(securityType, "None", StringComparison.OrdinalIgnoreCase))
            {
                var securityResult = await ApplySecurity(envelope, context, cancellationToken).ConfigureAwait(false);
                if (!securityResult.IsSuccess)
                {
                    return securityResult.ToNewResult<HttpRequestMessage>();
                }
                envelope = securityResult.Value!;
            }

            // Step 4: Build HTTP request
            var endpoint = GetRequestPath(command, container, context);
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(envelope.ToString(), Encoding.UTF8, DefaultContentType)
            };

            // Add SOAPAction header
            var soapAction = GetSoapAction(command, container, context);
            if (!string.IsNullOrEmpty(soapAction))
            {
                request.Headers.Add("SOAPAction", $"\"{soapAction}\"");
            }

            return GenericResult<HttpRequestMessage>.Success(request);
        }
        catch (Exception ex)
        {
            return GenericResult<HttpRequestMessage>.Failure(
                HttpResultCodes.ByName("SoapRequestBuildFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<object?>> ProcessResponse(
        HttpResponseMessage response,
        IStorageContainer container,
        Type resultType,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(content))
        {
            // For SOAP, empty response on success status is unusual but possible
            if (response.IsSuccessStatusCode)
            {
                return GenericResult<object?>.Success(null);
            }
            return GenericResult<object?>.Failure(
                HttpResultCodes.ByName("SoapHttpError"),
                ResultDetails.Create()
                    .With("StatusCode", (int)response.StatusCode)
                    .With("ReasonPhrase", response.ReasonPhrase ?? "Unknown"));
        }

        XDocument doc;
        try
        {
            doc = XDocument.Parse(content);
        }
        catch (Exception ex)
        {
            return GenericResult<object?>.Failure(
                HttpResultCodes.ByName("SoapResponseParseFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }

        // Check for SOAP fault (can occur even with HTTP 200)
        var faultResult = CheckForSoapFault(doc);
        if (!faultResult.IsSuccess)
        {
            return faultResult.ToNewResult<object?>();
        }

        // Extract body element
        var body = ExtractSoapBody(doc);
        if (body is null)
        {
            return GenericResult<object?>.Failure(HttpResultCodes.ByName("SoapMissingBody"));
        }

        // Delegate to virtual method for service-specific extraction
        return await ExtractResultFromBody(body, resultType, context, cancellationToken).ConfigureAwait(false);
    }

    #region Virtual Extension Points for Derived Classes

    /// <summary>
    /// Builds the SOAP body content for the request.
    /// </summary>
    /// <remarks>
    /// Override this method to build service-specific body content.
    /// For example, ERCOT would build its RequestMessage XML here.
    /// </remarks>
    /// <param name="command">The data command being translated.</param>
    /// <param name="container">The storage container.</param>
    /// <param name="context">The protocol context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The body content as an XElement.</returns>
    protected virtual Task<IGenericResult<XElement>> BuildSoapBody(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        // Short-circuit: SoapRequestCommand carries a pre-built body
        if (command is SoapRequestCommand soapCmd)
            return Task.FromResult<IGenericResult<XElement>>(
                GenericResult<XElement>.Success(soapCmd.Body));

        // Default: create a simple element from the command
        var operationName = command.Metadata.GetValueOrDefault("Operation") as string
            ?? command.CommandType;
        var body = new XElement(operationName);

        // Add command input data if present
        if (command is IDataCommandWithInput commandWithInput && commandWithInput.InputData is not null)
        {
            // Serialize input data as child elements
            // This is a simplified implementation - real usage would need proper XML serialization
            body.Add(new XElement("Data", commandWithInput.InputData.ToString()));
        }

        return Task.FromResult(GenericResult<XElement>.Success(body));
    }

    /// <summary>
    /// Gets the SOAPAction header value for the request.
    /// </summary>
    /// <param name="command">The data command being translated.</param>
    /// <param name="container">The storage container.</param>
    /// <param name="context">The protocol context.</param>
    /// <returns>The SOAPAction value, or null for no header.</returns>
    protected virtual string? GetSoapAction(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context)
    {
        // Prefer explicit SOAPAction from SoapRequestCommand
        if (command is SoapRequestCommand soapCmd &&
            !string.IsNullOrEmpty(soapCmd.SoapAction))
            return soapCmd.SoapAction;

        // Default: use configured pattern or operation name
        var pattern = (context.Configuration as HttpConnectionConfigurationBase)?.Soap?.SoapActionPattern;
        if (!string.IsNullOrEmpty(pattern))
        {
            return pattern
                .Replace("{operation}", command.CommandType)
                .Replace("{container}", container.Name);
        }

        return command.Metadata.GetValueOrDefault("SoapAction") as string;
    }

    /// <summary>
    /// Extracts the result from the SOAP body element.
    /// </summary>
    /// <remarks>
    /// Override this method to handle service-specific response formats.
    /// For example, ERCOT would extract from ResponseMessage/Payload here.
    /// </remarks>
    /// <param name="body">The SOAP body element.</param>
    /// <param name="resultType">The expected result type.</param>
    /// <param name="context">The protocol context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The extracted result.</returns>
    protected virtual Task<IGenericResult<object?>> ExtractResultFromBody(
        XElement body,
        Type resultType,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        // Default: return body as string if that's the expected type
        if (resultType == typeof(string))
        {
            return Task.FromResult(GenericResult<object?>.Success(body.ToString()));
        }

        if (resultType == typeof(XElement))
        {
            return Task.FromResult(GenericResult<object?>.Success(body));
        }

        // For other types, try to deserialize the first child element
        var firstChild = body.Elements().FirstOrDefault();
        if (firstChild is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        // Return as string for now - full implementation would use XML serialization
        return Task.FromResult(GenericResult<object?>.Success(firstChild.ToString()));
    }

    #endregion

    #region SOAP Envelope Handling

    /// <summary>
    /// Wraps content in a SOAP envelope.
    /// </summary>
    protected XDocument WrapInEnvelope(XElement bodyContent, HttpProtocolContext context)
    {
        var envelope = new XElement(Version.EnvelopeNamespace + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soap", Version.EnvelopeNamespace),
            new XElement(Version.EnvelopeNamespace + "Header"),
            new XElement(Version.EnvelopeNamespace + "Body", bodyContent));

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), envelope);
    }

    /// <summary>
    /// Extracts the Body element from a SOAP envelope.
    /// </summary>
    protected XElement? ExtractSoapBody(XDocument doc)
    {
        return doc.Root?
            .Element(Version.EnvelopeNamespace + "Body");
    }

    /// <summary>
    /// Checks for SOAP fault in the response.
    /// </summary>
    protected virtual IGenericResult CheckForSoapFault(XDocument doc)
    {
        var body = doc.Root?.Element(Version.EnvelopeNamespace + "Body");
        var fault = body?.Element(Version.EnvelopeNamespace + "Fault");

        if (fault is null)
        {
            return GenericResult.Success();
        }

        // Extract fault information (differs between SOAP 1.1 and 1.2)
        var faultCode = fault.Element("faultcode")?.Value
            ?? fault.Element(Version.EnvelopeNamespace + "Code")?.Element(Version.EnvelopeNamespace + "Value")?.Value
            ?? "Unknown";

        var faultString = fault.Element("faultstring")?.Value
            ?? fault.Element(Version.EnvelopeNamespace + "Reason")?.Element(Version.EnvelopeNamespace + "Text")?.Value
            ?? "Unknown SOAP fault";

        return GenericResult.Failure(
            HttpResultCodes.ByName("SoapFault"),
            ResultDetails.Create()
                .With("FaultCode", faultCode)
                .With("FaultString", faultString));
    }

    /// <summary>
    /// Applies WS-Security to the SOAP envelope.
    /// </summary>
    protected virtual async Task<IGenericResult<XDocument>> ApplySecurity(
        XDocument envelope,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        var securityType = (context.Configuration as HttpConnectionConfigurationBase)?.AuthenticationType;
        if (string.IsNullOrEmpty(securityType))
        {
            return GenericResult<XDocument>.Success(envelope);
        }

        var processor = SoapSecurityProcessors.ByName(securityType);
        if (processor == SoapSecurityProcessors.NotFound)
        {
            return GenericResult<XDocument>.Failure(
                HttpResultCodes.ByName("UnknownSecurityType"),
                ResultDetails.Create().With("SecurityType", securityType));
        }

        return await processor.Process(envelope, Version, context, cancellationToken).ConfigureAwait(false);
    }

    #endregion
}
