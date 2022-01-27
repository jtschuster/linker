// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.RoslynAnalyzer
{
	internal static class SourceTextExtensions
	{
		public static TextSpan SpanFromLinePosition(this SourceText source, int line, int position)
		{
			// line is 0 indexed
			if (line < 1)
				return new TextSpan();
			return new TextSpan(source.Lines[line - 1].Start + position - 1, 0);
		}
	}
}
