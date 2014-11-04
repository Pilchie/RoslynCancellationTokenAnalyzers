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
using Microsoft.CodeAnalysis.Simplification;

namespace CancellationAnalyzer
{
    [ExportCodeFixProvider("CancellationAnalyzerCodeFixProvider", LanguageNames.CSharp), Shared]
    public class CancellationTokenShouldBeOptionalInPublicApisAndRequiredInInternalApisCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(CancellationTokenShouldBeOptionalInPublicApisAndRequiredInInternalApisAnalyzer.DiagnosticId);
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
            if (declaration.ParameterList.Parameters.Last().Default == null)
            {
                context.RegisterFix(CodeAction.Create("Make optional", async ct => await MakeOptional(context.Document, declaration, ct)), diagnostic);
            }
            else
            {
                context.RegisterFix(CodeAction.Create("Make required", async ct => await MakeRequired(context.Document, declaration, ct)), diagnostic);
            }
        }

        private async Task<Document> MakeOptional(Document document, MethodDeclarationSyntax declaration, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var lastParam = declaration.ParameterList.Parameters.Last();
            var newRoot = root.ReplaceNode(lastParam, lastParam.WithDefault(
                SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName("System.Threading.CancellationToken")))
                    .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation)));
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> MakeRequired(Document document, MethodDeclarationSyntax declaration, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var lastParam = declaration.ParameterList.Parameters.Last();
            var newRoot = root.ReplaceNode(lastParam, lastParam.WithDefault(null).WithAdditionalAnnotations(Formatter.Annotation));
            return document.WithSyntaxRoot(newRoot);
        }
    }
}