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
                }
            });
        }
    }
}
