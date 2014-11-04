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
    public class CancellationTokenMustBePassedExplicitlyTests : CodeFixVerifier
    {
        [TestMethod]
        public void NoDiagnosticWhenCancellationTokenNonePassedExplicitly()
        {
            var source = @"
using System.Threading;
public class T
{
    public void M(CancellationToken t = default(CancellationToken))
    {
        M(CancellationToken.None);
    }
}";
            VerifyCSharpDiagnostic(source);
        }

        [TestMethod]
        public void DiagnosticWhenCancellationTokenOmittedExplicitly()
        {
            var source = @"
using System.Threading;
public class T
{
    public void M(CancellationToken t = default(CancellationToken))
    {
        M();
    }
}";

            var expected = new DiagnosticResult
            {
                Id = CancellationTokenMustBePassedExplicitlyAnalyzer.DiagnosticId,
                Message = CancellationTokenMustBePassedExplicitlyAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 7, 9)
                        }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return null;
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CancellationTokenMustBePassedExplicitlyAnalyzer();
        }
    }
}