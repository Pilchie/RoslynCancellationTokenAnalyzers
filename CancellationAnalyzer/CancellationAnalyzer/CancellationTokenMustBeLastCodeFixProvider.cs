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
    public class CancellationTokenMustBeLastCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(CancellationTokenMustBeLastAnalyzer.DiagnosticId);
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

            // Find the method declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            // TODO: When we have a public Change Signature API, use that
            // instead of introducing a bunch of build breaks :(

            context.RegisterFix(CodeAction.Create("Move CancellationToken to the end", async ct =>
            {
                var semanticModel = await context.Document.GetSemanticModelAsync(ct);
                var methodSymbol = semanticModel.GetDeclaredSymbol(declaration);
                var compilation = await context.Document.Project.GetCompilationAsync(ct);
                var cancellationTokenType = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

                var cancellationTokenParameters = new List<ParameterSyntax>();
                var nonCancellationTokenParameters = new List<ParameterSyntax>();
                foreach (var param in declaration.ParameterList.Parameters)
                {
                    var paramSymbol = semanticModel.GetDeclaredSymbol(param);
                    if (paramSymbol.Type.Equals(cancellationTokenType))
                    {
                        cancellationTokenParameters.Add(param);
                    }
                    else
                    {
                        nonCancellationTokenParameters.Add(param);
                    }
                }

                // TODO: This blows away trivia on the separators :(
                var newDeclaration = declaration.WithParameterList(
                    SyntaxFactory.ParameterList(
                        declaration.ParameterList.OpenParenToken,
                        SyntaxFactory.SeparatedList(nonCancellationTokenParameters.Concat(cancellationTokenParameters)),
                        declaration.ParameterList.CloseParenToken))
                    .WithAdditionalAnnotations(Formatter.Annotation);

                var newRoot = root.ReplaceNode(declaration, newDeclaration);
                return context.Document.WithSyntaxRoot(newRoot);
            }),
            diagnostic);
        }
    }
}