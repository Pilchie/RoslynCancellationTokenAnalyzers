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
    public class CancellationTokenMustBeOptionalInPublicApisAndRequiredInInternalApisAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CT1002";
        public const string MessageFormat = "Method '{0}' should {1} have optional CancellationToken parameter";
        internal const string Title = "Cancellation Token Should Be Optional In Public APIs and Required In Internal APIs";
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
                        var last = methodSymbol.Parameters.Length - 1;
                        if (methodSymbol.Parameters[last].IsParams)
                        {
                            last--;
                        }

                        var parameterSymbol = methodSymbol.Parameters[last];
                        if (parameterSymbol.Type.Equals(cancellationTokenType))
                        {
                            if (!parameterSymbol.IsOptional &&
                                methodSymbol.DeclaredAccessibility == Accessibility.Public &&
                                AllContainingTypesArePublic(methodSymbol))
                            {
                                symbolContext.ReportDiagnostic(Diagnostic.Create(
                                    Rule, parameterSymbol.Locations.First(), methodSymbol.ToDisplayString(), string.Empty));
                            }
                            else if (parameterSymbol.IsOptional &&
                                (methodSymbol.DeclaredAccessibility != Accessibility.Public ||
                                 !AllContainingTypesArePublic(methodSymbol)))
                            {
                                symbolContext.ReportDiagnostic(Diagnostic.Create(
                                    Rule, parameterSymbol.Locations.First(), methodSymbol.ToDisplayString(), "not"));
                            }
                        }
                    },
                    SymbolKind.Method);
                }
            });
        }

        private bool AllContainingTypesArePublic(IMethodSymbol methodSymbol)
        {
            var containingType = methodSymbol.ContainingType;
            while (containingType != null)
            {
                if (containingType.DeclaredAccessibility != Accessibility.Public)
                {
                    return false;
                }

                containingType = containingType.ContainingType;
            }

            return true;
        }
    }
}
