using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;

namespace CancellationAnalyzer
{
    [ExportCodeFixProvider("CancellationAnalyzerCodeFixProvider", LanguageNames.CSharp), Shared]
    public class CancellationTokenMustBeNamedCancellationTokenCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(CancellationTokenMustBeNamedCancellationTokenAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ParameterSyntax>().First();
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
            var parameterSymbol = semanticModel.GetDeclaredSymbol(declaration);

            context.RegisterFix(CodeAction.Create("Move CancellationToken to the end", ct =>
            {
                return Renamer.RenameSymbolAsync(context.Document.Project.Solution, parameterSymbol, "cancellationToken", context.Document.Project.Solution.Workspace.Options, ct);
            }),
            diagnostic);
        }
    }
}