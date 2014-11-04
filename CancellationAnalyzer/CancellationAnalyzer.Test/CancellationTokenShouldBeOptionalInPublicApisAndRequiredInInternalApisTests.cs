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
        public void NoDiagnosticForPublicMethodWithOptional()
        {
            var source = @"
using System.Threading;
public class T
{
    public void M(CancellationToken t = default(CancellationToken))
    {
    }
}";
            VerifyCSharpDiagnostic(source);
        }

        [TestMethod]
        public void DiagnosticForPublicMethodWithNoOptional()
        {
            var source = @"
using System.Threading;
public class T
{
    public void M(CancellationToken t)
    {
    }
}";

            var expected = new DiagnosticResult
            {
                Id = CancellationTokenShouldBeOptionalInPublicApisAndRequiredInInternalApisAnalyzer.DiagnosticId,
                Message = String.Format(CancellationTokenShouldBeOptionalInPublicApisAndRequiredInInternalApisAnalyzer.MessageFormat, "T.M(System.Threading.CancellationToken)", string.Empty),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 5, 37)
                        }
            };

            VerifyCSharpDiagnostic(source, expected);

            var fixedSource = @"
using System.Threading;
public class T
{
    public void M(CancellationToken t = default(CancellationToken))
    {
    }
}";
            VerifyCSharpFix(source, fixedSource);
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

            var fixedSource = @"
using System.Threading;
public class T
{
    void M(CancellationToken t)
    {
    }
}";
            VerifyCSharpFix(source, fixedSource);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CancellationTokenShouldBeOptionalInPublicApisAndRequiredInInternalApisCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CancellationTokenShouldBeOptionalInPublicApisAndRequiredInInternalApisAnalyzer();
        }
    }
}