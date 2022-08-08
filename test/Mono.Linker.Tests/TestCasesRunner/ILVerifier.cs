// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILVerify;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;
using Internal.TypeSystem.Ecma;
using Mono.Linker.Tests.Extensions;
using System.IO;
using Mono.Linker.Tests.Cases.Libraries;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner
{
	class ILVerifier : ILVerify.IResolver
	{
		IResolver resolver;
		Verifier _verifier;
		string _assemblyName;
		NPath _path;
		static Assembly systemModule = typeof (object).Assembly;
		public ILVerifier(LinkedTestCaseResult linkResult)
		{
			_assemblyName = linkResult.OutputAssemblyPath.FileNameWithoutExtension;
			_path = linkResult.OutputAssemblyPath;
			resolver =  this;
			_verifier = new ILVerify.Verifier (resolver, new ILVerify.VerifierOptions { SanityChecks = true });
			_verifier.SetSystemModuleName (typeof (object).Assembly.GetName ());
			AssemblyName a = new AssemblyName (linkResult.OutputAssemblyPath.FileNameWithoutExtension);
			//VerifyAssembly (a, linkResult.OutputAssemblyPath);



			var verificationResults = _verifier.Verify (resolver.ResolveAssembly (a));
			if (verificationResults.Count () != 0) {
				var failureMessages = new StringBuilder ();
				foreach (VerificationResult result in verificationResults) {
					if (result.Code == ILVerify.VerifierError.None)
						continue;
					Assert.Fail ($"ILVerify error: {result.Message}, {result}");
				}
			}
		}

		public PEReader Resolve (string simpleName)
		{
			if (simpleName == systemModule.GetName ().Name)
				return GetSystemModule ();
			if (simpleName != _assemblyName)
				return null;

			PEReader result = new PEReader (File.OpenRead (_path));
			return result;
		}

		static PEReader GetSystemModule () =>
			new PEReader (File.OpenRead (systemModule.Location));

		PEReader ILVerify.IResolver.ResolveAssembly (AssemblyName assemblyName)
			=> assemblyName.FullName == systemModule.FullName ? GetSystemModule () : Resolve (assemblyName.Name);

		PEReader ILVerify.IResolver.ResolveModule (AssemblyName referencingModule, string fileName)
			=> Resolve (Path.GetFileNameWithoutExtension (fileName));


		private string GetVerifyMethodsResult (VerificationResult result, EcmaModule module, string pathOrModuleName)
		{
			string message = "[IL]: Error [";
			if (result.Code != ILVerify.VerifierError.None) {
				message += result.Code;
			} else {
				message += result.ExceptionID;
			}
			message += "]: ";

			message += "[";
			message += pathOrModuleName;
			message += " : ";

			MetadataReader metadataReader = module.MetadataReader;

			TypeDefinition typeDef = metadataReader.GetTypeDefinition (metadataReader.GetMethodDefinition (result.Method).GetDeclaringType ());
			string typeNamespace = metadataReader.GetString (typeDef.Namespace);
			message += typeNamespace;
			message += ".";
			string typeName = metadataReader.GetString (typeDef.Name);
			message += typeName;

			message += "::";
			var method = (EcmaMethod) module.GetMethod (result.Method);
			message += PrintMethod (method);
			message += "]";

			if (result.Code != VerifierError.None) {
				message += "[offset 0x";
				message += result.GetArgumentValue<int> ("Offset").ToString ("X8");
				message += "]";

				if (result.TryGetArgumentValue ("Found", out string found)) {
					message += "[found ";
					message += found;
					message += "]";
				}

				if (result.TryGetArgumentValue ("Expected", out string expected)) {
					message += "[expected ";
					message += expected;
					message += "]";
				}

				if (result.TryGetArgumentValue ("Token", out int token)) {
					message += "[token  0x";
					message += token.ToString ("X8");
					message += "]";
				}
			}

			message += " ";
			message += result.Message + System.Environment.NewLine;
			return message;
		}
		private static string PrintMethod (EcmaMethod method)
		{
			string displayName = method.Name;
			displayName += "(";
			try {
				if (method.Signature.Length > 0) {
					bool first = true;
					for (int i = 0; i < method.Signature.Length; i++) {
						Internal.TypeSystem.TypeDesc parameter = method.Signature[i];
						if (first) {
							first = false;
						} else {
							displayName += ", ";
						}

						displayName += parameter.ToString ();
					}
				}
			} catch {
				displayName = "Error while getting method signature";
			}
			displayName += ")";
			return displayName;
		}
		//private int VerifyAssembly(AssemblyName name, string path)
  //      {
  //          PEReader peReader = Resolve(name.Name);
  //          EcmaModule module = _verifier.GetModule);

  //          return VerifyAssembly(peReader, module, path);
  //      }

  //      private int VerifyAssembly(PEReader peReader, EcmaModule module, string path)
  //      {
  //          int numErrors = 0;
  //          int verifiedMethodCounter = 0;
  //          int methodCounter = 0;
  //          int verifiedTypeCounter = 0;
  //          int typeCounter = 0;

  //          VerifyMethods(peReader, module, path, ref numErrors, ref verifiedMethodCounter, ref methodCounter);
  //          VerifyTypes(peReader, module, path, ref numErrors, ref verifiedTypeCounter, ref typeCounter);

  //          if (numErrors > 0)
  //              WriteLine(numErrors + " Error(s) Verifying " + path);
  //          else
  //              WriteLine("All Classes and Methods in " + path + " Verified.");

  //          if (_options.Statistics)
  //          {
  //              WriteLine($"Types found: {typeCounter}");
  //              WriteLine($"Types verified: {verifiedTypeCounter}");

  //              WriteLine($"Methods found: {methodCounter}");
  //              WriteLine($"Methods verified: {verifiedMethodCounter}");
  //          }

  //          return numErrors;
  //      }
	}
}