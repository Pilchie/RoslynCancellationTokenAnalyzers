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

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            var token = root.FindToken(diagnosticSpan.Start);
            var invocation = token.Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
            var compilation = await context.Document.Project.GetCompilationAsync(context.CancellationToken);
            var cancellationTokenType = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

            var stuff = new Stuff(
                context.Document,
                root,
                invocation,
                await context.Document.GetSemanticModelAsync(context.CancellationToken),
                cancellationTokenType);

            var containingMethod = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (containingMethod != null)
            {
                var containingMethodSymbol = stuff.SemanticModel.GetDeclaredSymbol(containingMethod);
                foreach (var param in containingMethodSymbol.Parameters.Where(p => p.Type.Equals(stuff.CancellationTokenType)))
                {
                    context.RegisterFix(CodeAction.Create("Pass '\{param.Name}' explicitly", async ct => await PassCancellationToken(stuff, param.Name, ct)), diagnostic);
                }
            }

            context.RegisterFix(CodeAction.Create("Pass 'CancellationToken.None' explicitly", async ct => await PassCancellationToken(stuff, "CancellationToken.None", ct), id: "pass none"), diagnostic);
        }

        private Task<Document> PassCancellationToken(Stuff stuff, string cancellationTokenExpression, CancellationToken cancellationToken)
        {

            var methodSymbol = (IMethodSymbol)stuff.SemanticModel.GetSymbolInfo(stuff.Invocation).Symbol;
            var specifiedParameters = CancellationTokenMustBePassedExplicitlyAnalyzer.DetermineSpecifiedArguments(methodSymbol, stuff.Invocation);

            var parameterNamesToAdd = methodSymbol
                .Parameters
                .Where(p => p.Type.Equals(stuff.CancellationTokenType))
                .Select(p => p.Name)
                .Except(specifiedParameters);

            var invocation = stuff.Invocation;
            var hasNamedParams = stuff.Invocation.ArgumentList.Arguments.Any(arg => arg.NameColon != null);

            // TODO: This blows away trivia on the argument list separators.
            var newInvocation = stuff.Invocation.WithArgumentList(
                SyntaxFactory.ArgumentList(
                    stuff.Invocation.ArgumentList.OpenParenToken,
                    SyntaxFactory.SeparatedList(
                        stuff.Invocation.ArgumentList.Arguments.Concat(
                            parameterNamesToAdd.Select(pn =>
                                MakeArgument(pn, hasNamedParams, cancellationTokenExpression)))),
                    stuff.Invocation.ArgumentList.CloseParenToken)
                .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation));

            return Task.FromResult(stuff.Document.WithSyntaxRoot(stuff.Root.ReplaceNode(stuff.Invocation, newInvocation)));
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

        class Stuff
        {
            public Document Document { get; private set; }
            public SyntaxNode Root { get; private set; }
            public InvocationExpressionSyntax Invocation { get; private set; }

            public SemanticModel SemanticModel { get; private set; }

            public INamedTypeSymbol CancellationTokenType { get; private set; }

            public Stuff(Document document, SyntaxNode root, InvocationExpressionSyntax invocation, SemanticModel semanticModel, INamedTypeSymbol cancellationTokenType)
            {
                this.Document = document;
                this.Root = root;
                this.Invocation = invocation;
                this.SemanticModel = semanticModel;
                this.CancellationTokenType = cancellationTokenType;
            }
        }
    }
}