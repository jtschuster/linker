// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using ILLink.Shared;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public class DynamicallyAccessedMembersAnalyzer : DiagnosticAnalyzer
	{
		internal const string DynamicallyAccessedMembers = nameof (DynamicallyAccessedMembers);
		internal const string DynamicallyAccessedMembersAttribute = nameof (DynamicallyAccessedMembersAttribute);
		public const string attributeArgument = "attributeArgument";
		public const string FullyQualifiedDynamicallyAccessedMembersAttribute = "System.Diagnostics.CodeAnalysis." + DynamicallyAccessedMembersAttribute;

		public static ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnostics ()
		{
			var diagDescriptorsArrayBuilder = ImmutableArray.CreateBuilder<DiagnosticDescriptor> (26);
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCode));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersIsNotAllowedOnMethods));
			AddRange (DiagnosticId.MethodParameterCannotBeStaticallyDetermined, DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter);
			AddRange (DiagnosticId.DynamicallyAccessedMembersOnFieldCanOnlyApplyToTypesOrStrings, DiagnosticId.DynamicallyAccessedMembersOnPropertyCanOnlyApplyToTypesOrStrings);
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersOnMethodReturnValueCanOnlyApplyToTypesOrStrings));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersFieldAccessedViaReflection));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.UnrecognizedTypeInRuntimeHelpersRunClassConstructor));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnGenericParameterBetweenOverrides));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnImplicitThisBetweenOverrides));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersConflictsBetweenPropertyAndAccessor));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.PropertyAccessorParameterInLinqExpressionsCannotBeStaticallyDetermined));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.MakeGenericType));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.MakeGenericMethod));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.CaseInsensitiveTypeGetTypeCallIsNotSupported));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.UnrecognizedTypeNameInTypeGetType));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.UnrecognizedParameterInMethodCreateInstance));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.ParametersOfAssemblyCreateInstanceCannotBeAnalyzed));
			return diagDescriptorsArrayBuilder.ToImmutable ();

			void AddRange (DiagnosticId first, DiagnosticId last)
			{
				Debug.Assert ((int) first < (int) last);

				for (int i = (int) first;
					i <= (int) last; i++) {
					diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor ((DiagnosticId) i));
				}
			}
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => GetSupportedDiagnostics ();

		public override void Initialize (AnalysisContext context)
		{
			if (!System.Diagnostics.Debugger.IsAttached)
				context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterCompilationStartAction (context => {
				if (!context.Options.IsMSBuildPropertyValueTrue (MSBuildPropertyOptionNames.EnableTrimAnalyzer, context.Compilation))
					return;

				context.RegisterOperationBlockAction (context => {
					if (context.OwningSymbol.IsInRequiresUnreferencedCodeAttributeScope ())
						return;


					foreach (var operationBlock in context.OperationBlocks) {
						TrimDataFlowAnalysis trimDataFlowAnalysis = new (context, operationBlock);
						trimDataFlowAnalysis.InterproceduralAnalyze ();
						foreach (var diagnostic in trimDataFlowAnalysis.TrimAnalysisPatterns.CollectDiagnostics ())
							context.ReportDiagnostic (diagnostic);
					}
				});
				context.RegisterSyntaxNodeAction (context => {
					ProcessGenericParameters (context);
				}, SyntaxKind.GenericName);
				context.RegisterSymbolAction (context => {
					VerifyMemberOnlyApplyToTypesOrStrings (context, context.Symbol);
					VerifyDamOnPropertyAndAccessorMatch (context, (IMethodSymbol) context.Symbol);
					VerifyDamOnDerivedAndBaseMethodsMatch (context, (IMethodSymbol) context.Symbol);
				}, SymbolKind.Method);
				context.RegisterSymbolAction (context => {
					VerifyDamOnInterfaceAndImplementationMethodsMatch (context, (INamedTypeSymbol) context.Symbol);
				}, SymbolKind.NamedType);
				context.RegisterSymbolAction (context => {
					VerifyMemberOnlyApplyToTypesOrStrings (context, context.Symbol);
				}, SymbolKind.Property);
				context.RegisterSymbolAction (context => {
					VerifyMemberOnlyApplyToTypesOrStrings (context, context.Symbol);
				}, SymbolKind.Field);
			});
		}

		static void ProcessGenericParameters (SyntaxNodeAnalysisContext context)
		{
			// RUC on the containing symbol normally silences warnings, but when examining a generic base type,
			// the containing symbol is the declared derived type. RUC on the derived type does not silence
			// warnings about base type arguments.
			if (context.ContainingSymbol is not null
				&& context.ContainingSymbol is not INamedTypeSymbol
				&& context.ContainingSymbol.IsInRequiresUnreferencedCodeAttributeScope ())
				return;

			var symbol = context.SemanticModel.GetSymbolInfo (context.Node).Symbol;

			// Avoid unnecessary execution if not NamedType or Method
			if (symbol is not INamedTypeSymbol && symbol is not IMethodSymbol)
				return;

			// Members inside nameof or cref comments, commonly used to access the string value of a variable, type, or a memeber,
			// can generate diagnostics warnings, which can be noisy and unhelpful.
			// Walking the node heirarchy to check if the member is inside a nameof/cref to not generate diagnostics
			var parentNode = context.Node;
			while (parentNode != null) {
				if (parentNode is InvocationExpressionSyntax invocationExpression &&
					invocationExpression.Expression is IdentifierNameSyntax ident1 &&
					ident1.Identifier.ValueText.Equals ("nameof"))
					return;
				else if (parentNode is CrefSyntax)
					return;

				parentNode = parentNode.Parent;
			}

			ImmutableArray<ITypeParameterSymbol> typeParams = default;
			ImmutableArray<ITypeSymbol> typeArgs = default;
			switch (symbol) {
			case INamedTypeSymbol type:
				typeParams = type.TypeParameters;
				typeArgs = type.TypeArguments;
				break;
			case IMethodSymbol targetMethod:
				typeParams = targetMethod.TypeParameters;
				typeArgs = targetMethod.TypeArguments;
				break;
			}

			if (typeParams != null) {
				Debug.Assert (typeParams.Length == typeArgs.Length);

				for (int i = 0; i < typeParams.Length; i++) {
					// Syntax like typeof (Foo<>) will have an ErrorType as the type argument.
					// These uninstantiated generics should not produce warnings.
					if (typeArgs[i].Kind == SymbolKind.ErrorType)
						continue;
					var sourceValue = SingleValueExtensions.FromTypeSymbol (typeArgs[i])!;
					var targetValue = new GenericParameterValue (typeParams[i]);
					foreach (var diagnostic in GetDynamicallyAccessedMembersDiagnostics (sourceValue, targetValue, context.Node.GetLocation ()))
						context.ReportDiagnostic (diagnostic);
				}
			}
		}

		static IEnumerable<Diagnostic> GetDynamicallyAccessedMembersDiagnostics (SingleValue sourceValue, SingleValue targetValue, Location location)
		{
			// The target should always be an annotated value, but the visitor design currently prevents
			// declaring this in the type system.
			if (targetValue is not ValueWithDynamicallyAccessedMembers targetWithDynamicallyAccessedMembers)
				throw new NotImplementedException ();

			var diagnosticContext = new DiagnosticContext (location);
			var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction (diagnosticContext, new ReflectionAccessAnalyzer ());
			requireDynamicallyAccessedMembersAction.Invoke (sourceValue, targetWithDynamicallyAccessedMembers);

			return diagnosticContext.Diagnostics;
		}

		static void VerifyMemberOnlyApplyToTypesOrStrings (SymbolAnalysisContext context, ISymbol member)
		{
			if (member is IFieldSymbol field && field.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None && !field.Type.IsTypeInterestingForDataflow ())
				context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersOnFieldCanOnlyApplyToTypesOrStrings), member.Locations[0], member.GetDisplayName ()));
			else if (member is IMethodSymbol method) {
				if (method.GetDynamicallyAccessedMemberTypesOnReturnType () != DynamicallyAccessedMemberTypes.None && !method.ReturnType.IsTypeInterestingForDataflow ())
					context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersOnMethodReturnValueCanOnlyApplyToTypesOrStrings), member.Locations[0], member.GetDisplayName ()));
				if (method.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None && !method.ContainingType.IsTypeInterestingForDataflow ())
					context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersIsNotAllowedOnMethods), member.Locations[0]));
				foreach (var parameter in method.Parameters) {
					if (parameter.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None && !parameter.Type.IsTypeInterestingForDataflow ())
						context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersOnMethodParameterCanOnlyApplyToTypesOrStrings), member.Locations[0], parameter.GetDisplayName (), member.GetDisplayName ()));
				}
			} else if (member is IPropertySymbol property && property.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None && !property.Type.IsTypeInterestingForDataflow ()) {
				context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersOnPropertyCanOnlyApplyToTypesOrStrings), member.Locations[0], member.GetDisplayName ()));
			}
		}

		static void VerifyDamOnDerivedAndBaseMethodsMatch (SymbolAnalysisContext context, IMethodSymbol methodSymbol)
		{
			if (methodSymbol.TryGetOverriddenMember (out var overriddenSymbol) && overriddenSymbol is IMethodSymbol overriddenMethod
				&& context.Symbol is IMethodSymbol method) {
				VerifyDamOnMethodsMatch (context, method, overriddenMethod);
			}
		}

		static void VerifyDamOnMethodsMatch (SymbolAnalysisContext context, IMethodSymbol method, IMethodSymbol overriddenMethod)
		{
			if (TryGetVirtualMethodAnnotationMismatchDiagnosticForReturnValue (method, overriddenMethod, out var returnDiagnostic))
				context.ReportDiagnostic (returnDiagnostic);

			for (ILParameterIndex ilIndex = 0; (int) ilIndex < method.GetILParameterCount (); ilIndex++) {
				if (TryGetVirtualMethodAnnotationMismatchDiagnosticForParameter (method, overriddenMethod, ilIndex, out var diagnostic))
					context.ReportDiagnostic (diagnostic);
			}

			for (int i = 0; i < method.TypeParameters.Length; i++) {
				if (TryGetVirtualMethodAnnotationMismatchDiagnosticForTypeParameter (method, overriddenMethod, i, out var diagnostic))
					context.ReportDiagnostic (diagnostic);
			}
		}

		public static bool TryGetVirtualMethodAnnotationMismatchDiagnosticForReturnValue (IMethodSymbol derived, IMethodSymbol @base, [NotNullWhen (true)] out Diagnostic? diagnostic)
		{
			diagnostic = null;
			var methodReturnAnnotations = FlowAnnotations.GetMethodReturnValueAnnotation (derived);
			var overriddenMethodReturnAnnotations = FlowAnnotations.GetMethodReturnValueAnnotation (@base);
			if (methodReturnAnnotations == overriddenMethodReturnAnnotations) {
				return false;
			}

			(IMethodSymbol attributableMethod, DynamicallyAccessedMemberTypes missingAttribute) = GetTargetAndRequirements (derived,
				@base, methodReturnAnnotations, overriddenMethodReturnAnnotations);

			Location attributableSymbolLocation = attributableMethod.Locations[0];

			// code fix does not support merging multiple attributes. If an attribute is present or the method is not in source, do not provide args for code fix.
			(Location[]? sourceLocation, Dictionary<string, string?>? DAMArgs) = (!attributableSymbolLocation.IsInSource
				|| (derived.TryGetReturnAttribute (DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute, out var _)
					&& @base.TryGetReturnAttribute (DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute, out var _))
					) ? (null, null) : CreateArguments (attributableSymbolLocation, missingAttribute);

			diagnostic = Diagnostic.Create (
				DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides),
				derived.Locations[0], sourceLocation, DAMArgs?.ToImmutableDictionary (), derived.GetDisplayName (), @base.GetDisplayName ());
			return true;

		}
		public static bool TryGetVirtualMethodAnnotationMismatchDiagnosticForParameter (IMethodSymbol derived, IMethodSymbol @base, ILParameterIndex ilIndex, [NotNullWhen (true)] out Diagnostic? diagnostic)
		{
			diagnostic = null;
			var derivedAnnotation = FlowAnnotations.GetMethodParameterAnnotation (derived, ilIndex);
			var baseAnnotation = FlowAnnotations.GetMethodParameterAnnotation (@base, ilIndex);
			if (derivedAnnotation == baseAnnotation)
				return false;

			if (derived.IsThisParameterIndex (ilIndex)) {
				diagnostic = Diagnostic.Create (
					DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnImplicitThisBetweenOverrides),
					derived.Locations[0],
					derived.GetDisplayName (), @base.GetDisplayName ());
				return true;
			}

			(IMethodSymbol attributableMethod, DynamicallyAccessedMemberTypes missingAttribute) =
				GetTargetAndRequirements (derived, @base, derivedAnnotation, baseAnnotation);

			Location attributableSymbolLocation = attributableMethod.GetParameterLocation (ilIndex);

			bool bothMethodsHaveDamAttribute = derived.TryGetParameterCustomAttribute (ilIndex, DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute, out var _)
				&& @base.TryGetParameterCustomAttribute (ilIndex, DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute, out var _);
			(Location[]? sourceLocation, Dictionary<string, string?>? DAMArgs) =
				(!attributableSymbolLocation.IsInSource || bothMethodsHaveDamAttribute) ? (null, null)
					: CreateArguments (attributableSymbolLocation, missingAttribute);

			diagnostic = Diagnostic.Create (
				descriptor: DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides),
				location: derived.GetParameterLocation (ilIndex),
				additionalLocations: sourceLocation,
				properties: DAMArgs?.ToImmutableDictionary (),
				derived.GetParameter (ilIndex)!.GetDisplayName (), derived.GetDisplayName (), @base.GetParameter (ilIndex)!.GetDisplayName (), @base.GetDisplayName ());
			return true;
		}

		public static bool TryGetVirtualMethodAnnotationMismatchDiagnosticForTypeParameter (IMethodSymbol derived, IMethodSymbol @base, int i, [NotNullWhen (true)] out Diagnostic? diagnostic)
		{
			diagnostic = null;
			var derivedAnnotation = derived.TypeParameters[i].GetDynamicallyAccessedMemberTypes ();
			var baseAnnotation = @base.TypeParameters[i].GetDynamicallyAccessedMemberTypes ();
			if (derivedAnnotation == baseAnnotation)
				return false;

			(IMethodSymbol attributableMethod, DynamicallyAccessedMemberTypes missingAttribute) = GetTargetAndRequirements (derived, @base, derivedAnnotation, baseAnnotation);

			Location attributableSymbolLocation = attributableMethod.TypeParameters[i].Locations[0];

			// code fix does not support merging multiple attributes. If an attribute is present or the method is not in source, do not provide args for code fix.
			(Location[]? sourceLocation, Dictionary<string, string?>? DAMArgs) = (!attributableSymbolLocation.IsInSource
				|| (derived.TypeParameters[i].TryGetAttribute (DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute, out var _)
					&& @base.TypeParameters[i].TryGetAttribute (DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute, out var _))
					) ? (null, null) : CreateArguments (attributableSymbolLocation, missingAttribute);

			diagnostic = Diagnostic.Create (
				DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnGenericParameterBetweenOverrides),
				derived.TypeParameters[i].Locations[0], sourceLocation, DAMArgs?.ToImmutableDictionary (),
				derived.TypeParameters[i].GetDisplayName (), derived.GetDisplayName (),
				@base.TypeParameters[i].GetDisplayName (), @base.GetDisplayName ());
			return true;
		}

		static void VerifyDamOnInterfaceAndImplementationMethodsMatch (SymbolAnalysisContext context, INamedTypeSymbol type)
		{
			foreach (var (interfaceMember, implementationMember) in type.GetMemberInterfaceImplementationPairs ()) {
				if (implementationMember is IMethodSymbol implementationMethod
					&& interfaceMember is IMethodSymbol interfaceMethod)
					VerifyDamOnMethodsMatch (context, implementationMethod, interfaceMethod);
			}
		}

		static void VerifyDamOnPropertyAndAccessorMatch (SymbolAnalysisContext context, IMethodSymbol methodSymbol)
		{
			if ((methodSymbol.MethodKind != MethodKind.PropertyGet && methodSymbol.MethodKind != MethodKind.PropertySet)
				|| (methodSymbol.AssociatedSymbol?.GetDynamicallyAccessedMemberTypes () == DynamicallyAccessedMemberTypes.None))
				return;

			// None on the return type of 'get' matches unannotated
			if (methodSymbol.MethodKind == MethodKind.PropertyGet
				&& methodSymbol.GetDynamicallyAccessedMemberTypesOnReturnType () != DynamicallyAccessedMemberTypes.None
				// None on parameter of 'set' matches unannotated
				|| methodSymbol.MethodKind == MethodKind.PropertySet
				&& methodSymbol.Parameters[0].GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None) {
				context.ReportDiagnostic (Diagnostic.Create (
					DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersConflictsBetweenPropertyAndAccessor),
					methodSymbol.AssociatedSymbol!.Locations[0],
					methodSymbol.AssociatedSymbol!.GetDisplayName (),
					methodSymbol.GetDisplayName ()
				));
				return;
			}
		}

		private static (IMethodSymbol Method, DynamicallyAccessedMemberTypes Requirements) GetTargetAndRequirements (IMethodSymbol method, IMethodSymbol overriddenMethod, DynamicallyAccessedMemberTypes methodAnnotation, DynamicallyAccessedMemberTypes overriddenMethodAnnotation)
		{
			DynamicallyAccessedMemberTypes mismatchedArgument;
			IMethodSymbol paramNeedsAttributes;
			if (methodAnnotation == DynamicallyAccessedMemberTypes.None) {
				mismatchedArgument = overriddenMethodAnnotation;
				paramNeedsAttributes = method;
			} else {
				mismatchedArgument = methodAnnotation;
				paramNeedsAttributes = overriddenMethod;
			}
			return (paramNeedsAttributes, mismatchedArgument);
		}

		private static (Location[]?, Dictionary<string, string?>?) CreateArguments (Location attributableSymbolLocation, DynamicallyAccessedMemberTypes mismatchedArgument)
		{
			Dictionary<string, string?>? DAMArgument = new ();
			Location[]? sourceLocation = new Location[] { attributableSymbolLocation };
			DAMArgument.Add (DynamicallyAccessedMembersAnalyzer.attributeArgument, mismatchedArgument.ToString ());
			return (sourceLocation, DAMArgument);
		}
	}
}
