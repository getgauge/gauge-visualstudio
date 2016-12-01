// Copyright [2014, 2015] [ThoughtWorks Inc.](www.thoughtworks.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using EnvDTE;
using FakeItEasy;
using FakeItEasy.Configuration;
using Gauge.VisualStudio.Highlighting;
using Gauge.VisualStudio.Model;
using Microsoft.VisualStudio.Text;
using NUnit.Framework;

namespace Gauge.VisualStudio.Tests.Highlighting
{
    [TestFixture]
    public class StepImplementationGeneratorTests
    {
        private StepImplementationGenerator _stepImplementationGenerator;
        private IReturnValueArgumentValidationConfiguration<CodeFunction> _functionCall;
        private ITextSnapshotLine _textSnapshotLine;
        private const string SelectedClass = "foo";

        public void Setup(string stepText, string functionName, bool hasInlineTable, params string[] parameters)
        {
            var vsProject = A.Fake<EnvDTE.Project>();
            var project = A.Fake<IProject>();
            var step = A.Fake<IStep>();
            A.CallTo(() => step.Text).Returns(stepText);
            A.CallTo(() => step.Parameters).Returns(new List<string>(parameters));
            A.CallTo(() => step.HasInlineTable).Returns(hasInlineTable);

            _textSnapshotLine = A.Fake<ITextSnapshotLine>();
            A.CallTo(() => _textSnapshotLine.GetText()).Returns(stepText);

            var codeClass = A.Fake<CodeClass>();
            _functionCall = A.CallTo(() => codeClass.AddFunction(functionName,
                vsCMFunction.vsCMFunctionFunction, vsCMTypeRef.vsCMTypeRefVoid, -1,
                vsCMAccess.vsCMAccessPublic, A<object>._));
            var codeFunction = A.Fake<CodeFunction>();
            _functionCall.Returns(codeFunction);
            A.CallTo(() => project.FindOrCreateClass(vsProject, SelectedClass)).Returns(codeClass);

            _stepImplementationGenerator = new StepImplementationGenerator(vsProject, project, step);
        }

        [Test]
        public void ShouldGenerateImplementationWithSignature()
        {
            const string stepText = "Do nothing";
            var stepLiteral = string.Format("\"{0}\"", stepText);
            Setup(stepText, "DoNothing", false);
            CodeClass targetClass;
            CodeFunction impl;

            _stepImplementationGenerator.TryGenerateMethodStub(SelectedClass, _textSnapshotLine, out targetClass, out impl);

            _functionCall.MustHaveHappened();
            A.CallTo(() => impl.AddParameter(A<string>._, A<object>._, A<object>._)).MustHaveHappened(Repeated.Like(i => i == 0));
            A.CallTo(() => impl.AddAttribute("Step", stepLiteral, A<object>._)).MustHaveHappened();
        }

        [Test]
        public void ShouldGenerateImplementationWithParameters()
        {
            const string stepText = "Do <something>";
            var stepLiteral = string.Format("\"{0}\"", stepText);
            Setup(stepText, "DoSomething", false, "something");
            CodeClass targetClass;
            CodeFunction impl;

            _stepImplementationGenerator.TryGenerateMethodStub(SelectedClass, _textSnapshotLine, out targetClass, out impl);

            _functionCall.MustHaveHappened();
            A.CallTo(() => impl.AddParameter("something", vsCMTypeRef.vsCMTypeRefString, A<object>._)).MustHaveHappened(Repeated.Like(i => i == 1));
            A.CallTo(() => impl.AddAttribute("Step", stepLiteral, A<object>._)).MustHaveHappened();
        }

        [Test]
        public void ShouldGenerateImplementationWithFileParameter()
        {
            const string stepText = "Do something with <file:foo.txt>";
            var stepLiteral = string.Format("\"{0}\"", stepText);
            Setup(stepText, "DoSomethingWithFilefootxt", false,  "file:foo.txt");
            CodeClass targetClass;
            CodeFunction impl;

            _stepImplementationGenerator.TryGenerateMethodStub(SelectedClass, _textSnapshotLine, out targetClass, out impl);

            _functionCall.MustHaveHappened();
            A.CallTo(() => impl.AddParameter("foo", vsCMTypeRef.vsCMTypeRefString, A<object>._)).MustHaveHappened(Repeated.Like(i => i == 1));
            A.CallTo(() => impl.AddAttribute("Step", stepLiteral, A<object>._)).MustHaveHappened();
        }

