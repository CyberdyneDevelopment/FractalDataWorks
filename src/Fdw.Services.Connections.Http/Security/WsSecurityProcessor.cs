using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.Results;
using Fdw.Services.Connections.Http.Protocols;

namespace Fdw.Services.Connections.Http.Security;

/// <summary>
/// WS-Security processor that adds timestamp, certificate, and XML signature to SOAP envelopes.
/// </summary>
/// <remarks>
/// <para>
/// This processor implements the WS-Security 1.0/1.1 specification for X.509 certificate-based
/// message security. The processing steps are:
/// </para>
/// <list type="number">
/// <item><description>Ensure Body has wsu:Id attribute for signing</description></item>
/// <item><description>Create wsse:Security header</description></item>
/// <item><description>Add wsu:Timestamp with Created and Expires</description></item>
/// <item><description>Add wsse:BinarySecurityToken with certificate</description></item>
/// <item><description>Compute XML signature over Body and Timestamp</description></item>
/// <item><description>Add ds:Signature to Security header</description></item>
/// </list>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SoapSecurityProcessors), "WsSecurity")]
public sealed class WsSecurityProcessor : SoapSecurityProcessorBase
{
    private static readonly XNamespace WsseNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private static readonly XNamespace WsuNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    private const string X509TokenProfile = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3";
    private const string Base64Encoding = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary";

    /// <summary>
    /// Initializes a new instance of the <see cref="WsSecurityProcessor"/> class.
    /// </summary>
    public WsSecurityProcessor()
        : base(2, "WsSecurity", "WS-Security with X.509 certificate signing")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<XDocument>> Process(
        XDocument envelope,
        SoapVersion soapVersion,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var certificate = context.ResolvedCertificate;
            if (certificate is null)
            {
                return Task.FromResult(GenericResult<XDocument>.Failure(
                    HttpResultCodes.ByName("WsSecurityMissingCertificate")));
            }

            var bodyId = $"Body-{Guid.NewGuid():N}";
            var timestampId = $"TS-{Guid.NewGuid():N}";
            var bstId = $"X509-{Guid.NewGuid():N}";

            var body = envelope.Root?.Element(soapVersion.EnvelopeNamespace + "Body");
            if (body is null)
            {
                return Task.FromResult(GenericResult<XDocument>.Failure(
                    HttpResultCodes.ByName("WsSecurityMissingBody")));
            }
            body.SetAttributeValue(WsuNamespace + "Id", bodyId);

            AddSecurityHeader(envelope, soapVersion, context, certificate, bodyId, timestampId, bstId);
            var result = SignAndConvert(envelope, certificate, bodyId, timestampId, bstId);

            return Task.FromResult(GenericResult<XDocument>.Success(result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<XDocument>.Failure(
                HttpResultCodes.ByName("WsSecurityProcessingFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message)));
        }
    }

    private static void AddSecurityHeader(
        XDocument envelope,
        SoapVersion soapVersion,
        HttpProtocolContext context,
        X509Certificate2 certificate,
        string bodyId,
        string timestampId,
        string bstId)
    {
        var header = envelope.Root?.Element(soapVersion.EnvelopeNamespace + "Header");
        if (header is null)
        {
            header = new XElement(soapVersion.EnvelopeNamespace + "Header");
            envelope.Root?.AddFirst(header);
        }

        int ttlSeconds = 300;
        if (context.Configuration is HttpConnectionConfigurationBase httpConfig
            && httpConfig.AdditionalProperties is { Count: > 0 } secValues
            && secValues.TryGetValue("TimestampTtlSeconds", out var ttlStr)
            && int.TryParse(ttlStr, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var parsedTtl))
        {
            ttlSeconds = parsedTtl;
        }
        var now = DateTime.UtcNow;
        var expires = now.AddSeconds(ttlSeconds);

        var securityHeader = new XElement(WsseNamespace + "Security",
            new XAttribute(XNamespace.Xmlns + "wsse", WsseNamespace),
            new XAttribute(XNamespace.Xmlns + "wsu", WsuNamespace),
            new XAttribute(soapVersion.EnvelopeNamespace + "mustUnderstand", soapVersion.MustUnderstandValue),
            new XElement(WsuNamespace + "Timestamp",
                new XAttribute(WsuNamespace + "Id", timestampId),
                new XElement(WsuNamespace + "Created", now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture)),
                new XElement(WsuNamespace + "Expires", expires.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture))),
            new XElement(WsseNamespace + "BinarySecurityToken",
                new XAttribute("EncodingType", Base64Encoding),
                new XAttribute("ValueType", X509TokenProfile),
                new XAttribute(WsuNamespace + "Id", bstId),
                Convert.ToBase64String(certificate.RawData)));

        header.AddFirst(securityHeader);
    }

    private static XDocument SignAndConvert(
        XDocument envelope,
        X509Certificate2 certificate,
        string bodyId,
        string timestampId,
        string bstId)
    {
        var xmlDoc = new XmlDocument { PreserveWhitespace = true };
        using (var reader = envelope.CreateReader())
        {
            xmlDoc.Load(reader);
        }

        var signedXml = CreateSignedXml(xmlDoc, certificate, bodyId, timestampId, bstId);
        signedXml.ComputeSignature();

        var signatureXml = signedXml.GetXml();
        var securityElement = xmlDoc.GetElementsByTagName("Security", WsseNamespace.NamespaceName)[0];
        var importedSig = xmlDoc.ImportNode(signatureXml, true);
        securityElement?.AppendChild(importedSig);

        using var nodeReader = new XmlNodeReader(xmlDoc);
        return XDocument.Load(nodeReader);
    }

    private static SignedXml CreateSignedXml(
        XmlDocument doc,
        X509Certificate2 certificate,
        string bodyId,
        string timestampId,
        string bstId)
    {
        var privateKey = certificate.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("Certificate does not contain an RSA private key");

        var signedXml = new SignedXml(doc)
        {
            SigningKey = privateKey
        };

        // Configure signature method
        signedXml.SignedInfo!.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;

        // Add reference to Body
        var bodyRef = new Reference($"#{bodyId}")
        {
            DigestMethod = SignedXml.XmlDsigSHA256Url
        };
        bodyRef.AddTransform(new XmlDsigExcC14NTransform());
        signedXml.AddReference(bodyRef);

        // Add reference to Timestamp
        var tsRef = new Reference($"#{timestampId}")
        {
            DigestMethod = SignedXml.XmlDsigSHA256Url
        };
        tsRef.AddTransform(new XmlDsigExcC14NTransform());
        signedXml.AddReference(tsRef);

        // Add KeyInfo with SecurityTokenReference
        var keyInfo = new KeyInfo();
        var secTokenRef = doc.CreateElement("wsse", "SecurityTokenReference", WsseNamespace.NamespaceName);
        var reference = doc.CreateElement("wsse", "Reference", WsseNamespace.NamespaceName);
        reference.SetAttribute("URI", $"#{bstId}");
        reference.SetAttribute("ValueType", X509TokenProfile);
        secTokenRef.AppendChild(reference);

        keyInfo.AddClause(new KeyInfoNode(secTokenRef));
        signedXml.KeyInfo = keyInfo;

        return signedXml;
    }
}
