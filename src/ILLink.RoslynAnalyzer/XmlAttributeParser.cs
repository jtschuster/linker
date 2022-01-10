// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System.Xml.XPath;
using System.Reflection.Metadata;

namespace ILLink.RoslynAnalyzer
{
	class AnalyzerXmlAttributeParser : XmlProcessorBase
	{
		AnalyzerXmlAttributeParser (string xmlDocumentLocation, Stream documentStream) : base (xmlDocumentLocation, documentStream)
		{
		}

		private static AdditionalText? findFile (CompilationStartAnalysisContext compilationStartContext)
		{
			// Find the file with the invalid terms.
			ImmutableArray<AdditionalText> additionalFiles = compilationStartContext.Options.AdditionalFiles;
			AdditionalText? termsFile = additionalFiles.FirstOrDefault (file => Path.GetFileName (file.Path).Equals ("ILLink.LinkAttributes.xml"));
			return termsFile;
		}

		public void RegisterCompilationStartAction (CompilationStartAnalysisContext compilationStartContext)
		{
			var AttributesFile = findFile (compilationStartContext);
			if (AttributesFile == null)
				return;
			var fileText = AttributesFile.GetText ();
			if (fileText == null)
				return;
			var stream = new MemoryStream ();
			using (var writer = new StreamWriter (stream, Encoding.UTF8, 1024, true)) {
				fileText.Write (writer);
			}
			stream.Position = 0;
			ProcessXml ();
		}
		protected virtual void ProcessXml ()
		{
			//if (!AllowedAssemblySelector.HasFlag (AllowedAssemblies.AnyAssembly) && _resource == null)
			//	throw new InvalidOperationException ("The containing assembly must be specified for XML which is restricted to modifying that assembly only.");

			try {
				XPathNavigator nav = _document.CreateNavigator ();

				// Initial structure check - ignore XML document which don't look like linker XML format
				if (!nav.MoveToChild (LinkerElementName, XmlNamespace))
					return;

				//if (_resource != null) {
				//	if (stripResource)
				//		_context.Annotations.AddResourceToRemove (_resource.Value.Assembly, _resource.Value.Resource);
				//	if (ignoreResource)
				//		return;
				//}

				if (!ShouldProcessElement (nav))
					return;

				ProcessAssemblies (nav);

				//// For embedded XML, allow not specifying the assembly explicitly in XML.
				//if (_resource != null)
				//	ProcessAssembly (_resource.Value.Assembly, nav, warnOnUnresolvedTypes: true);

			} catch (Exception ex) when (!(ex is LinkerFatalErrorException)) {
				throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage (null, DiagnosticId.ErrorProcessingXmlLocation, _xmlDocumentLocation), ex);
			}
		}

		protected virtual void ProcessAssemblies (XPathNavigator nav)
		{
			foreach (XPathNavigator assemblyNav in nav.SelectChildren ("assembly", "")) {
				// Errors for invalid assembly names should show up even if this element will be
				// skipped due to feature conditions.
				bool processAllAssemblies = ShouldProcessAllAssemblies (assemblyNav, out AssemblyNameReference? name);
				if (processAllAssemblies && AllowedAssemblySelector != AllowedAssemblies.AllAssemblies) {
					LogWarning ($"XML contains unsupported wildcard for assembly 'fullname' attribute.", 2100, assemblyNav);
					continue;
				}

				AssemblyDefinition? assemblyToProcess = null;
				if (!AllowedAssemblySelector.HasFlag (AllowedAssemblies.AnyAssembly)) {
					Debug.Assert (!processAllAssemblies);
					Debug.Assert (_resource != null);
					if (_resource.Value.Assembly.Name.Name != name!.Name) {
						LogWarning ($"Embedded XML in assembly '{_resource.Value.Assembly.Name.Name}' contains assembly 'fullname' attribute for another assembly '{name}'.", 2101, assemblyNav);
						continue;
					}
					assemblyToProcess = _resource.Value.Assembly;
				}

				if (!ShouldProcessElement (assemblyNav))
					continue;

				if (processAllAssemblies) {
					// We could avoid loading all references in this case: https://github.com/dotnet/linker/issues/1708
					foreach (AssemblyDefinition assembly in _context.GetReferencedAssemblies ())
						ProcessAssembly (assembly, assemblyNav, warnOnUnresolvedTypes: false);
				} else {
					Debug.Assert (!processAllAssemblies);
					AssemblyDefinition? assembly = assemblyToProcess ?? _context.TryResolve (name!);

					if (assembly == null) {
						LogWarning ($"Could not resolve assembly '{name!.Name}'.", 2007, assemblyNav);
						continue;
					}

					ProcessAssembly (assembly, assemblyNav, warnOnUnresolvedTypes: true);
				}
			}
		}
		
