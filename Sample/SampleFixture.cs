using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using NUnit.Framework;
using System.Globalization;

namespace Sample
{
    /* 
     * This snippet of Project XML was hand-added to the .csproj for this project to make the build always unit test

        <UsingTask AssemblyFile="..\bin\$(Configuration)\MSBuild.NUnit.dll" TaskName="MSBuild.Tasks.NUnit" />
        <Target Name="RunUnitTest">
            <NUnit Assemblies="$(ProjectName).csproj" 
                   ToolPath="C:\Program Files (x86)\NUnit 2.5.5\bin\net-2.0\"
                   DisableShadowCopy="true" Force32Bit="false" ProjectConfiguration="$(Configuration)" FrameworkToUse="net-3.5" 
                   WorkingDirectory="$(ProjectDir)" ContinueOnError="false" />
        </Target>
        <Target Name="AfterBuild" DependsOnTargets="RunUnitTest">
        </Target>

     * TWO IMPORTANT NOTES:
     * 
     *  1) You can easily make the unit testing run for some specific configuration or other condition by adding a Condition attribute to the RunUnitTest target
     *  2) In the example above NUnit isn't side-installed with MSBuild.NUnit.dll so the ToolPath attribute was used with a hard-reference to a NUnit installation.
     *     For my usage I have a Dependencies\NUnit folder inside the solution folder for a big solution with many projects and many unit-test projects, that is sent up
     *     to the version control repository which contains a copy the bin\(framework)\ of the latest NUnit install and the NUnit task dll, so a relative path, or
     *     solution-rooted path to the MSBuild.NUnit.dll there is enough to make things work nicely, without the hardcoded ToolPath
     */

    [TestFixture]
    public class SampleFixture
    {
        [Test]
        public void SimpleTest()
        {
            Assert.Pass("Just OK, for testing the NUnit Task from the MSBuild NUnit Tas Project");
        }


        [Test]
        public void TestNumberParsing()
        {
            var formatter = CultureInfo.GetCultureInfo("en-US").NumberFormat;
            string longText = "4.9406564584124654E-308";
            double dvalue0 = Convert.ToDouble(longText, formatter);
            Assert.AreEqual(4.9406564584124654E-308d, dvalue0);
            string text = "4.9E-308";
            double dvalue1 = Convert.ToDouble(text, formatter);
            Assert.AreEqual(4.9E-308d, dvalue1);
         }


        private class MyClass
        {
            public int Id { get; set; }

        }

        private int count;

        private IEnumerable<MyClass> GetYieldResult(int qtResult)
        {
            for (int i = 0; i < qtResult; i++)
            {
                count++;
                yield return new MyClass() { Id = i + 1 };
            }
        }

        private IEnumerable<MyClass> GetNonYieldResult(int qtResult)
        {
            var result = new List<MyClass>();

            for (int i = 0; i < qtResult; i++)
            {
                count++;
                result.Add(new MyClass() { Id = i + 1 });
            }

            return result;
        }

        [Test]
        public void Test1()
        {
            count = 0;

            IEnumerable<MyClass> yieldResult = GetYieldResult(1);

            var firstGet = yieldResult.First();
            var secondGet = yieldResult.First();

            Assert.AreEqual(1, firstGet.Id);
            Assert.AreEqual(1, secondGet.Id);

            Assert.AreEqual(2, count);//calling "First()" 2 times, yieldResult is created 2 times
            Assert.AreNotSame(firstGet, secondGet);//and created different instances of each list item
        }

        [Test]
        public void Test2()
        {
            count = 0;

            IEnumerable<MyClass> yieldResult = GetNonYieldResult(1);

            var firstGet = yieldResult.First();
            var secondGet = yieldResult.First();

            Assert.AreEqual(1, firstGet.Id);
            Assert.AreEqual(1, secondGet.Id);

            Assert.AreEqual(1, count);//as expected, it creates only 1 result set
            Assert.AreSame(firstGet, secondGet);//and calling "First()" several times will always return same instance of MyClass
        }

        [Test]
        public void Test3()
        {
            count = 0;

            ICachedEnumerable<MyClass> yieldResult = GetYieldResult(1).AsCachedEnumerable();

            var firstGet = yieldResult.First();
            var secondGet = yieldResult.First();

            Assert.AreEqual(1, firstGet.Id);
            Assert.AreEqual(1, secondGet.Id);

            Assert.AreEqual(1, count);//calling "First()" 2 times, yieldResult is created 2 times
            Assert.AreSame(firstGet, secondGet);//and created different instances of each list item
        }

    }
}
