// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	public class XmlAttributeData : AttributeData
	{
		public XmlAttributeData (INamedTypeSymbol attributeType)
		{
			attributeClass = attributeType;
		}

		readonly INamedTypeSymbol? attributeClass;
		
		protected override INamedTypeSymbol? CommonAttributeClass { get => attributeClass; }

		protected override IMethodSymbol? CommonAttributeConstructor => throw new NotImplementedException ();

		protected override SyntaxReference? CommonApplicationSyntaxReference => throw new NotImplementedException ();

		protected override ImmutableArray<TypedConstant> CommonConstructorArguments { get => ImmutableArray.Create<TypedConstant> (); }

		protected override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments { get => ImmutableArray.Create<KeyValuePair<string, TypedConstant>> (); }
	}
}
