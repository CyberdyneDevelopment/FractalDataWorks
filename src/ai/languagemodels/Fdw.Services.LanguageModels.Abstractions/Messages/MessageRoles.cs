using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// The TypeCollection of who a message in a conversation is from. A consumer's own provider package adds options in its own assembly.
/// </summary>
[TypeCollection(typeof(MessageRoleBase), typeof(IMessageRole), typeof(MessageRoles))]
[ExcludeFromCodeCoverage]
public abstract partial class MessageRoles : TypeCollectionBase<MessageRoleBase, IMessageRole>
{
}
