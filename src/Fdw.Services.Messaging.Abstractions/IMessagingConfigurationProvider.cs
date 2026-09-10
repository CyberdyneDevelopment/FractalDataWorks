using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messaging.Abstractions;

/// <summary>
/// Resolves configured messaging services and routes each to the implementation provider that owns it.
/// </summary>
/// <remarks>
/// Named rather than a bare closed generic: a constructor asking for this states which rows it reads.
/// Two providers over different tables that share a shape are interchangeable at a call site when both
/// are spelled <c>IImplementationConfigurationProvider&lt;T&gt;</c>, and nothing catches the swap.
/// </remarks>
public interface IMessagingConfigurationProvider
    : IImplementationConfigurationProvider<IMessagingImplementationConfiguration>
{

}