        [Test]
        public void ShouldGenerateImplementationWithRefTableParameter()
        {
            const string stepText = "Do something with <table:foo.csv>";
            var stepLiteral = string.Format("\"{0}\"", stepText);
            Setup(stepText, "DoSomethingWithTablefoocsv", false, "table:foo.csv");
            CodeClass targetClass;
            CodeFunction impl;

            _stepImplementationGenerator.TryGenerateMethodStub(SelectedClass, _textSnapshotLine, out targetClass, out impl);

            _functionCall.MustHaveHappened();
            A.CallTo(() => impl.AddParameter("foo", "Table", A<object>._)).MustHaveHappened(Repeated.Like(i => i == 1));
            A.CallTo(() => impl.AddAttribute("Step", stepLiteral, A<object>._)).MustHaveHappened();
        }

        [Test]
        public void ShouldGenerateImplementationWithTheRightOrder()
        {
            const string stepText = "Do something with <something> and <another thing>";
            var stepLiteral = string.Format("\"{0}\"", stepText);
            Setup(stepText, "DoSomethingWithSomethingandAnotherThing", false, "something", "anotherthing");
            
            CodeClass targetClass;
            CodeFunction impl;
            _stepImplementationGenerator.TryGenerateMethodStub(SelectedClass, _textSnapshotLine, out targetClass, out impl);
            _functionCall.MustHaveHappened();

            A.CallTo(() => impl.AddParameter("anotherthing", vsCMTypeRef.vsCMTypeRefString, A<object>._)).MustHaveHappened()
                .Then(A.CallTo(() => impl.AddParameter("something", vsCMTypeRef.vsCMTypeRefString, A<object>._)).MustHaveHappened())
                .Then(A.CallTo(() => impl.AddAttribute("Step", stepLiteral, A<object>._)).MustHaveHappened());
        }

        [Test]
        public void ShouldGenerateImplementationForInlineTable()
        {
            const string stepText = "Do something with <table>";
            var stepLiteral = string.Format("\"{0}\"", stepText);
            Setup(stepText, "DoSomethingWithTable", true, "table");
            
            CodeClass targetClass;
            CodeFunction impl;
            _stepImplementationGenerator.TryGenerateMethodStub(SelectedClass, _textSnapshotLine, out targetClass, out impl);
                _functionCall.MustHaveHappened();

                A.CallTo(() => impl.AddParameter("table", "Table", A<object>._)).MustHaveHappened();
            A.CallTo(() => impl.AddAttribute("Step", stepLiteral, A<object>._)).MustHaveHappened();
        }
        
        [Test]
        public void ShouldGenerateImplementationForMultipleSpecialParameters()
        {
            const string stepText = "Do something with a <file:foo.txt>, a <table:bar.csv> and <table>";
            var stepLiteral = string.Format("\"{0}\"", stepText);
            Setup(stepText, "DoSomethingWithAFilefootxtaTablebarcsvandTable", true, "file:foo.txt", "table:bar.csv", "table");

            CodeClass targetClass;
            CodeFunction impl;
            _stepImplementationGenerator.TryGenerateMethodStub(SelectedClass, _textSnapshotLine, out targetClass, out impl);
            _functionCall.MustHaveHappened();

            A.CallTo(() => impl.AddParameter("table", "Table", A<object>._)).MustHaveHappened()
                .Then(A.CallTo(() => impl.AddParameter("bar", "Table", A<object>._)).MustHaveHappened())
                .Then(A.CallTo(() => impl.AddParameter("foo", vsCMTypeRef.vsCMTypeRefString, A<object>._)).MustHaveHappened())
                .Then(A.CallTo(() => impl.AddAttribute("Step", stepLiteral, A<object>._)).MustHaveHappened());
        }
    }
}
