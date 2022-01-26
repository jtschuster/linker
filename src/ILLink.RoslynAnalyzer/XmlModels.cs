// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared {
	public partial class LinkAttributes
	{
		public partial record TypeNode
		{
			public bool ResolveType(Compilation comp)
			{
				var classNames = comp.GetSymbolsWithName (ClassName)
					.Where (symbol => symbol is INamedTypeSymbol typeSymbol
						 && typeSymbol.HasName (FullName));
				if (classNames.Count() != 1)
					return false;
				else {
					Symbol = (INamedTypeSymbol) classNames.First ();
					semaphore.Release ();
					return true;
				}
			}
			public static readonly int semaphoreCount = 10;
			public SemaphoreSlim semaphore = new SemaphoreSlim (0);
			public INamedTypeSymbol? Symbol = null;
			public string ClassName => FullName.Split ('.').Last ();
		}
		public partial record AttributeNode
		{
			public static readonly int semaphoreCount = 10;
			public SemaphoreSlim semaphore = new SemaphoreSlim (semaphoreCount, semaphoreCount);
			public INamedTypeSymbol? AttributeClass = null;
			public string ClassName => FullName.Split ('.').Last ();
		}
	}
}
