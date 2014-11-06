using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using CancellationAnalyzer;

namespace CancellationAnalyzer.Test
{
    [TestClass]
    public class CancellationTokenMustBeNamedCancellationToken : CodeFixVerifier
    {
        [TestMethod]
        public void NoDiagnosticInWhenNamedCorrectly()
        {
            var test = @"
using System.Threading;
class C
{
    void M(CancellationToken cancellationToken) { }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void DiagnosticWhenNamedIncorrectly()
        {
            var source = @"
using System.Threading;
class C
{
    void M(CancellationToken ct) { }
}";

            var expected = new DiagnosticResult
            {
                Id = CancellationTokenMustBeNamedCancellationTokenAnalyzer.DiagnosticId,
                Message = String.Format(CancellationTokenMustBeNamedCancellationTokenAnalyzer.MessageFormat, "ct"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 5, 30)
                        }
            };

            VerifyCSharpDiagnostic(source, expected);

            var fixedSource = @"
using System.Threading;
class C
{
    void M(CancellationToken cancellationToken) { }
}";

            //VerifyCSharpFix(source, fixedSource);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return null/*new CancellationTokenMustBeNamedCancellationTokenCodeFixProvider()*/;
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CancellationTokenMustBeNamedCancellationTokenAnalyzer();
        }
    }
}