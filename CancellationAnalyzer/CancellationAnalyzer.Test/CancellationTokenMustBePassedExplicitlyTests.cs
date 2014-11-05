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
            VerifyCSharpFix(source, @"
using System.Threading;
public class T
{
    public void M(CancellationToken t = default(CancellationToken))
    {
        M(t);
    }
}", codeFixIndex: 0);

            VerifyCSharpFix(source, @"
using System.Threading;
public class T
{
    public void M(CancellationToken t = default(CancellationToken))
    {
        M(CancellationToken.None);
    }
}", codeFixIndex: 1);
        }

        [TestMethod]
        public void FixAddsNamedParameterAfterNamedParameter()
        {
            var source = @"
using System.Threading;
public class T
{
    public void M()
    {
        M2(i: 42);
    }

    public void M2(int i, CancellationToken cancellationToken = default(CancellationToken)) { }
}";

            VerifyCSharpFix(source, @"
using System.Threading;
public class T
{
    public void M()
    {
        M2(i: 42, cancellationToken: CancellationToken.None);
    }

    public void M2(int i, CancellationToken cancellationToken = default(CancellationToken)) { }
}");
        }

        [TestMethod]
        public void FixAddsNamedParameterWhenOtherParametersAreOmitted()
        {
            var source = @"
using System.Threading;
public class T
{
    public void M()
    {
        M2();
    }

    public void M2(int i = 42, CancellationToken cancellationToken = default(CancellationToken)) { }
}";

            VerifyCSharpFix(source, @"
using System.Threading;
public class T
{
    public void M()
    {
        M2(cancellationToken: CancellationToken.None);
    }

    public void M2(int i = 42, CancellationToken cancellationToken = default(CancellationToken)) { }
}");
        }
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CancellationTokenMustBePassedExplicitlyCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CancellationTokenMustBePassedExplicitlyAnalyzer();
        }
    }
}