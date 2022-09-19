// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared.TypeSystemProxy;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
	static class Intrinsics
	{
		public static IntrinsicId GetIntrinsicIdForMethod (MethodProxy calledMethod)
		{
			return calledMethod.Name switch {
				// static System.Reflection.IntrospectionExtensions.GetTypeInfo (Type type)
				/// <see cref="System.Reflection.IntrospectionExtensions.GetTypeInfo(System.Type)"/>
				"GetTypeInfo" when calledMethod.IsDeclaredOnType ("System.Reflection.IntrospectionExtensions") => IntrinsicId.IntrospectionExtensions_GetTypeInfo,

				// System.Reflection.TypeInfo.AsType ()
				/// <see cref="System.Reflection.TypeInfo.AsType()" />
				"AsType" when calledMethod.IsDeclaredOnType ("System.Reflection.TypeInfo") => IntrinsicId.TypeInfo_AsType,

				// System.Type.GetTypeFromHandle (Type type)
				/// <see cref="System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)" />
				"GetTypeFromHandle" when calledMethod.IsDeclaredOnType ("System.Type") => IntrinsicId.Type_GetTypeFromHandle,

				// System.Type.TypeHandle getter
				/// <see cref="System.Type.TypeHandle" />
				"get_TypeHandle" when calledMethod.IsDeclaredOnType ("System.Type") => IntrinsicId.Type_get_TypeHandle,

				// System.Reflection.MethodBase.GetMethodFromHandle (RuntimeMethodHandle handle)
				/// <see cref="System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)" />
				// System.Reflection.MethodBase.GetMethodFromHandle (RuntimeMethodHandle handle, RuntimeTypeHandle declaringType)
				/// <see cref="System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle, System.RuntimeTypeHandle)" />
				"GetMethodFromHandle" when calledMethod.IsDeclaredOnType ("System.Reflection.MethodBase")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 0, "System.RuntimeMethodHandle")
					&& (calledMethod.HasNonThisParametersCount (1) || calledMethod.HasNonThisParametersCount (2))
					=> IntrinsicId.MethodBase_GetMethodFromHandle,

				// System.Reflection.MethodBase.MethodHandle getter
				/// <see cref="System.Reflection.MethodBase.MethodHandle"/>
				"get_MethodHandle" when calledMethod.IsDeclaredOnType ("System.Reflection.MethodBase") => IntrinsicId.MethodBase_get_MethodHandle,

				// static System.Type.MakeGenericType (Type [] typeArguments)
				/// <see cref="System.Type.MakeGenericType(System.Type[])"/>
				"MakeGenericType" when calledMethod.IsDeclaredOnType ("System.Type") => IntrinsicId.Type_MakeGenericType,

				// static System.Reflection.RuntimeReflectionExtensions.GetRuntimeEvent (this Type type, string name)
				/// <see cref="System.Reflection.RuntimeReflectionExtensions.GetRuntimeEvent(System.Type, string)"/>
				"GetRuntimeEvent" when calledMethod.IsDeclaredOnType ("System.Reflection.RuntimeReflectionExtensions")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 0, "System.Type")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.String")
					=> IntrinsicId.RuntimeReflectionExtensions_GetRuntimeEvent,

				// static System.Reflection.RuntimeReflectionExtensions.GetRuntimeField (this Type type, string name)
				/// <see cref="System.Reflection.RuntimeReflectionExtensions.GetRuntimeField(System.Type, string)"/>
				"GetRuntimeField" when calledMethod.IsDeclaredOnType ("System.Reflection.RuntimeReflectionExtensions")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 0, "System.Type")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.,String")
					=> IntrinsicId.RuntimeReflectionExtensions_GetRuntimeField,

				// static System.Reflection.RuntimeReflectionExtensions.GetRuntimeMethod (this Type type, string name, Type[] parameters)
				/// <see cref="System.Reflection.RuntimeReflectionExtensions.GetRuntimeMethod(System.Type, string, System.Type[])"/>
				"GetRuntimeMethod" when calledMethod.IsDeclaredOnType ("System.Reflection.RuntimeReflectionExtensions")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 0, "System.Type")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.String")
					=> IntrinsicId.RuntimeReflectionExtensions_GetRuntimeMethod,

				// static System.Reflection.RuntimeReflectionExtensions.GetRuntimeProperty (this Type type, string name)
				/// <see cref="System.Reflection.RuntimeReflectionExtensions.GetRuntimeProperty(System.Type, string)"/>
				"GetRuntimeProperty" when calledMethod.IsDeclaredOnType ("System.Reflection.RuntimeReflectionExtensions")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 0, "System.Type")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.String")
					=> IntrinsicId.RuntimeReflectionExtensions_GetRuntimeProperty,

				// static System.Linq.Expressions.Expression.Call (Type, String, Type[], Expression[])
				/// <see cref="System.Linq.Expressions.Expression.Call(System.Type, string, System.Type[], System.Linq.Expressions.Expression[])"/>
				"Call" when calledMethod.IsDeclaredOnType ("System.Linq.Expressions.Expression")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 0, "System.Type")
					&& calledMethod.HasNonThisParametersCount (4)
					=> IntrinsicId.Expression_Call,

				// static System.Linq.Expressions.Expression.Field (Expression, Type, String)
				/// <see cref="System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Type, string)"/>
				"Field" when calledMethod.IsDeclaredOnType ("System.Linq.Expressions.Expression")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.Type")
					&& calledMethod.HasNonThisParametersCount (3)
					=> IntrinsicId.Expression_Field,

				// static System.Linq.Expressions.Expression.Property (Expression, Type, String)
				/// <see cref="System.Linq.Expressions.Expression.Property(System.Linq.Expressions.Expression, System.Type, string)"/>
				// static System.Linq.Expressions.Expression.Property (Expression, MethodInfo)
				/// <see cref="System.Linq.Expressions.Expression.Property(System.Linq.Expressions.Expression, System.Reflection.MethodInfo)"/>
				"Property" when calledMethod.IsDeclaredOnType ("System.Linq.Expressions.Expression")
					&& ((calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.Type") && calledMethod.HasNonThisParametersCount (3))
					|| (calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.Reflection.MethodInfo") && calledMethod.HasNonThisParametersCount (2)))
					=> IntrinsicId.Expression_Property,

				// static System.Linq.Expressions.Expression.New (Type)
				"New" when calledMethod.IsDeclaredOnType ("System.Linq.Expressions.Expression")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 0, "System.Type")
					&& calledMethod.HasNonThisParametersCount (1)
					=> IntrinsicId.Expression_New,

				// static Array System.Enum.GetValues (Type)
				"GetValues" when calledMethod.IsDeclaredOnType ("System.Enum")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 0, "System.Type")
					&& calledMethod.HasNonThisParametersCount (1)
					=> IntrinsicId.Enum_GetValues,

				// static int System.Runtime.InteropServices.Marshal.SizeOf (Type)
				"SizeOf" when calledMethod.IsDeclaredOnType ("System.Runtime.InteropServices.Marshal")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 0, "System.Type")
					&& calledMethod.HasNonThisParametersCount (1)
					=> IntrinsicId.Marshal_SizeOf,

				// static int System.Runtime.InteropServices.Marshal.OffsetOf (Type, string)
				"OffsetOf" when calledMethod.IsDeclaredOnType ("System.Runtime.InteropServices.Marshal")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 0, "System.Type")
					&& calledMethod.HasNonThisParametersCount (2)
					=> IntrinsicId.Marshal_OffsetOf,

				// static object System.Runtime.InteropServices.Marshal.PtrToStructure (IntPtr, Type)
				"PtrToStructure" when calledMethod.IsDeclaredOnType ("System.Runtime.InteropServices.Marshal")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.Type")
					&& calledMethod.HasNonThisParametersCount (2)
					=> IntrinsicId.Marshal_PtrToStructure,

				// static void System.Runtime.InteropServices.Marshal.DestroyStructure (IntPtr, Type)
				"DestroyStructure" when calledMethod.IsDeclaredOnType ("System.Runtime.InteropServices.Marshal")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.Type")
					&& calledMethod.HasNonThisParametersCount (2)
					=> IntrinsicId.Marshal_DestroyStructure,

				// static Delegate System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer (IntPtr, Type)
				"GetDelegateForFunctionPointer" when calledMethod.IsDeclaredOnType ("System.Runtime.InteropServices.Marshal")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.Type")
					&& calledMethod.HasNonThisParametersCount (2)
					=> IntrinsicId.Marshal_GetDelegateForFunctionPointer,

				// static System.Type.GetType (string)
				// static System.Type.GetType (string, Boolean)
				// static System.Type.GetType (string, Boolean, Boolean)
				// static System.Type.GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>)
				// static System.Type.GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean)
				// static System.Type.GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean, Boolean)
				"GetType" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 0, "System.String")
					=> IntrinsicId.Type_GetType,

				// System.Type.GetConstructor (Type[])
				// System.Type.GetConstructor (BindingFlags, Type[])
				// System.Type.GetConstructor (BindingFlags, Binder, Type[], ParameterModifier [])
				// System.Type.GetConstructor (BindingFlags, Binder, CallingConventions, Type[], ParameterModifier [])
				"GetConstructor" when calledMethod.IsDeclaredOnType ("System.Type")
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetConstructor,

				// System.Type.GetConstructors (BindingFlags)
				/// <see cref="System.Type.GetConstructors(System.Reflection.BindingFlags)"/>
				"GetConstructors" when calledMethod.IsDeclaredOnType ("System.Type")
					&& !calledMethod.IsStatic ()
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.Reflection.BindingFlags")
					&& calledMethod.HasNonThisParametersCount (1)
					=> IntrinsicId.Type_GetConstructors,

				// System.Type.GetMethod (string)
				/// <see cref="System.Type.GetMethod(System.String)"/>
				// System.Type.GetMethod (string, BindingFlags)
				// System.Type.GetMethod (string, Type[])
				// System.Type.GetMethod (string, Type[], ParameterModifier[])
				// System.Type.GetMethod (string, BindingFlags, Type[])
				// System.Type.GetMethod (string, BindingFlags, Binder, Type[], ParameterModifier[])
				// System.Type.GetMethod (string, BindingFlags, Binder, CallingConventions, Type[], ParameterModifier[])
				// System.Type.GetMethod (string, int, Type[])
				// System.Type.GetMethod (string, int, Type[], ParameterModifier[]?)
				// System.Type.GetMethod (string, int, BindingFlags, Binder?, Type[], ParameterModifier[]?)
				// System.Type.GetMethod (string, int, BindingFlags, Binder?, CallingConventions, Type[], ParameterModifier[]?)
				"GetMethod" when calledMethod.IsDeclaredOnType ("System.Type")
					&& !calledMethod.IsStatic ()
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.String")
					=> IntrinsicId.Type_GetMethod,

				// System.Type.GetMethods (BindingFlags)
				"GetMethods" when calledMethod.IsDeclaredOnType ("System.Type")
					&& !calledMethod.IsStatic ()
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.Reflection.BindingFlags")
					&& calledMethod.HasNonThisParametersCount (1)
					=> IntrinsicId.Type_GetMethods,

				// System.Type.GetField (string)
				// System.Type.GetField (string, BindingFlags)
				"GetField" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.String")
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetField,

				// System.Type.GetFields (BindingFlags)
				"GetFields" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.Reflection.BindingFlags")
					&& calledMethod.HasNonThisParametersCount (1)
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetFields,

				// System.Type.GetEvent (string)
				// System.Type.GetEvent (string, BindingFlags)
				"GetEvent" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.String")
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetEvent,

				// System.Type.GetEvents (BindingFlags)
				"GetEvents" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.Reflection.BindingFlags")
					&& calledMethod.HasNonThisParametersCount (1)
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetEvents,

				// System.Type.GetNestedType (string)
				// System.Type.GetNestedType (string, BindingFlags)
				"GetNestedType" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.String")
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetNestedType,

				// System.Type.GetNestedTypes (BindingFlags)
				"GetNestedTypes" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.Reflection.BindingFlags")
					&& calledMethod.HasNonThisParametersCount (1)
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetNestedTypes,

				// System.Type.GetMember (String)
				// System.Type.GetMember (String, BindingFlags)
				// System.Type.GetMember (String, MemberTypes, BindingFlags)
				"GetMember" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.String")
					&& !calledMethod.IsStatic ()
					&& (calledMethod.HasNonThisParametersCount (1) ||
					(calledMethod.HasNonThisParametersCount (2) && calledMethod.HasParameterOfType ((ILParameterIndex) 2, "System.Reflection.BindingFlags")) ||
					(calledMethod.HasNonThisParametersCount (3) && calledMethod.HasParameterOfType ((ILParameterIndex) 3, "System.Reflection.BindingFlags")))
					=> IntrinsicId.Type_GetMember,

				// System.Type.GetMembers (BindingFlags)
				"GetMembers" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.Reflection.BindingFlags")
					&& calledMethod.HasNonThisParametersCount (1)
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetMembers,

				// System.Type.GetInterface (string)
				// System.Type.GetInterface (string, bool)
				"GetInterface" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.String")
					&& !calledMethod.IsStatic ()
					&& (calledMethod.HasNonThisParametersCount (1) ||
					(calledMethod.HasNonThisParametersCount (2) && calledMethod.HasParameterOfType ((ILParameterIndex) 2, "System.Boolean")))
					=> IntrinsicId.Type_GetInterface,

				// System.Type.AssemblyQualifiedName
				"get_AssemblyQualifiedName" when calledMethod.IsDeclaredOnType ("System.Type")
					&& !calledMethod.HasNonThisParameters ()
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_get_AssemblyQualifiedName,

				// System.Type.UnderlyingSystemType
				"get_UnderlyingSystemType" when calledMethod.IsDeclaredOnType ("System.Type")
					&& !calledMethod.HasNonThisParameters ()
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_get_UnderlyingSystemType,

				// System.Type.BaseType
				"get_BaseType" when calledMethod.IsDeclaredOnType ("System.Type")
					&& !calledMethod.HasNonThisParameters ()
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_get_BaseType,

				// System.Type.GetProperty (string)
				// System.Type.GetProperty (string, BindingFlags)
				// System.Type.GetProperty (string, Type)
				// System.Type.GetProperty (string, Type[])
				// System.Type.GetProperty (string, Type, Type[])
				// System.Type.GetProperty (string, Type, Type[], ParameterModifier[])
				// System.Type.GetProperty (string, BindingFlags, Binder, Type, Type[], ParameterModifier[])
				"GetProperty" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.String")
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetProperty,

				// System.Type.GetProperties (BindingFlags)
				"GetProperties" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.Reflection.BindingFlags")
					&& calledMethod.HasNonThisParametersCount (1)
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetProperties,

				// static System.Object.GetType ()
				"GetType" when calledMethod.IsDeclaredOnType ("System.Object")
					=> IntrinsicId.Object_GetType,

				".ctor" when calledMethod.IsDeclaredOnType ("System.Reflection.TypeDelegator")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.Type")
					=> IntrinsicId.TypeDelegator_Ctor,

				"Empty" when calledMethod.IsDeclaredOnType ("System.Array")
					=> IntrinsicId.Array_Empty,

				// static System.Activator.CreateInstance (System.Type type)
				// static System.Activator.CreateInstance (System.Type type, bool nonPublic)
				// static System.Activator.CreateInstance (System.Type type, params object?[]? args)
				// static System.Activator.CreateInstance (System.Type type, object?[]? args, object?[]? activationAttributes)
				// static System.Activator.CreateInstance (System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture)
				// static System.Activator.CreateInstance (System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture, object?[]? activationAttributes) { throw null; }
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System.Activator")
					&& !calledMethod.HasGenericParameters ()
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 0, "System.Type")
					=> IntrinsicId.Activator_CreateInstance_Type,

				// static System.Activator.CreateInstance (string assemblyName, string typeName)
				// static System.Activator.CreateInstance (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture, object?[]? activationAttributes)
				// static System.Activator.CreateInstance (string assemblyName, string typeName, object?[]? activationAttributes)
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System.Activator")
					&& !calledMethod.HasGenericParameters ()
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 0, "System.String")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.String")
					=> IntrinsicId.Activator_CreateInstance_AssemblyName_TypeName,

				// static System.Activator.CreateInstanceFrom (string assemblyFile, string typeName)
				// static System.Activator.CreateInstanceFrom (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// static System.Activator.CreateInstanceFrom (string assemblyFile, string typeName, object? []? activationAttributes)
				"CreateInstanceFrom" when calledMethod.IsDeclaredOnType ("System.Activator")
					&& !calledMethod.HasGenericParameters ()
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 0, "System.String")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.String")
					=> IntrinsicId.Activator_CreateInstanceFrom,

				// System.AppDomain.CreateInstance (string assemblyName, string typeName)
				// System.AppDomain.CreateInstance (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// System.AppDomain.CreateInstance (string assemblyName, string typeName, object? []? activationAttributes)
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System.AppDomain")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.String")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 2, "System.String")
					=> IntrinsicId.AppDomain_CreateInstance,

				// System.AppDomain.CreateInstanceAndUnwrap (string assemblyName, string typeName)
				// System.AppDomain.CreateInstanceAndUnwrap (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// System.AppDomain.CreateInstanceAndUnwrap (string assemblyName, string typeName, object? []? activationAttributes)
				"CreateInstanceAndUnwrap" when calledMethod.IsDeclaredOnType ("System.AppDomain")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.String")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 2, "System.String")
					=> IntrinsicId.AppDomain_CreateInstanceAndUnwrap,

				// System.AppDomain.CreateInstanceFrom (string assemblyFile, string typeName)
				// System.AppDomain.CreateInstanceFrom (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// System.AppDomain.CreateInstanceFrom (string assemblyFile, string typeName, object? []? activationAttributes)
				"CreateInstanceFrom" when calledMethod.IsDeclaredOnType ("System.AppDomain")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.String")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 2, "System.String")
					=> IntrinsicId.AppDomain_CreateInstanceFrom,

				// System.AppDomain.CreateInstanceFromAndUnwrap (string assemblyFile, string typeName)
				/// <see cref="System.AppDomain.CreateInstanceFromAndUnwrap(string, string)"/>
				// System.AppDomain.CreateInstanceFromAndUnwrap (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				/// <see cref="System.AppDomain.CreateInstanceFromAndUnwrap(string, string, bool, System.Reflection.BindingFlags, System.Reflection.Binder?, object?[]?, System.Globalization.CultureInfo?, object?[]?"/>
				// System.AppDomain.CreateInstanceFromAndUnwrap (string assemblyFile, string typeName, object? []? activationAttributes)
				/// <see cref="System.AppDomain.CreateInstanceFromAndUnwrap(string, string, object?[])"/>
				"CreateInstanceFromAndUnwrap" when calledMethod.IsDeclaredOnType ("System.AppDomain")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.String")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 2, "System.String")
					=> IntrinsicId.AppDomain_CreateInstanceFromAndUnwrap,

				// System.Reflection.Assembly.CreateInstance (string typeName)
				/// <see cref="System.Reflection.Assembly.CreateInstance(string)"/>
				// System.Reflection.Assembly.CreateInstance (string typeName, bool ignoreCase)
				/// <see cref="System.Reflection.Assembly.CreateInstance(string, bool)"/>
				// System.Reflection.Assembly.CreateInstance (string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object []? args, CultureInfo? culture, object []? activationAttributes)
				/// <see cref="System.Reflection.Assembly.CreateInstance(string, bool, System.Reflection.BindingFlags, System.Reflection.Binder, object[], System.Globalization.CultureInfo, object[])"/>
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System.Reflection.Assembly")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 1, "System.String")
					=> IntrinsicId.Assembly_CreateInstance,

				// System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor (RuntimeTypeHandle type)
				/// <see cref="System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(System.RuntimeTypeHandle)"/>
				"RunClassConstructor" when calledMethod.IsDeclaredOnType ("System.Runtime.CompilerServices.RuntimeHelpers")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 0, "System.RuntimeTypeHandle")
					=> IntrinsicId.RuntimeHelpers_RunClassConstructor,

				// System.Reflection.MethodInfo.MakeGenericMethod (Type[] typeArguments)
				"MakeGenericMethod" when calledMethod.IsDeclaredOnType ("System.Reflection.MethodInfo")
					&& !calledMethod.IsStatic ()
					&& calledMethod.HasNonThisParametersCount (1)
					=> IntrinsicId.MethodInfo_MakeGenericMethod,

				"GetUnderlyingType" when calledMethod.IsDeclaredOnType ("System.Nullable")
					&& calledMethod.HasParameterOfType ((ILParameterIndex) 0, "System.Type")
					&& calledMethod.IsStatic ()
					=> IntrinsicId.Nullable_GetUnderlyingType,

				_ => IntrinsicId.None,
			};
		}
	}
}
