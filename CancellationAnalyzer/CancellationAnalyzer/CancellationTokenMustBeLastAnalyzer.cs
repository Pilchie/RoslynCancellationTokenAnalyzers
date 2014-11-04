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
    public class CancellationTokenMustBeLastAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CT1001";
        public const string MessageFormat = "Method '{0}' should take CancellationToken as the last parameter";
        internal const string Title = "CancellationToken parameters should come last";
        internal const string Category = "ApiDesign";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var cancellationTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
                if (cancellationTokenType != null)
                {
                    compilationContext.RegisterSymbolAction(symbolContext =>
                    {
                    var methodSymbol = (IMethodSymbol)symbolContext.Symbol;
                    for (int i = methodSymbol.Parameters.Length - 1; i >= 0; i--)
                    {
                        if (methodSymbol.Parameters[i].Type.Equals(cancellationTokenType)
                            && i != methodSymbol.Parameters.Length - 1)
                        {
                            symbolContext.ReportDiagnostic(Diagnostic.Create(
                                Rule, methodSymbol.Locations.First(), methodSymbol.ToDisplayString()));
                            }
                        }
                    },
                    SymbolKind.Method);
                }
            });
        }
    }
}
