using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CancellationAnalyzer
{
    [DiagnosticAnalyzer]
    public class CancellationTokenMustBeNamedCancellationTokenAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CT1004";
        public const string MessageFormat = "Parameter '{0}' should be named 'cancellationToken'";
        internal const string Title = "CancellationToken parameters should be named 'cancellationToken'";
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
                        foreach (var parameter in methodSymbol.Parameters)
                        {
                            if (parameter.Type.Equals(cancellationTokenType)
                                && parameter.Name != "cancellationToken")
                            {
                                symbolContext.ReportDiagnostic(Diagnostic.Create(
                                    Rule, parameter.Locations.First(), parameter.Name));
                            }
                        }
                    },
                    SymbolKind.Method);
                }
            });
        }
    }
}
