using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fdw.CodeBuilder.Abstractions;

namespace Fdw.CodeBuilder.CSharp.Builders;

/// <summary>
/// Builder for generating C# constructor definitions.
/// </summary>
public sealed class ConstructorBuilder : CodeBuilderBase, IConstructorBuilder
{
    private static readonly string[] NewLineSeparators = ["\r\n", "\n"];
    private string _className = "MyClass";
    private string _accessModifier = "public";
    private bool _isStatic;
    private readonly List<(string Type, string Name, string? Default)> _parameters = [];
    private readonly List<string> _baseCallArguments = [];
    private readonly List<string> _thisCallArguments = [];
    private readonly List<string> _attributes = [];
    private readonly List<string> _bodyLines = [];
    private string? _xmlDocSummary;
    private readonly Dictionary<string, string> _paramDocs = new(StringComparer.Ordinal);

    /// <summary>
    /// Sets the class name for the constructor.
    /// </summary>
    /// <param name="className">The class name.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConstructorBuilder WithClassName(string className)
    {
        _className = className;
        return this;
    }

    /// <inheritdoc/>
    public IConstructorBuilder WithAccessModifier(string accessModifier)
    {
        _accessModifier = accessModifier;
        return this;
    }

    /// <inheritdoc/>
    public IConstructorBuilder AsStatic()
    {
        _isStatic = true;
        return this;
    }

    /// <inheritdoc/>
    public IConstructorBuilder WithParameter(string type, string name, string? defaultValue = null)
    {
        _parameters.Add((type, name, defaultValue));
        return this;
    }

    /// <inheritdoc/>
    public IConstructorBuilder WithBaseCall(params string[] arguments)
    {
        _baseCallArguments.Clear();
        _baseCallArguments.AddRange(arguments);
        _thisCallArguments.Clear();
        return this;
    }

    /// <inheritdoc/>
    public IConstructorBuilder WithThisCall(params string[] arguments)
    {
        _thisCallArguments.Clear();
        _thisCallArguments.AddRange(arguments);
        _baseCallArguments.Clear();
        return this;
    }

    /// <inheritdoc/>
    public IConstructorBuilder WithAttribute(string attribute)
    {
        _attributes.Add(attribute);
        return this;
    }

    /// <inheritdoc/>
    public IConstructorBuilder WithBody(string body)
    {
        _bodyLines.Clear();
        if (!string.IsNullOrWhiteSpace(body))
        {
            _bodyLines.AddRange(body.Split('\n'));
        }
        return this;
    }

    /// <inheritdoc/>
    public IConstructorBuilder AddBodyLine(string line)
    {
        _bodyLines.Add(line);
        return this;
    }

    /// <inheritdoc/>
    public IConstructorBuilder WithXmlDoc(string summary)
    {
        _xmlDocSummary = summary;
        return this;
    }

    /// <inheritdoc/>
    public IConstructorBuilder WithParamDoc(string parameterName, string description)
    {
        _paramDocs[parameterName] = description;
        return this;
    }

    /// <inheritdoc/>
    public override string Build()
    {
        Clear();

        BuildXmlDocumentation();
        BuildAttributes();
        BuildConstructorSignature();
        BuildConstructorBody();

        return Builder.ToString().TrimEnd();
    }

    private void BuildXmlDocumentation()
    {
        if (!string.IsNullOrEmpty(_xmlDocSummary) || _paramDocs.Count > 0)
        {
            if (!string.IsNullOrEmpty(_xmlDocSummary))
            {
                AppendLine("/// <summary>");
                foreach (var line in _xmlDocSummary!.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries))
                {
                    AppendLine($"/// {EscapeXmlEntities(line.Trim())}");
                }
                AppendLine("/// </summary>");
            }

            foreach (var param in _parameters)
            {
                if (_paramDocs.TryGetValue(param.Name, out var paramDoc))
                {
                    AppendLine($"/// <param name=\"{param.Name}\">{EscapeXmlEntities(paramDoc)}</param>");
                }
            }
        }
    }

    private void BuildAttributes()
    {
        foreach (var attribute in _attributes)
        {
            AppendLine($"[{attribute}]");
        }
    }

    private void BuildConstructorSignature()
    {
        var signature = new StringBuilder();

        if (_isStatic)
        {
            signature.Append("static ");
            signature.Append(_className);
        }
        else
        {
            signature.Append(_accessModifier);
            signature.Append($" {_className}");
        }

        BuildParameterList(signature);
        BuildConstructorChain(signature);

        AppendLine(signature.ToString());
    }

    private void BuildParameterList(StringBuilder signature)
    {
        signature.Append('(');

        var paramStrings = _parameters.Select(p =>
        {
            var paramStr = $"{p.Type} {p.Name}";
            if (p.Default != null)
                paramStr += $" = {p.Default}";
            return paramStr;
        });

        signature.Append(string.Join(", ", paramStrings));
        signature.Append(')');
    }

    private void BuildConstructorChain(StringBuilder signature)
    {
        if (_baseCallArguments.Count > 0)
        {
            signature.Append($" : base({string.Join(", ", _baseCallArguments)})");
        }
        else if (_thisCallArguments.Count > 0)
        {
            signature.Append($" : this({string.Join(", ", _thisCallArguments)})");
        }
    }

    private void BuildConstructorBody()
    {
        AppendLine("{");
        Indent();

        foreach (var line in _bodyLines)
        {
            AppendLine(line.TrimEnd());
        }

        Outdent();
        AppendLine("}");
    }
}
