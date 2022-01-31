// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed class LinkAttributesTests : LinkerTestBase
	{
		protected override string TestSuiteName => "LinkAttributes";

		[Fact (Skip = "XML Analyzer not implemented")]
		public Task EmbeddedLinkAttributes ()
		{
			return RunTest (nameof (EmbeddedLinkAttributes));
		}

		[Fact ]
		public Task LinkerAttributeRemoval ()
		{
			return RunTest (nameof (LinkerAttributeRemoval));
		}

		[Fact (Skip = "XML Analyzer not implemented")]
		public Task TypedArguments ()
		{
			return RunTest (nameof (TypedArguments));
		}

		[Fact (Skip = "XML Analyzer not implemented")]
		public Task LinkerAttributeRemovalConditional ()
		{
			return RunTest (nameof (LinkerAttributeRemovalConditional));
		}
		[Fact]
		public Task LinkAttributeErrorCases ()
		{
			return RunTest (nameof (LinkAttributeErrorCases));
		}
	}
}
