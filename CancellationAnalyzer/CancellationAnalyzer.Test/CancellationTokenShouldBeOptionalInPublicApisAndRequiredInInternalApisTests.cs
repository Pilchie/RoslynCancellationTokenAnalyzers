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
    public class CancellationTokenShouldBeOptionalInPublicApisAndRequiredInInternalApisTests : CodeFixVerifier
    {
        [TestMethod]
        public void NoDiagnosticForDefaultAccessibilityMethodWithNoOptional()
        {
            var source = @"
using System.Threading;
public class T
{
    void M(CancellationToken t)
    {
    }
}";
            VerifyCSharpDiagnostic(source);
        }

        [TestMethod]
        public void DiagnosticForDefaultAccessibilityMethodWithOptional()
        {
            var source = @"
using System.Threading;
public class T
{
    void M(CancellationToken t = default(CancellationToken))
    {
    }
}";
            var expected = new DiagnosticResult
            {
                Id = CancellationTokenShouldBeOptionalInPublicApisAndRequiredInInternalApisAnalyzer.DiagnosticId,
                Message = String.Format(CancellationTokenShouldBeOptionalInPublicApisAndRequiredInInternalApisAnalyzer.MessageFormat, "T.M(System.Threading.CancellationToken)", "not"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 5, 30)
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
            return new CancellationAnalyzer.CancellationTokenShouldBeOptionalInPublicApisAndRequiredInInternalApisAnalyzer();
        }
    }
}