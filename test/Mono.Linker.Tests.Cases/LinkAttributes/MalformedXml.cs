// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkAttributes
{
	[SetupLinkAttributesFile ("MalformedXml.xml")]
	[SetupLinkerArgument ("--skip-unresolved", "true")]

	[ExpectedWarning ("IL2008", "Could not resolve type 'NonExistentType'.")]
	class MalformedXml
	{
		public static void Main ()
		{
		}

		public class NestedType 
		{
		}
	}
}
