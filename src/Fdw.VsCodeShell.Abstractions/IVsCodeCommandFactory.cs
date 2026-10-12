using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.VsCodeShell.Abstractions;

/// <summary>
/// Generic marker factory interface for VS Code command types.
/// Each concrete <typeparamref name="THandler"/> produces a unique closed generic type, which ensures
/// <c>ServiceTypeBase&lt;TService, TFactory&gt;.Id</c> computes a unique GUID per command.
/// </summary>
/// <typeparam name="THandler">The concrete handler that implements the command's behaviour
/// (e.g., <c>OpenCanvasHandler</c>).</typeparam>
/// <remarks>
/// <para>
/// This interface has no members — it exists purely as a type-level discriminator for
/// <see cref="VsCodeCommandTypeBase{THandler}"/>. It extends <see cref="IServiceFactory{TService}"/>
/// to satisfy the <c>ServiceTypeBase&lt;TService, TFactory&gt;</c> constraint.
/// </para>
/// <para>
/// Do not delete this as unused. <c>ServiceTypeBase.Id</c> is <c>MD5($"{TService.FullName}:{TFactory.FullName}")</c>
/// and the generated <c>RegisterMember</c> discards a second option whose Id already exists — so two
/// commands closing the same factory type would leave one silently unregistered. <c>ST001</c> catches that
/// only within a single compilation; commands declared in a downstream package are invisible to it.
/// </para>
/// </remarks>
public interface IVsCodeCommandFactory<THandler> : IServiceFactory<IGenericService, IServiceConfiguration>
    where THandler : class, IVsCodeCommandHandler
{
}
