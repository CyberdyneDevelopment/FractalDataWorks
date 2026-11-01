using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Build.Commands;
using Fdw.Sql.Commands.Logging;
using Fdw.Sql.Workspace;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Sql.Commands.Build.Translators;

[TypeOption(typeof(SqlBuildTranslators), "ValidateSyntax", RestrictToCurrentCompilation = true)]
public sealed class ValidateSyntaxTranslator : SqlCommandTranslatorBase<ValidateSyntaxCommand, QueryResult<string>>
{
    public ValidateSyntaxTranslator() : base("ValidateSyntax", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(ValidateSyntaxCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<ValidateSyntaxTranslator>.Instance;
        ValidateSyntaxTranslatorLog.Translating(logger, nameof(ValidateSyntaxCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(ValidateSyntaxTranslatorLog.NotYetImplemented(logger, nameof(ValidateSyntaxCommand))));
    }
}
