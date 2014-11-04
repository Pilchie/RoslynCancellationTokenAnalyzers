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
    public class CancellationTokenMustBePassedExplicitlyCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(CancellationTokenMustBePassedExplicitlyAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override Task ComputeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            context.RegisterFix(CodeAction.Create("Pass 'CancellationToken.None' explicitly", async ct => await PassCancellationToken(context.Document, diagnosticSpan, "CancellationToken.None", ct)), diagnostic);
            return Task.FromResult(true);
        }

        private async Task<Document> PassCancellationToken(Document document, TextSpan span, string cancellationTokenExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var token = root.FindToken(span.Start);
            var invocation = token.Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var compilation = await document.Project.GetCompilationAsync(cancellationToken);
            var cancellationTokenType = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

            var methodSymbol = (IMethodSymbol)semanticModel.GetSymbolInfo(invocation).Symbol;
            var specifiedParameters = CancellationTokenMustBePassedExplicitlyAnalyzer.DetermineSpecifiedArguments(methodSymbol, invocation);

            var parameterNamesToAdd = methodSymbol
                .Parameters
                .Where(p => p.Type.Equals(cancellationTokenType))
                .Select(p => p.Name)
                .Except(specifiedParameters);

            var hasNamedParams = invocation.ArgumentList.Arguments.Any(arg => arg.NameColon != null);

            // TODO: This blows away trivia on the argument list separators.
            var newInvocation = invocation.WithArgumentList(
                SyntaxFactory.ArgumentList(
                    invocation.ArgumentList.OpenParenToken,
                    SyntaxFactory.SeparatedList(
                        invocation.ArgumentList.Arguments.Concat(
                            parameterNamesToAdd.Select(pn =>
                                MakeArgument(pn, hasNamedParams, cancellationTokenExpression)))),
                    invocation.ArgumentList.CloseParenToken)
                .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation));

            return document.WithSyntaxRoot(root.ReplaceNode(invocation, newInvocation));
        }

        private static ArgumentSyntax MakeArgument(string paramName, bool useNamedParameters, string cancellationTokenExpression)
        {
            return useNamedParameters
                ? SyntaxFactory.Argument(
                    SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(paramName)),
                    default(SyntaxToken),
                    SyntaxFactory.ParseExpression(cancellationTokenExpression))
                : SyntaxFactory.Argument(SyntaxFactory.ParseExpression(cancellationTokenExpression));
        }
    }
}