// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/Library.cs" })]
	[SetupLinkerAction ("skip", "library")]
	[SetupLinkerArgument ("-a", "test.exe")]
	public static class StaticVirtualInterfaceMethodsInPreserveScope
	{
		[Kept]
		public static void Main ()
		{
			NotRelevantToVariantCasting.Keep ();
			var t = typeof (RelevantToVariantCasting);
			MarkInterfaceMethods<UsedAsTypeArgument> ();
			var x = new InstantiatedClass ();
		}

		[Kept]
		static void MarkInterfaceMethods<T> () where T : IStaticVirtualMethods
		{
			T.Property = T.Property + 1;
			T.Method ();
			CallInstanceMethod (null);

			[Kept]
			void CallInstanceMethod (IStaticVirtualMethods x)
			{
				x.InstanceMethod ();
			}
		}

		[Kept]
		[KeptInterface (typeof (IStaticVirtualMethods))]
		public class RelevantToVariantCasting : IStaticVirtualMethods
		{
			[Kept]
			static int IStaticVirtualMethods.Property { [Kept][KeptOverride (typeof (IStaticVirtualMethods))] get => 1; [Kept][KeptOverride (typeof (IStaticVirtualMethods))] set => _ = value; }
			[Kept]
			[KeptOverride (typeof (IStaticVirtualMethods))]
			static int IStaticVirtualMethods.Method () => 1;
			int IStaticVirtualMethods.InstanceMethod () => 0;
		}

		[Kept]
		[KeptInterface (typeof (IStaticVirtualMethods))]
		public class UsedAsTypeArgument : IStaticVirtualMethods
		{
			[Kept]
			static int IStaticVirtualMethods.Property { [Kept][KeptOverride (typeof (IStaticVirtualMethods))] get => 1; [Kept][KeptOverride (typeof (IStaticVirtualMethods))] set => _ = value; }
			[Kept]
			[KeptOverride (typeof (IStaticVirtualMethods))]
			static int IStaticVirtualMethods.Method () => 1;
			int IStaticVirtualMethods.InstanceMethod () => 0;
		}

		[Kept]
		public class NotRelevantToVariantCasting : IStaticVirtualMethods
		{
			[Kept]
			public static void Keep () { }
			static int IStaticVirtualMethods.Property { get => 1; set => _ = value; }
			static int IStaticVirtualMethods.Method () => 1;
			int IStaticVirtualMethods.InstanceMethod () => 0;
		}
		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IStaticVirtualMethods))]
		public class InstantiatedClass : IStaticVirtualMethods
		{
			[Kept] //Should be able to remove if not relevant to variant casting
			static int IStaticVirtualMethods.Property { [Kept][KeptOverride (typeof (IStaticVirtualMethods))] get => 1; [Kept][KeptOverride (typeof (IStaticVirtualMethods))] set => _ = value; }
			[Kept] //Should be able to remove if not relevant to variant casting
			[KeptOverride (typeof (IStaticVirtualMethods))]
			static int IStaticVirtualMethods.Method () => 1;
			[Kept]
			int IStaticVirtualMethods.InstanceMethod () => 0;
		}
	}
}

