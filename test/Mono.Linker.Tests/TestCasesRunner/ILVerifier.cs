// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILVerify;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;
using Internal.TypeSystem.Ecma;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Mono.Linker.Tests.Extensions;
using NUnit.Framework;
using System.Resources;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;
using System.Drawing;

#nullable enable
namespace Mono.Linker.Tests.TestCasesRunner
{
	class ILVerifier : ILVerify.IResolver
	{
		Verifier _verifier;
		string _assemblyName;
		NPath _assemblyPath;
		NPath _assemblyFolder;
		NPath _frameworkFolder;
		Dictionary<string, PEReader> _assemblyCache;
		AssemblyLoadContext _alc;

		public IEnumerable<VerificationResult> Results { get; private set; }

		public ILVerifier (NPath assemblyPath)
		{
			_assemblyPath = assemblyPath;
			_assemblyName = assemblyPath.FileNameWithoutExtension;
			_assemblyFolder = assemblyPath.Parent;
			_assemblyCache = new Dictionary<string, PEReader> ();
			_frameworkFolder = typeof (object).Assembly.Location.ToNPath ().Parent;
			_alc = new AssemblyLoadContext (_assemblyFolder.FileName);
			LoadAssembly ("mscorlib");
			LoadAssembly ("System.Private.CoreLib");
			LoadAssemblyFromPath (_assemblyName, _assemblyPath);

			_verifier = new ILVerify.Verifier (
				this,
				new ILVerify.VerifierOptions {
					SanityChecks = true,
					IncludeMetadataTokensInErrorMessages = true
				});
			_verifier.SetSystemModuleName (new AssemblyName ("mscorlib"));

			var allResults = _verifier.Verify (Resolve (_assemblyName))
				?? Enumerable.Empty<VerificationResult> ();

			Results = allResults.Where (r => r.Code switch {
				ILVerify.VerifierError.None
				// Static interface methods cause this warning
				or ILVerify.VerifierError.CallAbstract
				// "Missing callVirt after constrained prefix - static interface methods cause this warning
				or ILVerify.VerifierError.Constrained
				// ex. localloc cannot be statically verified by ILVerify
				or ILVerify.VerifierError.Unverifiable
				// ref returning a ref local causes this warning but is okay
				or VerifierError.ReturnPtrToStack
				// Span indexing with indexer (ex. span[^4]) causes this warning
				or VerifierError.InitOnly
				=> false,
				_ => true
			});
		}

		PEReader LoadAssembly (string assemblyName)
		{
			if (_assemblyCache.TryGetValue (assemblyName, out PEReader? reader))
				return reader;
			var assembly = _alc.LoadFromAssemblyName (new AssemblyName (assemblyName));
			reader = new PEReader (File.OpenRead (assembly.Location));
			_assemblyCache.Add (assemblyName, reader);
			return reader;
		}

		PEReader LoadAssemblyFromPath (string assemblyName, NPath pathToAssembly)
		{
			if (_assemblyCache.TryGetValue (assemblyName, out PEReader? reader))
				return reader;
			var assembly = _alc.LoadFromAssemblyPath (pathToAssembly);
			reader = new PEReader (File.OpenRead (assembly.Location));
			_assemblyCache.Add (assemblyName, reader);
			return reader;
		}

		bool TryLoadAssemblyFromFolder (string assemblyName, NPath folder, [NotNullWhen (true)] out PEReader? peReader)
		{
			Assembly? assembly = null;
			string assemblyPath = Path.Join (folder.ToString (), assemblyName);
			if (File.Exists (assemblyPath + ".dll"))
				assembly = _alc.LoadFromAssemblyPath (assemblyPath + ".dll");
			else if (File.Exists (assemblyPath + ".exe"))
				assembly = _alc.LoadFromAssemblyPath (assemblyPath + ".exe");

			if (assembly is not null) {
				peReader = new PEReader (File.OpenRead (assembly.Location));
				_assemblyCache.Add (assemblyName, peReader);
				return true;
			}
			peReader = null;
			return false;
		}

		PEReader? Resolve (string assemblyName)
		{
			PEReader? reader;
			if (_assemblyCache.TryGetValue (assemblyName, out reader)) {
				return reader;
			}

			if (TryLoadAssemblyFromFolder (assemblyName, _frameworkFolder, out reader))
				return reader;

			if (TryLoadAssemblyFromFolder (assemblyName, _assemblyFolder, out reader))
				return reader;

			return null;
		}

		PEReader? ILVerify.IResolver.ResolveAssembly (AssemblyName assemblyName)
			=> Resolve (assemblyName.Name ?? assemblyName.FullName);

		PEReader? ILVerify.IResolver.ResolveModule (AssemblyName referencingModule, string fileName)
			=> Resolve (Path.GetFileNameWithoutExtension (fileName));


		public string GetErrorMessage (VerificationResult result)
		{
			return $"IL Verification error:\n{result.Message}";
		}
	}
}
#nullable restore
