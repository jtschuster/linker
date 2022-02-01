// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ILLink.Shared
{
	public static class LinkAttributes
	{
		public delegate void ReportDiagnostic (DiagnosticId diagnosticId, IXmlLineInfo? lineInfo, params string[] messageArgs);
		public static ImmutableArray<DiagnosticId> SupportedDiagnosticIds = ImmutableArray.Create (
			DiagnosticId.XmlFeatureDoesNotSpecifyFeatureValue,
			DiagnosticId.XmlRemoveAttributeInstancesCanOnlyBeUsedOnType,
			DiagnosticId.XmlElementDoesNotContainRequiredAttributeFullname,
			DiagnosticId.XmlElementDoesNotContainRequiredAttributeFullname,
			DiagnosticId.XmlUnsupportedNonBooleanValueForFeature,
			DiagnosticId.XmlUnsupportedNonBooleanValueForFeature,
			DiagnosticId.XmlDocumentLocationHasInvalidFeatureDefault
			);

		public static List<IRootNode> ProcessXml (string filename, XDocument doc, ReportDiagnostic reportDiagnostic)
		{
			try {
				XPathNavigator nav = doc.CreateNavigator ();
				return NodeBase.ProcessRootNodes (nav,  reportDiagnostic, filename);
			}
			// TODO: handle the correct exceptions correctly
			catch (ArgumentException) {
				// XML doesn't have a <linker> tag and is invalid. The document should have been validated before 
				return new List<IRootNode> ();
			}
		}

		public record AttributeNode : NodeBase
		{
			public string FullName;
			public string Internal;
			public string Assembly;
			public List<IAttributeArgumentNode> Arguments;
			public List<AttributePropertyNode> Properties;
			public List<AttributeFieldNode> Fields;

			public AttributeNode (XPathNavigator nav, ReportDiagnostic reportDiagnostic, string filename) : base (nav,  reportDiagnostic, filename)
			{
				var arguments = new List<IAttributeArgumentNode> ();
				foreach (XPathNavigator argNav in nav.SelectChildren (ArgumentElementName, "")) {
					if (ProcessArgument (argNav) is AttributeArgumentNode arg) {
						arguments.Add (arg);
					}
				}
				var properties = new List<AttributePropertyNode> ();
				foreach (XPathNavigator propertyNav in nav.SelectChildren (ArgumentElementName, "")) {
					var prop = new AttributePropertyNode (name: GetName (propertyNav), value: propertyNav.Value);
					properties.Add (prop);
				}
				var fields = new List<AttributeFieldNode> ();
				foreach (XPathNavigator fieldNav in nav.SelectChildren (ArgumentElementName, "")) {
					var field = new AttributeFieldNode (name: GetName (fieldNav), value: fieldNav.Value);
					fields.Add (field);
				}
				FullName = GetFullName (nav);
				Internal = GetAttribute (nav, "internal");
				Assembly = GetAttribute (nav, AssemblyElementName);
				Arguments = arguments;
				Properties = properties;
				Fields = fields;
				if (string.IsNullOrEmpty (FullName) && string.IsNullOrEmpty(Internal))
					reportDiagnostic (DiagnosticId.XmlElementDoesNotContainRequiredAttributeFullname, nav as IXmlLineInfo);

				if (string.IsNullOrEmpty (FullName) && !string.IsNullOrEmpty (Internal)) {
					if (Internal == "RemoveAttributeInstances" || Internal == "RemoveAttributeInstancesAttribute") {
						var parent = nav.Clone ();
						parent.MoveToParent ();
						if (parent.Name != TypeElementName)
							reportDiagnostic (DiagnosticId.XmlRemoveAttributeInstancesCanOnlyBeUsedOnType, nav as IXmlLineInfo);
					}
					else {
						reportDiagnostic (DiagnosticId.UnrecognizedInternalAttribute, nav as IXmlLineInfo, Internal);
					}
				}

				if (!string.IsNullOrEmpty (FullName) && !string.IsNullOrEmpty (Internal)) {
					// New diagnostic?
				};
				
				
				static IAttributeArgumentNode ProcessArgument (XPathNavigator argNav)
				{
					var type = argNav.GetAttribute ("type", "");
					if (type == "") type = "System.String";
					if (type == "System.Object") {
						foreach (XPathNavigator innerArgNav in argNav.SelectChildren (ArgumentElementName, "")) {
							if (ProcessArgument (innerArgNav) is AttributeArgumentNode innerArg) {
								return new AttributeArgumentBoxNode (innerArg);
							} else {
								// error
								throw new ArgumentException ("Arguments to attribute cannot be wrapped in more than one type");
							}
						}
						// No argument child in <argument type="System.Object>
						return new AttributeArgumentBoxNode (null);
					}
					var arg = new AttributeArgumentNode (type, argNav.Value);
					return arg;
				}
			}
		}

		public interface IAttributeArgumentNode { }

		public record AttributeArgumentBoxNode : IAttributeArgumentNode
		{
			public Type Type = typeof (System.Object);
			public AttributeArgumentNode? InnerArgument;

			public AttributeArgumentBoxNode (AttributeArgumentNode? innerArgument)
			{
				InnerArgument = innerArgument;
			}
		}

		public record AttributeArgumentNode : IAttributeArgumentNode
		{
			public string Type;
			public string Value;

			public AttributeArgumentNode (string type, string value)
			{
				Type = type;
				Value = value;
			}
		}

		public record AttributePropertyNode
		{
			public string Name;
			public string Value;

			public AttributePropertyNode (string name, string value)
			{
				Value = value;
				Name = name;
			}
		}

		public record AttributeFieldNode
		{
			public string Name;
			public string Value;

			public AttributeFieldNode (string name, string value)
			{
				Value = value;
				Name = name;
			}
		}

		public partial record TypeMemberNode : FeatureSwitchedNode
		{
			public string Name;
			public string Signature;

			public TypeMemberNode (XPathNavigator nav, ReportDiagnostic reportDiagnostic, string filename) : base (nav,  reportDiagnostic, filename)
			{
				Name = GetName (nav);
				Signature = GetSignature (nav);
			}
		}

		public partial record ParameterNode : AttributeTargetNode
		{
			public string Name;
			public ParameterNode (XPathNavigator nav, ReportDiagnostic reportDiagnostic, string filename) : base (nav,  reportDiagnostic, filename)
			{
				Name = GetName (nav);
			}
		}

		public partial record MethodNode : TypeMemberNode
		{
			public List<ParameterNode> Parameters;
			public List<AttributeNode> ReturnAttributes;
			public MethodNode (XPathNavigator nav, ReportDiagnostic reportDiagnostic, string filename) : base (nav,  reportDiagnostic, filename)
			{
				Parameters = new List<ParameterNode> ();
				ReturnAttributes = new List<AttributeNode> ();
				foreach (XPathNavigator parameterNav in nav.SelectChildren (ParameterElementName, "")) {
					Parameters.Add (new ParameterNode (parameterNav,  reportDiagnostic, filename));
				}
				foreach (XPathNavigator returnNav in nav.SelectChildren (ReturnElementName, "")) {
					foreach (XPathNavigator attributeNav in returnNav.SelectChildren (AttributeElementName, "")) {
						ReturnAttributes.Add (new AttributeNode (attributeNav,  reportDiagnostic, filename));
					}
				}
			}
		}

		public abstract record TypeBaseNode : FeatureSwitchedNode
		{
			public List<TypeMemberNode> Events;
			public List<TypeMemberNode> Fields;
			public List<TypeMemberNode> Properties;
			public List<MethodNode> Methods;
			public List<NestedTypeNode> Types;
			public TypeBaseNode (XPathNavigator nav, ReportDiagnostic reportDiagnostic, string filename) : base (nav,  reportDiagnostic, filename)
			{
				Methods = new List<MethodNode> ();
				Properties = new List<TypeMemberNode> ();
				Fields = new List<TypeMemberNode> ();
				Events = new List<TypeMemberNode> ();
				foreach (XPathNavigator methodNav in nav.SelectChildren (MethodElementName, "")) {
					Methods.Add (new MethodNode (methodNav,  reportDiagnostic, filename));
				}
				foreach (XPathNavigator propertyNav in nav.SelectChildren (PropertyElementName, "")) {
					Properties.Add (new TypeMemberNode (propertyNav,  reportDiagnostic, filename));
				}
				foreach (XPathNavigator eventNav in nav.SelectChildren (EventElementName, "")) {
					Events.Add (new TypeMemberNode (eventNav,  reportDiagnostic, filename));
				}
				foreach (XPathNavigator fieldNav in nav.SelectChildren (FieldElementName, "")) {
					Fields.Add (new TypeMemberNode (fieldNav,  reportDiagnostic, filename));
				}
				Types = new List<NestedTypeNode> ();
				foreach (XPathNavigator nestedTypeNav in nav.SelectChildren (TypeElementName, "")) {
					Types.Add (new NestedTypeNode (nestedTypeNav,  reportDiagnostic, filename));
				}
			}
		}
		public partial record NestedTypeNode : TypeBaseNode
		{
			public string Name;
			public NestedTypeNode (XPathNavigator nav, ReportDiagnostic reportDiagnostic, string filename) : base (nav,  reportDiagnostic, filename)
			{
				Name = GetName (nav);
			}
		}

		public partial record TypeNode : TypeBaseNode, IRootNode
		{
			public string FullName;
			public TypeNode (XPathNavigator nav, ReportDiagnostic reportDiagnostic, string filename) : base (nav,  reportDiagnostic, filename)
			{
				FullName = GetFullName (nav);
			}
		}

		public record AssemblyNode : FeatureSwitchedNode, IRootNode
		{
			public List<TypeNode> Types;
			public string FullName;
			public AssemblyNode (XPathNavigator nav, ReportDiagnostic reportDiagnostic, string filename) : base (nav,  reportDiagnostic, filename)
			{
				FullName = GetFullName (nav);
				Types = new List<TypeNode> ();
				foreach (XPathNavigator typeNav in nav.SelectChildren (TypeElementName, "")) {
					Types.Add (new TypeNode (typeNav,  reportDiagnostic, filename));
				}
			}
		}

		public interface IRootNode
		{
		}

		public abstract record NodeBase : XmlProcessorBase
		{
			public IXmlLineInfo? LineInfo;
			protected NodeBase (XPathNavigator nav, ReportDiagnostic reportDiagnostic, string filename)
			{
				LineInfo = (nav is IXmlLineInfo lineInfo) ? lineInfo : null;
			}

			public static List<IRootNode> ProcessRootNodes (XPathNavigator nav, ReportDiagnostic reportDiagnostic, string filename)
			{
				if (!nav.MoveToChild (LinkerElementName, XmlNamespace)) {
					throw new ArgumentException ($"XML does not have <{LinkerElementName}> base tag");
				}
				var roots = new List<IRootNode> ();
				foreach (XPathNavigator typeNav in nav.SelectChildren (TypeElementName, "")) {
					roots.Add (new TypeNode (typeNav,  reportDiagnostic, filename));
				}
				foreach (XPathNavigator assemblyNav in nav.SelectChildren (AssemblyElementName, "")) {
					roots.Add (new AssemblyNode (assemblyNav,  reportDiagnostic, filename));
				}
				return roots;
			}
		}

		public abstract record AttributeTargetNode : NodeBase
		{
			public List<AttributeNode> Attributes;

			public AttributeTargetNode (XPathNavigator nav, ReportDiagnostic reportDiagnostic, string filename) : base (nav,  reportDiagnostic, filename)
			{
				var attributes = new List<AttributeNode> ();
				foreach (XPathNavigator attributeNav in nav.SelectChildren (AttributeElementName, "")) {
					var attr = new AttributeNode (attributeNav,  reportDiagnostic, filename);
					if (attr == null)
						continue;
					attributes.Add (attr);
				}
				Attributes = attributes;
			}
		}

		public abstract record FeatureSwitchedNode : AttributeTargetNode
		{
			public FeatureSwitch? FeatureSwitch;

			public FeatureSwitchedNode (XPathNavigator nav, ReportDiagnostic reportDiagnostic, string filename) : base (nav,  reportDiagnostic, filename)
			{
				string feature, featurevalue, featuredefault;
				bool FeatureValue = false;
				bool FeatureDefault = true;
				bool valid = true;
				// TODO: new diagnosticId for when there is no feature but is featurevalue or featuredefault
				feature = GetAttribute (nav, "feature");
				if (string.IsNullOrEmpty (feature)) {
					FeatureSwitch = null;
					return;
				}

				featurevalue = GetAttribute (nav, "featurevalue");
				if (string.IsNullOrEmpty (featurevalue)) {
					reportDiagnostic (DiagnosticId.XmlFeatureDoesNotSpecifyFeatureValue, nav as IXmlLineInfo, filename, feature);
					valid = false;
				}
				else if (!bool.TryParse (featurevalue, out FeatureValue)) {
					reportDiagnostic (DiagnosticId.XmlUnsupportedNonBooleanValueForFeature, nav as IXmlLineInfo, filename, featurevalue);
					valid = false;
				}

				featuredefault = GetAttribute (nav, "featuredefault");
				if (string.IsNullOrEmpty (featuredefault))
					FeatureDefault = false;
				else if (!bool.TryParse (featurevalue, out FeatureValue)) {
					reportDiagnostic (DiagnosticId.XmlUnsupportedNonBooleanValueForFeature, nav as IXmlLineInfo, filename, featuredefault);
					valid = false;
				}
				else if (!FeatureDefault) {
					reportDiagnostic (DiagnosticId.XmlDocumentLocationHasInvalidFeatureDefault, nav as IXmlLineInfo, filename);
					valid = false;
				}

				if (valid)
					FeatureSwitch = new FeatureSwitch (feature, FeatureValue, FeatureDefault);
				else
					FeatureSwitch = null;
			}
		}

		public record FeatureSwitch
		{
			public string Feature;
			public bool FeatureValue;
			public bool FeatureDefault;
			public FeatureSwitch (string feature, bool featureValue, bool featureDefault)
			{
				Feature = feature;
				FeatureValue = featureValue;
				FeatureDefault = featureDefault;
			}
		}
	}
}