		protected virtual void ProcessTypes (AssemblyDefinition assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
		{
			foreach (XPathNavigator typeNav in nav.SelectChildren (TypeElementName, XmlNamespace)) {

				if (!ShouldProcessElement (typeNav))
					continue;

				string fullname = GetFullName (typeNav);

				if (fullname.IndexOf ("*") != -1) {
					if (ProcessTypePattern (fullname, assembly, typeNav))
						continue;
				}

				TypeDefinition type = assembly.MainModule.GetType (fullname);

				if (type == null && assembly.MainModule.HasExportedTypes) {
					foreach (var exported in assembly.MainModule.ExportedTypes) {
						if (fullname == exported.FullName) {
							var resolvedExternal = ProcessExportedType (exported, assembly, typeNav);
							if (resolvedExternal != null) {
								type = resolvedExternal;
								break;
							}
						}
					}
				}

				if (type == null) {
					if (warnOnUnresolvedTypes)
						LogWarning ($"Could not resolve type '{fullname}'.", 2008, typeNav);
					continue;
				}

				ProcessType (type, typeNav);
			}
		}

		CustomAttribute[]? ProcessAttributes (XPathNavigator nav, System.Reflection.ICustomAttributeProvider provider)
		{
			var builder = new ArrayBuilder<CustomAttribute> ();
			foreach (XPathNavigator argumentNav in nav.SelectChildren ("attribute", string.Empty)) {
				if (!ShouldProcessElement (argumentNav))
					continue;

				TypeDefinition? attributeType;
				string internalAttribute = GetAttribute (argumentNav, "internal");
				if (!string.IsNullOrEmpty (internalAttribute)) {
					attributeType = GenerateRemoveAttributeInstancesAttribute ();
					if (attributeType == null)
						continue;

					// TODO: Replace with IsAttributeType check once we have it
					if (provider is not TypeDefinition) {
						LogWarning ($"Internal attribute '{attributeType.Name}' can only be used on attribute types.", 2048, argumentNav);
						continue;
					}
				} else {
					string attributeFullName = GetFullName (argumentNav);
					if (string.IsNullOrEmpty (attributeFullName)) {
						LogWarning ($"'attribute' element does not contain attribute 'fullname' or it's empty.", 2029, argumentNav);
						continue;
					}

					if (!GetAttributeType (argumentNav, attributeFullName, out attributeType))
						continue;
				}

				CustomAttribute? customAttribute = CreateCustomAttribute (argumentNav, attributeType);
				if (customAttribute != null) {
					_context.LogMessage ($"Assigning external custom attribute '{FormatCustomAttribute (customAttribute)}' instance to '{provider}'.");
					builder.Add (customAttribute);
				}
			}

			return builder.ToArray ();

			static string FormatCustomAttribute (CustomAttribute ca)
			{
				StringBuilder sb = new StringBuilder ();
				sb.Append (ca.Constructor.GetDisplayName ());
				sb.Append (" { args: ");
				for (int i = 0; i < ca.ConstructorArguments.Count; ++i) {
					if (i > 0)
						sb.Append (", ");

					var caa = ca.ConstructorArguments[i];
					sb.Append ($"{caa.Type.GetDisplayName ()} {caa.Value}");
				}
				sb.Append (" }");

				return sb.ToString ();
			}

			
		}
		// Looks at features settings in the XML to determine whether the element should be processed
		protected virtual bool ShouldProcessElement (XPathNavigator nav) => true;
	}
}
