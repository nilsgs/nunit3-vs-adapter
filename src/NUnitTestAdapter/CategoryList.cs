﻿// ***********************************************************************
// Copyright (c) 2014-2020 Charlie Poole, Terje Sandstrom
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NUnit.VisualStudio.TestAdapter
{
    public class CategoryList
    {
        public const string NUnitCategoryName = "NUnit.TestCategory";
        private const string NunitTestCategoryLabel = "Category";
        private const string VsTestCategoryLabel = "TestCategory";
        private const string MSTestCategoryName = "MSTestDiscoverer.TestCategory";

        internal static readonly TestProperty NUnitTestCategoryProperty = TestProperty.Register(
            NUnitCategoryName,
            VsTestCategoryLabel, typeof(string[]), TestPropertyAttributes.Hidden | TestPropertyAttributes.Trait,
            typeof(TestCase));

        private TestProperty
            msTestCategoryProperty; // = TestProperty.Register(MSTestCategoryName, VsTestCategoryLabel, typeof(string[]), TestPropertyAttributes.Hidden | TestPropertyAttributes.Trait, typeof(TestCase));

        internal static readonly TestProperty NUnitExplicitProperty = TestProperty.Register(
            "NUnit.Explicit",
            "Explicit", typeof(bool), TestPropertyAttributes.Hidden, typeof(TestCase));

        private const string ExplicitTraitName = "Explicit";

        // The empty string causes the UI we want.
        // If it's null, the explicit trait doesn't show up in Test Explorer.
        // If it's not empty, it shows up as “Explicit [value]” in Test Explorer.
        private const string ExplicitTraitValue = "";

        private readonly List<string> categorylist = new List<string>();
        private readonly TestCase testCase;
        private readonly IAdapterSettings settings;

        public CategoryList(TestCase testCase, IAdapterSettings adapterSettings)
        {
            settings = adapterSettings;
            this.testCase = testCase;
            // MsTestCategoryProperty = TestProperty.Register(MSTestCategoryName, VsTestCategoryLabel, typeof(string[]), TestPropertyAttributes.Hidden | TestPropertyAttributes.Trait, typeof(TestCase));
        }

        public void AddRange(IEnumerable<string> categories)
        {
            categorylist.AddRange(categories);
        }

        public int LastNodeListCount { get; private set; }

        public IEnumerable<string> ProcessTestCaseProperties(XmlNode testNode, bool addToCache, string key = null,
            IDictionary<string, TraitsFeature.CachedTestCaseInfo> traitsCache = null)
        {
            var nodelist = testNode.SelectNodes("properties/property");
            LastNodeListCount = nodelist.Count;
            foreach (XmlNode propertyNode in nodelist)
            {
                string propertyName = propertyNode.GetAttribute("name");
                string propertyValue = propertyNode.GetAttribute("value");
                if (addToCache)
                    AddTraitsToCache(traitsCache, key, propertyName, propertyValue);
                if (IsInternalProperty(propertyName, propertyValue))
                    continue;
                if (propertyName != NunitTestCategoryLabel)
                {
                    testCase.Traits.Add(new Trait(propertyName, propertyValue));
                }
                else
                {
                    categorylist.Add(propertyValue);
                }
            }

            if (testNode.Attributes?["runstate"]?.Value != "Explicit")
                return categorylist;
            // Add UI grouping “Explicit”
            if (testCase.Traits.All(trait => trait.Name != ExplicitTraitName))
                testCase.Traits.Add(new Trait(ExplicitTraitName, ExplicitTraitValue));

            // Track whether the test is actually explicit since multiple things result in the same UI grouping
            testCase.SetPropertyValue(NUnitExplicitProperty, true);

            if (addToCache)
            {
                // Add UI grouping “Explicit”
                AddTraitsToCache(traitsCache, key, ExplicitTraitName, ExplicitTraitValue);

                // Track whether the test is actually explicit since multiple things result in the same UI grouping
                GetCachedInfo(traitsCache, key).Explicit = true;
            }

            return categorylist;
        }

        /// <summary>
        /// See https://github.com/nunit/nunit/blob/master/src/NUnitFramework/framework/Internal/PropertyNames.cs.
        /// </summary>
        private readonly List<string> _internalProperties = new List<string>
        { "Author", "ApartmentState", "Description", "IgnoreUntilDate", "LevelOfParallelism", "MaxTime", "Order", "ParallelScope", "Repeat", "RequiresThread", "SetCulture", "SetUICulture", "TestOf", "Timeout" };


        private bool IsInternalProperty(string propertyName, string propertyValue)
        {
            if (propertyName == ExplicitTraitName)
            {
                // Otherwise the IsNullOrEmpty check does the wrong thing,
                // but I'm not sure of the consequences of allowing all empty strings.
                return false;
            }

            // Property names starting with '_' are for internal use only, but over time this has changed, so we now use a list
            if (!settings.ShowInternalProperties &&
                _internalProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase))
                return true;
            return string.IsNullOrEmpty(propertyName) || propertyName[0] == '_' || string.IsNullOrEmpty(propertyValue);
        }

        private void AddTraitsToCache(IDictionary<string, TraitsFeature.CachedTestCaseInfo> traitsCache, string key, string propertyName, string propertyValue)
        {
            if (IsInternalProperty(propertyName, propertyValue)) return;

            var info = GetCachedInfo(traitsCache, key);
            info.Traits.Add(new Trait(propertyName, propertyValue));
        }

        private static TraitsFeature.CachedTestCaseInfo GetCachedInfo(IDictionary<string, TraitsFeature.CachedTestCaseInfo> traitsCache, string key)
        {
            if (!traitsCache.TryGetValue(key, out var info))
                traitsCache.Add(key, info = new TraitsFeature.CachedTestCaseInfo());
            return info;
        }

        public void UpdateCategoriesToVs()
        {
            if (categorylist.Any())
            {
                testCase.SetPropertyValue(
                    settings.VsTestCategoryType == VsTestCategoryType.NUnit
                        ? NUnitTestCategoryProperty
                        : msTestCategoryProperty, categorylist.Distinct().ToArray());
            }
        }
    }
}