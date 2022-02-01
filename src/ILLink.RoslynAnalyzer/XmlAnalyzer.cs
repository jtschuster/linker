// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public class XmlAnalyzer : DiagnosticAnalyzer
	{
		private static readonly DiagnosticDescriptor s_moreThanOneValueForParameterOfMethod = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.XmlMoreThanOneValueForParameterOfMethod);
		private static readonly DiagnosticDescriptor s_errorProcessingXmlLocation = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.ErrorProcessingXmlLocation);
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> LinkAttributes.SupportedDiagnosticIds.Select(diagnosticId => DiagnosticDescriptors.GetDiagnosticDescriptor(diagnosticId))
				.Union(new[] { s_moreThanOneValueForParameterOfMethod, s_errorProcessingXmlLocation }).ToImmutableArray();

		private static readonly Regex _linkAttributesRegex = new (@"ILLink\.LinkAttributes.*\.xml");

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);

			context.RegisterCompilationStartAction (context => {
				// Report Diagnostics on malformed XML with additionalFileContext and register actions to resolve names with (CompilationStartAnalysisContext) context.
				context.RegisterAdditionalFileAction (additionalFileContext => {
					// Check if it's a LinkAttributes xml
					if (_linkAttributesRegex.IsMatch (Path.GetFileName (additionalFileContext.AdditionalFile.Path))) {
						if (additionalFileContext.AdditionalFile.GetText () is not SourceText text)
							return;

						DiagnosticReporters.Add (text, ReportDiagnostic(additionalFileContext));
						Filenames.Add (text, additionalFileContext.AdditionalFile.Path);

						//if (!ValidateLinkAttributesXml (additionalFileContext, text))
						//	return;

						if (!context.TryGetValue (text, ProcessXmlProvider, out var xmlData) || xmlData is null) {
							additionalFileContext.ReportDiagnostic (Diagnostic.Create (s_errorProcessingXmlLocation, null, additionalFileContext.AdditionalFile.Path));
							return;
						}
						foreach (var root in xmlData) {
							if (root is LinkAttributes.TypeNode typeNode) {
								foreach (var duplicatedMethods in typeNode.Methods.GroupBy (m => m.Name).Where (m => m.Count () > 0)) {
									additionalFileContext.ReportDiagnostic (Diagnostic.Create (s_moreThanOneValueForParameterOfMethod, null, duplicatedMethods.FirstOrDefault ().Name, typeNode.FullName));
								}
							}
						}
					}
				});
			});
		}

		private static XmlSchema GenerateLinkAttributesSchema ()
		{
			var assembly = Assembly.GetExecutingAssembly ();
			using var schemaStream = assembly.GetManifestResourceStream ("ILLink.RoslynAnalyzer.ILLink.LinkAttributes.xsd");
			using var reader = XmlReader.Create (schemaStream);
			var schema = XmlSchema.Read (
				reader,
				null);
			return schema;
		}
		private static readonly XmlSchema LinkAttributesSchema = GenerateLinkAttributesSchema ();

		private static bool ValidateLinkAttributesXml (AdditionalFileAnalysisContext context, SourceText text)
		{
			var xmlStream = GenerateStream (text);
			XDocument? document;
			try {
				document = XDocument.Load (xmlStream, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
			} catch (XmlException ex) {
				context.ReportDiagnostic (Diagnostic.Create (s_errorProcessingXmlLocation, null, ex.Message));
				return false;
			}
			XmlSchemaSet schemaSet = new XmlSchemaSet ();
			schemaSet.Add (LinkAttributesSchema);
			bool valid = true;
			document.Validate (schemaSet, (sender, error) => {
//				var lineposition = new LinePosition(error.Exception.LineNumber, error.Exception.LinePosition);
//				var message = error.Message;
//				if (sender is XElement element) {
//					message = message.Insert(0, $"Element <{element.Name.LocalName}>: ");
//				}
//				context.ReportDiagnostic (Diagnostic.Create (
//					s_errorProcessingXmlLocation,
//					Location.Create(context.AdditionalFile.Path, new TextSpan(), new LinePositionSpan(lineposition, lineposition)),
//					message.ToString()));
//				valid = false;
			});
			return valid;
		}

		private static Stream GenerateStream (SourceText xmlText)
		{
			MemoryStream stream = new MemoryStream ();
			using (StreamWriter writer = new StreamWriter (stream, Encoding.UTF8, 1024, true)) {
				xmlText.Write (writer);
			}
			stream.Position = 0;
			return stream;
		}

		static LinkAttributes.ReportDiagnostic ReportDiagnostic (AdditionalFileAnalysisContext context)
		{
		return (DiagnosticId diagnosticId, IXmlLineInfo? lineInfo, string[] messageArgs) =>
			{
				var severity = DiagnosticSeverity.Warning;
				if ((int) diagnosticId < 2000)
					severity = DiagnosticSeverity.Error;
				context.ReportDiagnostic (Diagnostic.Create (
					DiagnosticDescriptors.GetDiagnosticDescriptor (diagnosticId),
					lineInfo?.ToLocation (context.AdditionalFile.Path),
					severity,
					null,
					null,
					messageArgs));
			};
		}

		static ListDictionary DiagnosticReporters = new ();
		static ListDictionary Filenames = new ();
		

		// Used in context.TryGetValue to cache the xml model
		public static readonly SourceTextValueProvider<List<LinkAttributes.IRootNode>?> ProcessXmlProvider = new ((sourceText) => {
			Stream stream = GenerateStream (sourceText);
			XDocument? document;
			try {
				document = XDocument.Load (stream, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
			} catch (System.Xml.XmlException) {
				return null;
			}
			if (DiagnosticReporters[sourceText] is LinkAttributes.ReportDiagnostic reportDiagnostic
				&& Filenames[sourceText] is string filename)
				return LinkAttributes.ProcessXml (filename, document, reportDiagnostic);
			return null;
		});
	}

	static class IXmlLineInfoExtensions
	{
		public static Location ToLocation(this IXmlLineInfo xmlLineInfo, string filename)
		{
			var linePosition = new LinePosition (xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
			return Location.Create (filename, new TextSpan(), new LinePositionSpan (linePosition, linePosition));
		}
	}
}
