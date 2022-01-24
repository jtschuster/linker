
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed class LinkAttributesTests : LinkerTestBase
	{
		protected override string TestSuiteName => "LinkAttributes";

		//[Fact]
		public Task EmbeddedLinkAttributes ()
		{
			return RunTest (nameof (EmbeddedLinkAttributes));
		}
		//[Fact]
		public Task LinkerAttributeRemoval ()
		{
			return RunTest (nameof (LinkerAttributeRemoval));
		}
		//[Fact]
		public Task TypedArguments ()
		{
			return RunTest (nameof (TypedArguments));
		}
		//[Fact]
		public Task LinkerAttributeRemovalConditional ()
		{
			return RunTest (nameof (LinkerAttributeRemovalConditional));
		}
	}
}