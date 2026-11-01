using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Search.Commands;
using Fdw.Roslyn.Commands.Search.Logging;
using Fdw.Roslyn.Commands.Search.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Roslyn.Commands.Search.Translators;

/// <summary>
/// Translator for the <see cref="InspectFamilyContractCommand"/>.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "InspectFamilyContract")]
public sealed class InspectFamilyContractTranslator : RoslynCommandTranslatorBase<InspectFamilyContractCommand, QueryResult<FamilyContract>>
{
    private readonly ILogger<InspectFamilyContractTranslator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InspectFamilyContractTranslator"/> class.
    /// </summary>
    /// <remarks>
    /// Why: a genuinely zero-parameter overload — not just an optional-parameter one — is required
    /// because the cross-assembly TypeOption module initializer instantiates every translator via a
    /// bare <c>new()</c> call and only discovers types with a constructor of exactly zero declared
    /// parameters (FDW027). An <c>(ILogger? logger = null)</c>-only constructor has Parameters.Length
    /// == 1 and is silently skipped.
    /// </remarks>
    public InspectFamilyContractTranslator()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InspectFamilyContractTranslator"/> class.
    /// </summary>
    /// <param name="logger">Optional logger; falls back to <see cref="NullLogger{T}.Instance"/> if not injected.</param>
    public InspectFamilyContractTranslator(ILogger<InspectFamilyContractTranslator>? logger)
        : base("InspectFamilyContract", "Inspects the public surface of a family root type (interface or abstract base)")
    {
        _logger = logger ?? NullLogger<InspectFamilyContractTranslator>.Instance;
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<FamilyContract>>> Translate(
        InspectFamilyContractCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        InspectFamilyContractTranslatorLog.TranslateStart(_logger, command.TypeName);

        if (string.IsNullOrEmpty(command.TypeName))
        {
            InspectFamilyContractTranslatorLog.ValidationFailedTypeNameRequired(_logger);
            return GenericResult<QueryResult<FamilyContract>>.Failure(
                RoslynResultCodes.ByName("ClassNameRequired"));
        }

        var typeSymbol = await FamilyTypeResolver.Resolve(command.TypeName, solution, cancellationToken, _logger).ConfigureAwait(false);
        if (typeSymbol is null)
        {
            InspectFamilyContractTranslatorLog.TypeNotFound(_logger, command.TypeName);
            return GenericResult<QueryResult<FamilyContract>>.Failure(
                RoslynResultCodes.ByName("FailedToGetTypeSymbol"),
                ResultDetails.Create().With("TypeName", command.TypeName));
        }

        var kindLabel = FamilyMemberHelpers.DescribeTypeKind(typeSymbol, _logger);
        InspectFamilyContractTranslatorLog.TypeResolved(_logger, typeSymbol.ToDisplayString(), kindLabel, typeSymbol.IsAbstract, typeSymbol.IsSealed);

        var members = FamilyMemberHelpers.GetDeclaredPublicMembers(typeSymbol, _logger)
            .Select(m => FamilyMemberHelpers.ToContractMember(m, _logger))
            .ToList();
        InspectFamilyContractTranslatorLog.MembersEnumerated(_logger, members.Count);

        var genericParameters = typeSymbol.TypeParameters
            .Select(p => FamilyMemberHelpers.DescribeTypeParameter(p, _logger))
            .ToList();
        InspectFamilyContractTranslatorLog.GenericParametersEnumerated(_logger, genericParameters.Count);

        var baseTypes = new List<string>();
        for (var current = typeSymbol.BaseType; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            baseTypes.Add(current.ToDisplayString());
        }
        InspectFamilyContractTranslatorLog.BaseChainWalked(_logger, baseTypes.Count);

        var implementedInterfaces = typeSymbol.Interfaces
            .Select(i => i.ToDisplayString())
            .ToList();
        InspectFamilyContractTranslatorLog.InterfacesEnumerated(_logger, implementedInterfaces.Count);

        var location = typeSymbol.Locations.FirstOrDefault(l => l.IsInSource);
        var lineSpan = location?.GetLineSpan();
        var filePath = lineSpan?.Path ?? string.Empty;
        var line = lineSpan is null ? 0 : lineSpan.Value.StartLinePosition.Line + 1;

        var contract = new FamilyContract(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(),
            kindLabel,
            typeSymbol.IsAbstract,
            typeSymbol.IsSealed,
            genericParameters,
            baseTypes,
            implementedInterfaces,
            members,
            filePath,
            line);

        var result = new QueryResult<FamilyContract>(
            $"Contract for '{typeSymbol.Name}': {members.Count} member(s)",
            contract);

        InspectFamilyContractTranslatorLog.TranslateSuccess(_logger, typeSymbol.Name, members.Count);
        return GenericResult<QueryResult<FamilyContract>>.Success(result);
    }
}
