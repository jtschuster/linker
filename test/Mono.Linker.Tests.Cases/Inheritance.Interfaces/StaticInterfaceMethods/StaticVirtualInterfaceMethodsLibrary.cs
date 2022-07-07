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
	[SetupLinkerArgument ("-a", "test.exe", "library")]
	public static class StaticVirtualInterfaceMethodsLibrary
	{
		[Kept]
		public static void Main ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IStaticVirtualMethods))]
		public class ImplementVirtualIface : IStaticVirtualMethods
		{
			[Kept]
			static int IStaticVirtualMethods.Property { [Kept][KeptOverride (typeof (IStaticVirtualMethods))] get => 1; [Kept][KeptOverride (typeof (IStaticVirtualMethods))] set => _ = value; }
			[Kept]
			[KeptOverride (typeof (IStaticVirtualMethods))]
			static int IStaticVirtualMethods.Method () => 1;
			[Kept]
			[KeptOverride (typeof (IStaticVirtualMethods))]
			int IStaticVirtualMethods.InstanceMethod () => 0;
		}

		[Kept]
		[KeptInterface (typeof (IStaticVirtualMethods))]
		public class ImplementVirtualIfaceProtectedCtor : IStaticVirtualMethods
		{
			[Kept]
			protected ImplementVirtualIfaceProtectedCtor () { }
			[Kept]
			static int IStaticVirtualMethods.Property { [Kept][KeptOverride (typeof (IStaticVirtualMethods))] get => 1; [Kept][KeptOverride (typeof (IStaticVirtualMethods))] set => _ = value; }
			[Kept]
			[KeptOverride (typeof (IStaticVirtualMethods))]
			static int IStaticVirtualMethods.Method () => 1;
			[Kept]
			int IStaticVirtualMethods.InstanceMethod () => 0;
		}

		[Kept]
		[KeptInterface (typeof (IStaticVirtualMethods))]
		public class ImplementVirtualIfaceUninstantiated : IStaticVirtualMethods
		{
			private ImplementVirtualIfaceUninstantiated () { }
			[Kept]
			static int IStaticVirtualMethods.Property { [Kept][KeptOverride (typeof (IStaticVirtualMethods))] get => 1; [Kept][KeptOverride (typeof (IStaticVirtualMethods))] set => _ = value; }
			[Kept]
			[KeptOverride (typeof (IStaticVirtualMethods))]
			static int IStaticVirtualMethods.Method () => 1;
			int IStaticVirtualMethods.InstanceMethod () => 0;
		}

		[Kept]
		[KeptInterface (typeof (IStaticVirtualMethods))]
		public class ImplicitImplementVirtualIfaceUninstantiated : IStaticVirtualMethods
		{
			private ImplicitImplementVirtualIfaceUninstantiated () { }
			[Kept]
			public static int Property { [Kept][KeptOverride (typeof (IStaticVirtualMethods))] get => 1; [Kept][KeptOverride (typeof (IStaticVirtualMethods))] set => _ = value; }
			[Kept]
			[KeptOverride (typeof (IStaticVirtualMethods))]
			public static int Method () => 1;
			[Kept]
			public int InstanceMethod () => 0;
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IStaticAbstractMethods))]
		public class ImplementAbstractIface : IStaticAbstractMethods
		{
			[Kept]
			static int IStaticAbstractMethods.Property { [Kept][KeptOverride (typeof (IStaticAbstractMethods))] get => 1; [Kept][KeptOverride (typeof (IStaticAbstractMethods))] set => _ = value; }
			[Kept]
			[KeptOverride (typeof (IStaticAbstractMethods))]
			static int IStaticAbstractMethods.Method () => 1;
			[Kept]
			[KeptOverride (typeof (IStaticAbstractMethods))]
			int IStaticAbstractMethods.InstanceMethod () => 0;
		}
	}
}

