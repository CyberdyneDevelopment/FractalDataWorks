using System;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.RowQuery;

/// <summary>
/// Validates that every column a filter condition or join condition references is a DECLARED field on
/// the relevant container's schema — the container's <see cref="IDataNode.Nodes"/> field children
/// ARE the schema, and a filter/join referencing an undeclared column is a schema/configuration error,
/// not a "no match" data condition.
/// </summary>
/// <remarks>
/// Why this exists: without this check, a filter or join column absent from the target container's
/// declared schema (a typo, or a schema/query mismatch) resolves to <see langword="null"/> when read
/// from a row and is silently treated as "no match" by <see cref="RecordRowMatcher"/> — the read then
/// returns zero rows and still reports SUCCESS. Combined with <see cref="RecordRowValidator"/> (which
/// guarantees every DECLARED non-nullable field is actually present on every row), this closes the gap:
/// a column that is genuinely part of the schema and required is guaranteed present; a column that is
/// NOT part of the schema at all is now a fail-loud configuration error instead of a silent empty match.
/// </remarks>
public static class RecordColumnValidator
{
    /// <summary>
    /// Validates the join's field pair: the child-side field must be declared on
    /// <paramref name="primaryContainer"/>; the parent-side field must be declared on
    /// <paramref name="joinedContainer"/>.
    /// </summary>
    public static IGenericResult ValidateJoinColumns(
        string leftField,
        string rightField,
        IDataContainer primaryContainer,
        IDataContainer joinedContainer,
        ILogger logger)
    {
        if (!ContainerFields.Declares(primaryContainer, leftField))
            return GenericResult.Failure(RecordQueryLog.JoinColumnNotDeclared(logger, leftField, primaryContainer.Name));

        if (!ContainerFields.Declares(joinedContainer, rightField))
            return GenericResult.Failure(RecordQueryLog.JoinColumnNotDeclared(logger, rightField, joinedContainer.Name));

        return GenericResult.Success();
    }

    /// <summary>
    /// Walks the (already grammar-validated — see <see cref="RecordQueryValidator"/>) filter tree and
    /// validates every referenced column against the relevant container's declared fields: a bare column
    /// against <paramref name="primaryContainer"/>, a <c>"{parentContainerName}.Column"</c>-qualified
    /// one against <paramref name="joinedContainer"/>.
    /// </summary>
    public static IGenericResult ValidateFilterColumns(
        IFilterNode? node,
        IDataContainer primaryContainer,
        IDataContainer? joinedContainer,
        string? parentContainerName,
        ILogger logger)
    {
        switch (node)
        {
            case null:
                return GenericResult.Success();

            case FilterCondition condition:
                return ValidateCondition(condition, primaryContainer, joinedContainer, parentContainerName, logger);

            case FilterGroup group:
                foreach (var child in group.Nodes)
                {
                    var childResult = ValidateFilterColumns(child, primaryContainer, joinedContainer, parentContainerName, logger);
                    if (!childResult.IsSuccess)
                        return childResult;
                }
                return GenericResult.Success();

            // Why: RecordQueryValidator.ValidateShape already rejects any node type unreachable here —
            // reaching this point with an unrecognised node means that upstream grammar check has a gap,
            // not that this is a data condition. Never silently pass an unvalidated column.
            default:
                return GenericResult.Failure(RecordQueryLog.UnsupportedFilterNodeType(logger, node.GetType().Name));
        }
    }

    private static IGenericResult ValidateCondition(
        FilterCondition condition,
        IDataContainer primaryContainer,
        IDataContainer? joinedContainer,
        string? parentContainerName,
        ILogger logger)
    {
        var (qualifier, column) = QualifiedColumn.Split(condition.PropertyName);

        if (qualifier is null)
        {
            return ContainerFields.Declares(primaryContainer, column)
                ? GenericResult.Success()
                : GenericResult.Failure(RecordQueryLog.FilterColumnNotDeclared(logger, column, primaryContainer.Name));
        }

        if (parentContainerName is not null && string.Equals(qualifier, parentContainerName, StringComparison.Ordinal))
        {
            if (joinedContainer is null)
                return GenericResult.Failure(RecordQueryLog.UnknownFilterQualifier(logger, qualifier, condition.PropertyName));

            return ContainerFields.Declares(joinedContainer, column)
                ? GenericResult.Success()
                : GenericResult.Failure(RecordQueryLog.FilterColumnNotDeclared(logger, column, joinedContainer.Name));
        }

        return GenericResult.Failure(RecordQueryLog.UnknownFilterQualifier(logger, qualifier, condition.PropertyName));
    }
}
