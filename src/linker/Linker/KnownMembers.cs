﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Mono.Linker
{
	public class KnownMembers
	{
		public MethodDefinition? NotSupportedExceptionCtorString { get; set; }
		public MethodDefinition? DisablePrivateReflectionAttributeCtor { get; set; }
		public MethodDefinition? ObjectCtor { get; set; }

		public TypeDefinition? RemoveAttributeInstancesAttributeDefinition { get; set; }

		public static bool IsNotSupportedExceptionCtorString (MethodDefinition method)
		{
			if (!method.IsConstructor || method.IsStatic || !method.HasParameters)
				return false;

			if (method.Parameters.Count != 1 || method.Parameters[0].ParameterType.MetadataType != MetadataType.String)
				return false;

			return true;
		}

		public static bool IsSatelliteAssemblyMarker (MethodDefinition method)
		{
			if (!method.IsConstructor || method.IsStatic)
				return false;

			var declaringType = method.DeclaringType;
			return declaringType.Name == "ResourceManager" && declaringType.Namespace == "System.Resources";
		}
	}
}
