﻿// ***********************************************************************
// Copyright (c) 2011-2020 Charlie Poole, Terje Sandstrom
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Engine;

namespace NUnit.VisualStudio.TestAdapter
{
    public class NUnitTestFilterBuilder
    {
        private readonly ITestFilterService _filterService;

        // ReSharper disable once StringLiteralTypo
        public static readonly TestFilter NoTestsFound = new TestFilter("<notestsfound/>");

        public NUnitTestFilterBuilder(ITestFilterService filterService)
        {
            _filterService = filterService ?? throw new NUnitEngineException("TestFilterService is not available. Engine in use is incorrect version.");
        }

        public TestFilter ConvertTfsFilterToNUnitFilter(ITfsTestFilter tfsFilter, IList<TestCase> loadedTestCases)
        {
            var filteredTestCases = tfsFilter.CheckFilter(loadedTestCases);
            var testCases = filteredTestCases as TestCase[] ?? filteredTestCases.ToArray();
            // TestLog.Info(string.Format("TFS Filter detected: LoadedTestCases {0}, Filtered Test Cases {1}", loadedTestCases.Count, testCases.Count()));
            return testCases.Any() ? FilterByList(testCases) : NoTestsFound;
        }

        public TestFilter FilterByWhere(string where)
        {
            if (string.IsNullOrEmpty(where))
                return TestFilter.Empty;
            var filterBuilder = _filterService.GetTestFilterBuilder();
            filterBuilder.SelectWhere(where);
            return filterBuilder.GetFilter();
        }

        public TestFilter FilterByList(IEnumerable<TestCase> testCases)
        {
            var filterBuilder = _filterService.GetTestFilterBuilder();

            foreach (var testCase in testCases)
            {
                filterBuilder.AddTest(testCase.FullyQualifiedName);
            }

            return filterBuilder.GetFilter();
        }
    }
}
