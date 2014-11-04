using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CancellationAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CancellationTokenMustBePassedExplicitlyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CT1003";
        public const string MessageFormat = "Do not omit CancellationToken arguments";
        internal const string Title = "CancellationToken Arguments Should Not Be Omitted";
        internal const string Category = "Responsiveness";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var cancellationTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
                if (cancellationTokenType != null)
                {
                    compilationContext.RegisterCodeBlockStartAction<SyntaxKind>(codeBlockContext =>
                    {
                        codeBlockContext.RegisterSyntaxNodeAction(syntaxNodeContext =>
                        {
                            var symbolInfo = syntaxNodeContext.SemanticModel.GetSymbolInfo(syntaxNodeContext.Node, syntaxNodeContext.CancellationToken);
                            if (symbolInfo.Symbol != null &&
                                symbolInfo.Symbol.Kind == SymbolKind.Method)
                            {
                                var methodSymbol = (IMethodSymbol)symbolInfo.Symbol;
                                var invocation = (InvocationExpressionSyntax)syntaxNodeContext.Node;
                                var specifiedArguments = DetermineSpecifiedArguments(methodSymbol, invocation);

                                foreach (var cancellationParam in methodSymbol.Parameters.Where(p => p.Type.Equals(cancellationTokenType)))
                                {
                                    if (!specifiedArguments.Contains(cancellationParam.Name))
                                    {
                                        syntaxNodeContext.ReportDiagnostic(Diagnostic.Create
                                            (Rule, invocation.GetLocation()));
                                    }
                                }
                            }
                        },
                        SyntaxKind.InvocationExpression);
                    });
                }
            });
        }

        private static HashSet<string> DetermineSpecifiedArguments(IMethodSymbol methodSymbol, InvocationExpressionSyntax invocation)
        {
            var specifiedArguments = new HashSet<string>();
            for (var i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
            {
                var arg = invocation.ArgumentList.Arguments[i];
                if (arg.NameColon == null)
                {
                    specifiedArguments.Add(methodSymbol.Parameters[i].Name);
                }
                else
                {
                    specifiedArguments.Add(arg.NameColon.Name.Identifier.Text);
                }
            }

            return specifiedArguments;
        }
    }
}
