﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	static class INamedTypeSymbolExtensions
	{
		/// <summary>
		/// Returns true if <see paramref="type" /> has the same name as <see paramref="typename" />
		/// </summary>
		internal static bool HasName (this INamedTypeSymbol type, string typeName)
		{
			var roSpan = typeName.AsSpan ();
			INamespaceOrTypeSymbol? currentType = type;
			while (roSpan.Length > 0) {
				var dot = roSpan.LastIndexOf ('.');
				var currentName = dot < 0 ? roSpan : roSpan.Slice (dot + 1);
				if (currentType is null ||
					!currentName.Equals (currentType.Name.AsSpan (), StringComparison.Ordinal)) {
					return false;
				}
				currentType = (INamespaceOrTypeSymbol?) currentType.ContainingType ?? currentType.ContainingNamespace;
				roSpan = roSpan.Slice (0, dot > 0 ? dot : 0);
			}

			return true;
		}

		internal static IEnumerable<(ISymbol InterfaceMember, ISymbol ImplementationMember)> GetMemberInterfaceImplementationPairs (this INamedTypeSymbol namedType)
		{
			var interfaces = namedType.Interfaces;
			foreach (INamedTypeSymbol iface in interfaces) {
				foreach (var pair in GetMatchingMembers (namedType, iface)) {
					yield return pair;
				}
			}
		}

		private static IEnumerable<(ISymbol InterfaceMember, ISymbol ImplementationMember)> GetMatchingMembers (INamedTypeSymbol implementationSymbol, INamedTypeSymbol interfaceSymbol)
		{
			var members = interfaceSymbol.GetMembers ();
			foreach (var member in members) {
				if (member is ISymbol interfaceMember
					&& implementationSymbol.FindImplementationForInterfaceMember (member) is ISymbol implementationMember) {
					yield return (InterfaceMember: interfaceMember, ImplementationMember: implementationMember);
				}
			}
			foreach (var iface in interfaceSymbol.Interfaces) {
				foreach (var pair in GetMatchingMembers (implementationSymbol, iface)) {
					yield return pair;
				}
			}
		}
	}
}
