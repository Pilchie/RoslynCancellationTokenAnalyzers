using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CancellationAnalyzer
{
    [DiagnosticAnalyzer]
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
                        if (methodSymbol.IsOverride
                            || methodSymbol.ExplicitInterfaceImplementations.Any()
                            || ImplementsAnInterfaceMethodImplicitly(methodSymbol))
                        {
                            return;
                        }

                        var last = methodSymbol.Parameters.Length - 1;
                        if (last >= 0 && methodSymbol.Parameters[last].IsParams)
                        {
                            last--;
                        }

                        // Skip optional parameters, UNLESS one of them is a CancellationToken
                        // AND it's not the last one.
                        if (last >= 0 && methodSymbol.Parameters[last].IsOptional
                            && !methodSymbol.Parameters[last].Type.Equals(cancellationTokenType))
                        {
                            last--;

                            while (last >= 0 && methodSymbol.Parameters[last].IsOptional)
                            {
                                if (methodSymbol.Parameters[last].Type.Equals(cancellationTokenType))
                                {
                                    symbolContext.ReportDiagnostic(Diagnostic.Create(
                                        Rule, methodSymbol.Locations.First(), methodSymbol.ToDisplayString()));
                                }

                                last--;
                            }
                        }

                        while (last >= 0 && methodSymbol.Parameters[last].RefKind != RefKind.None)
                        {
                            last--;
                        }

                        for (int i = last; i >= 0; i--)
                        {
                            var parameterType = methodSymbol.Parameters[i].Type;
                            if (parameterType.Equals(cancellationTokenType)
                                && i != last)
                            {
                                symbolContext.ReportDiagnostic(Diagnostic.Create(
                                    Rule, methodSymbol.Locations.First(), methodSymbol.ToDisplayString()));
                                break;
                            }
                        }
                    },
                    SymbolKind.Method);
                }
            });
        }

        private bool ImplementsAnInterfaceMethodImplicitly(IMethodSymbol methodSymbol)
        {
            // This is an approximation, because another class could derive from this one
            // and rely on methodSymbol implementing one of *it's* interfaces methods, but
            // it's good enough.

            foreach (var interfaceSymbol in methodSymbol.ContainingType.AllInterfaces)
            {
                foreach (var interfaceMethod in interfaceSymbol.GetMembers().Where(m => m.Kind == SymbolKind.Method))
                {
                    if (methodSymbol.ContainingType.FindImplementationForInterfaceMember(interfaceMethod)?.Equals(methodSymbol) ?? false)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
