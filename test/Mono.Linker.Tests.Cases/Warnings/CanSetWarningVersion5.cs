using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[SkipKeptItemsValidation]
	[SetupLinkerArgument ("--verbose")]
	[SetupLinkerArgument ("--warn", "5")]
	[ExpectedNoWarnings]
	public class CanSetWarningVersion5
	{
		public static void Main ()
		{
			GetMethod ();
			AccessCompilerGeneratedCode.Test ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		static string type;

		[ExpectedWarning ("IL2075")]
		static void GetMethod ()
		{
			_ = Type.GetType (type).GetMethod ("Method");
		}

		class AccessCompilerGeneratedCode
		{
			static void LambdaWithDataflow ()
			{
				var lambda =
				() => {
					var t = GetAll ();
					t.RequiresAll ();
				};
				lambda ();
			}

			// This warns with --warn 7, but not --warn 5.
			public static void Test ()
			{
				typeof (AccessCompilerGeneratedCode).RequiresAll ();
			}

			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
			static Type GetAll () => null;
		}
	}
}
