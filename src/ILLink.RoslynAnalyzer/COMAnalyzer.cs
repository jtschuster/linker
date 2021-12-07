﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public sealed class COMAnalyzer : DiagnosticAnalyzer
	{
		private const string StructLayoutAttribute = nameof (StructLayoutAttribute);
		private const string DllImportAttribute = nameof (DllImportAttribute);
		private const string MarshalAsAttribute = nameof (MarshalAsAttribute);
		private const string RequiresUnreferencedCodeAttribute = nameof (RequiresUnreferencedCodeAttribute);

		static readonly DiagnosticDescriptor s_correctnessOfCOMCannotBeGuaranteed = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.CorrectnessOfCOMCannotBeGuaranteed,
			helpLinkUri: "https://docs.microsoft.com/en-us/dotnet/core/deploying/trim-warnings/il2050");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (s_correctnessOfCOMCannotBeGuaranteed);

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterCompilationStartAction (context => {
				var compilation = context.Compilation;
				if (!context.Options.IsMSBuildPropertyValueTrue (MSBuildPropertyOptionNames.EnableTrimAnalyzer, compilation))
					return;

				context.RegisterOperationAction (operationContext => {
					var invocationOperation = (IInvocationOperation) operationContext.Operation;
					var targetMethod = invocationOperation.TargetMethod;
					if (!targetMethod.HasAttribute (DllImportAttribute))
						return;

					if (operationContext.ContainingSymbol is ISymbol containingSymbol) {
						if (containingSymbol.IsInRequiresScope(RequiresUnreferencedCodeAttribute)) {
							return;
						}
					}

					bool comDangerousMethod = IsComInterop (targetMethod.ReturnType);
					foreach (var parameter in targetMethod.Parameters) {
						comDangerousMethod |= IsComInterop (parameter);
					}

					if (comDangerousMethod) {
						operationContext.ReportDiagnostic (Diagnostic.Create (s_correctnessOfCOMCannotBeGuaranteed,
							operationContext.Operation.Syntax.GetLocation (), targetMethod.GetDisplayName ()));
					}
				}, OperationKind.Invocation);
			});

			static bool IsComInterop (ISymbol symbol)
			{
				if (symbol.TryGetAttribute (MarshalAsAttribute, out var marshalAsAttribute) &&
					marshalAsAttribute.ConstructorArguments.Length >= 1 && marshalAsAttribute.ConstructorArguments[0] is TypedConstant typedConstant &&
					typedConstant.Type != null && typedConstant.Type.IsUnmanagedType) {
					var unmanagedType = typedConstant.Value;
					switch (unmanagedType) {
					case (int) UnmanagedType.IUnknown:
					case (int) UnmanagedType.IDispatch:
					case (int) UnmanagedType.Interface:
						return true;

					default:
						if (Enum.IsDefined (typeof (UnmanagedType), unmanagedType))
							return false;

						break;
					}
				}

				if (symbol.IsInterface ())
					return true;

				ITypeSymbol? typeSymbol = symbol is ITypeSymbol ? symbol as ITypeSymbol : null;
				if (symbol is IParameterSymbol parameterSymbol)
					typeSymbol = parameterSymbol.Type;

				if (typeSymbol == null)
					return false;

				if (typeSymbol.ContainingNamespace.Name == "System" && typeSymbol.Name == "Array") {
					// System.Array marshals as IUnknown by default
					return true;
				} else if (typeSymbol.ContainingNamespace.Name == "System" && typeSymbol.Name == "String" ||
					typeSymbol.ContainingNamespace.Name == "System.Text" && typeSymbol.Name == "StringBuilder") {
					// String and StringBuilder are special cased by interop
					return false;
				}

				if (typeSymbol.IsValueType) {
					// Value types don't marshal as COM
					return false;
				} else if (typeSymbol.IsInterface ()) {
					// Interface types marshal as COM by default
					return true;
				} else if (typeSymbol.ContainingNamespace.Name == "System" &&
					typeSymbol.Name == "MulticastDelegate") {
					// Delegates are special cased by interop
					return false;
				} else if (typeSymbol.IsSubclassOf ("System.Runtime.InteropServices", "CriticalHandle") ||
					typeSymbol.IsSubclassOf ("System.Runtime.InteropServices", "SafeHandle")) {
					// Subclasses of CriticalHandle and SafeHandle are special cased by interop
					return false;
				} else if (typeSymbol.TryGetAttribute (StructLayoutAttribute, out var structLayoutAttribute) &&
					(LayoutKind) structLayoutAttribute.ConstructorArguments[0].Value! == LayoutKind.Auto) {
					// Rest of classes that don't have layout marshal as COM
					return true;
				}

				return false;
			}
		}
	}
}
