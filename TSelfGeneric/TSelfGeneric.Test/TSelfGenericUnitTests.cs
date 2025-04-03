using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = TSelfGeneric.Test.CSharpCodeFixVerifier<
    TSelfGeneric.TSelfGenericAnalyzer,
    TSelfGeneric.TSelfGenericCodeFixProvider>;

namespace TSelfGeneric.Test
{
    [TestClass]
    public class TSelfGenericUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = """
                namespace Self
                {
                    interface ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }    
                    
                    class SelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }

                    class Implementation : ISelfRequested<Implementation> { }
                
                    class ClassImplementation : SelfRequested<ClassImplementation> { }
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2()
        {
            var test =  """
                namespace Self
                {
                    interface ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }

                    class Implementation : ISelfRequested<Implementation> { }
                
                    class BadImplementation : ISelfRequested<{|#0:Implementation|}> { }
                }
                """;

            var fixTest = """
                namespace Self
                {
                    interface ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }

                    class Implementation : ISelfRequested<Implementation> { }
                
                    class BadImplementation : ISelfRequested<BadImplementation> { }
                }
                """;

            var expected = VerifyCS.Diagnostic("TSelfGeneric")
                .WithSpan(7, 46, 7, 60)
                .WithArguments("BadImplementation");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest);
        }

        [TestMethod]
        public async Task TestMethod3()
        {
            var test =  """
                namespace Self
                {
                    interface ISelfRequested<TOther, TSelf> where TSelf : ISelfRequested<TOther, TSelf> { }

                    class Implementation : ISelfRequested<int, Implementation> { }
                
                    class BadImplementation : ISelfRequested<int, {|#0:Implementation|}> { }
                }
                """;

            var fixTest = """
                namespace Self
                {
                    interface ISelfRequested<TOther, TSelf> where TSelf : ISelfRequested<TOther, TSelf> { }

                    class Implementation : ISelfRequested<int, Implementation> { }
                
                    class BadImplementation : ISelfRequested<int, BadImplementation> { }
                }
                """;

            var expected = VerifyCS.Diagnostic("TSelfGeneric")
                .WithSpan(7, 51, 7, 65)
                .WithArguments("BadImplementation");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest);
        }

        [TestMethod]
        public async Task TestMethod4()
        {
            var test =  """
                namespace Self
                {
                    interface ISelfRequested<TSelf, TOther> where TSelf : ISelfRequested<TSelf, TOther> { }

                    class Implementation : ISelfRequested<Implementation, int> { }
                
                    class Deep {
                        class BadImplementation : ISelfRequested<{|#0:Implementation|}, int> { }
                    }
                }
                """;

            var fixTest = """
                namespace Self
                {
                    interface ISelfRequested<TSelf, TOther> where TSelf : ISelfRequested<TSelf, TOther> { }

                    class Implementation : ISelfRequested<Implementation, int> { }
                
                    class Deep {
                        class BadImplementation : ISelfRequested<BadImplementation, int> { }
                    }
                }
                """;

            var expected = VerifyCS.Diagnostic("TSelfGeneric")
                .WithSpan(8, 50, 8, 64)
                .WithArguments("BadImplementation");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest);
        }

        [TestMethod]
        public async Task TestMethod6()
        {
            var test =  """
                namespace Self
                {
                    class SelfRequested<TSelf> where TSelf : SelfRequested<TSelf> { }

                    class Implementation : SelfRequested<Implementation> { }
                
                    class BadImplementation : SelfRequested<{|#0:Implementation|}> { }
                }
                """;

            var fixTest = """
                namespace Self
                {
                    class SelfRequested<TSelf> where TSelf : SelfRequested<TSelf> { }

                    class Implementation : SelfRequested<Implementation> { }
                
                    class BadImplementation : SelfRequested<BadImplementation> { }
                }
                """;

            var expected = VerifyCS.Diagnostic("TSelfGeneric")
                .WithSpan(7, 45, 7, 59)
                .WithArguments("BadImplementation");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest);
        }

        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod7()
        {
            var test = """
                namespace Self
                {
                    interface ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }

                    abstract class Implementation<TSelf> : ISelfRequested<TSelf> where TSelf : Implementation<TSelf> { }
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}