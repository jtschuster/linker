// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TypeSystemProxy;
using ILLink.Shared.DataFlow;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Dataflow;

namespace ILLink.Shared.TrimAnalysis
{
	partial struct RequireDynamicallyAccessedMembersAction
	{
		readonly ReflectionMarker _reflectionMarker;
		readonly LinkContext _context;

		public RequireDynamicallyAccessedMembersAction (
			ReflectionMarker reflectionMarker,
			in DiagnosticContext diagnosticContext,
			in LinkContext context)
		{
			_reflectionMarker = reflectionMarker;
			_diagnosticContext = diagnosticContext;
			_context = context;
		}

		public partial bool TryResolveTypeNameAndMark (string typeName, bool needsAssemblyName, out TypeProxy type)
		{
			if (_reflectionMarker.TryResolveTypeNameAndMark (typeName, _diagnosticContext, needsAssemblyName, out TypeDefinition? foundType)) {
				type = new (foundType);
				return true;
			} else {
				type = default;
				return false;
			}
		}

		private partial void MarkTypeForDynamicallyAccessedMembers (in TypeProxy type, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			_reflectionMarker.MarkTypeForDynamicallyAccessedMembers (_diagnosticContext.Origin, type.Type, dynamicallyAccessedMemberTypes, DependencyKind.DynamicallyAccessedMember);
		}

		private partial (SingleValue Source, ValueWithDynamicallyAccessedMembers Target) Quirk (SingleValue source, ValueWithDynamicallyAccessedMembers target) {
			if (_context.WarnVersion < WarnVersion.ILLink7
				&& source is MethodParameterValue parameterValue
				&& parameterValue.ParameterDefinition.ParameterType.IsByRefOrPointer ())
				source = UnknownValue.Instance;
			return (source, target);
		}
	}
}
