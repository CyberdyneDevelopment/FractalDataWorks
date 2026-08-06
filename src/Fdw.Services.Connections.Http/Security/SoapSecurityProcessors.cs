using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Security;

/// <summary>
/// TypeCollection for SOAP security processors.
/// </summary>
/// <remarks>
/// Available security processors:
/// <list type="bullet">
/// <item><description>None - No security applied (pass-through)</description></item>
/// <item><description>WsSecurity - WS-Security with timestamp, certificate, and signature</description></item>
/// <item><description>UsernameToken - WS-Security UsernameToken (username/password in header)</description></item>
/// </list>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SoapSecurityProcessorBase), typeof(ISoapSecurityProcessor), typeof(SoapSecurityProcessors))]
public abstract partial class SoapSecurityProcessors : TypeCollectionBase<SoapSecurityProcessorBase, ISoapSecurityProcessor>
{
}
