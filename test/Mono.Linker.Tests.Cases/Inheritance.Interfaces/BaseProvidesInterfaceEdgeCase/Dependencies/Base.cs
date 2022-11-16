﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.BaseProvidesInterfaceEdgeCase.Dependencies
{
	public class Base
	{
		public virtual void Method()
		{
			throw new NotImplementedException();
		}
	}
}
