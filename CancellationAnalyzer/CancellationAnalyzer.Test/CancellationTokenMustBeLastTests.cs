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
    public class CancellationTokenMustBeLastTests : CodeFixVerifier
    {
        [TestMethod]
        public void NoDiagnosticInEmptyFile()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void DiagnosticForMethod()
        {
            var source = @"
using System.Threading;
class T
{
    void M(CancellationToken t, int i)
    {
    }
}";
            var expected = new DiagnosticResult
            {
                Id = CancellationTokenMustBeLastAnalyzer.DiagnosticId,
                Message = String.Format(CancellationTokenMustBeLastAnalyzer.MessageFormat, "T.M(System.Threading.CancellationToken, int)"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 5, 10)
                        }
            };

            VerifyCSharpDiagnostic(source, expected);

            var fixedSource = @"
using System.Threading;
class T
{
    void M(int i, CancellationToken t)
    {
    }
}";
            VerifyCSharpFix(source, fixedSource);
        }

        [TestMethod]
        public void NoDiagnosticWhenLastParam()
        {
            var test = @"
using System.Threading;
class T
{
    void M(int i, CancellationToken t)
    {
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void NoDiagnosticWhenOnlyParam()
        {
            var test = @"
using System.Threading;
class T
{
    void M(CancellationToken t)
    {
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void NoDiagnosticWhenParamsComesAfter()
        {
            var test = @"
using System.Threading;
class T
{
    void M(CancellationToken t, params object[] args)
    {
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void NoDiagnosticWhenOutComesAfter()
        {
            var test = @"
using System.Threading;
class T
{
    void M(CancellationToken t, out int i)
    {
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void NoDiagnosticWhenRefComesAfter()
        {
            var test = @"
using System.Threading;
class T
{
    void M(CancellationToken t, ref int x, ref int y)
    {
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void NoDiagnosticWhenOptionalParameterComesAfterNonOptionalCancellationToken()
        {
            var test = @"
using System.Threading;
class T
{
    void M(CancellationToken t, int x = 0)
    {
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void NoDiagnosticOnOverride()
        {
            var test = @"
using System.Threading;
class B
{
    protected virtual void M(CancellationToken t, int i) { }
}

class T : B
{
    protected override void M(CancellationToken t, int i) { }
}";

            // One diagnostic for the virtual, but none for the override.
            var expected = new DiagnosticResult
            {
                Id = CancellationTokenMustBeLastAnalyzer.DiagnosticId,
                Message = String.Format(CancellationTokenMustBeLastAnalyzer.MessageFormat, "B.M(System.Threading.CancellationToken, int)"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 5, 28)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void NoDiagnosticOnImplicitInterfaceImplentation()
        {
            var test = @"
using System.Threading;
interface I
{
    void M(CancellationToken t, int i);
}

class T : I
{
    public void M(CancellationToken t, int i) { }
}";

            // One diagnostic for the interface, but none for the implementation.
            var expected = new DiagnosticResult
            {
                Id = CancellationTokenMustBeLastAnalyzer.DiagnosticId,
                Message = String.Format(CancellationTokenMustBeLastAnalyzer.MessageFormat, "I.M(System.Threading.CancellationToken, int)"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 5, 10)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void NoDiagnosticOnExplicitInterfaceImplementation()
        {
            var test = @"
using System.Threading;
interface I
{
    void M(CancellationToken t, int i);
}

class T : I
{
    void I.M(CancellationToken t, int i) { }
}";

            // One diagnostic for the interface, but none for the implementation.
            var expected = new DiagnosticResult
            {
                Id = CancellationTokenMustBeLastAnalyzer.DiagnosticId,
                Message = String.Format(CancellationTokenMustBeLastAnalyzer.MessageFormat, "I.M(System.Threading.CancellationToken, int)"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 5, 10)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CancellationTokenMustBeLastCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CancellationTokenMustBeLastAnalyzer();
        }
    }
}