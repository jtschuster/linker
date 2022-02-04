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
	public partial class LinkAttributes<TCustomAttribute, TCustomAttributeProvider>
	{
		public static ImmutableArray<DiagnosticId> SupportedDiagnosticIds = ImmutableArray.Create (
			DiagnosticId.XmlFeatureDoesNotSpecifyFeatureValue,
			DiagnosticId.XmlRemoveAttributeInstancesCanOnlyBeUsedOnType,
			DiagnosticId.XmlElementDoesNotContainRequiredAttributeFullname,
			DiagnosticId.XmlElementDoesNotContainRequiredAttributeFullname,
			DiagnosticId.XmlUnsupportedNonBooleanValueForFeature,
			DiagnosticId.XmlUnsupportedNonBooleanValueForFeature,
			DiagnosticId.XmlDocumentLocationHasInvalidFeatureDefault
			);

		public List<ITopLevelNode>? TopLevelNodes { get; }

		public Dictionary<TCustomAttributeProvider, HashSet<TCustomAttribute>>? InjectedAttributes { get; }

		private LinkAttributes (Dictionary<TCustomAttributeProvider, HashSet<TCustomAttribute>> injectedAttributes, List<ITopLevelNode> topLevelNodes)
		{
			TopLevelNodes = topLevelNodes;
			InjectedAttributes = injectedAttributes;
		}

		public static LinkAttributes<TCustomAttribute, TCustomAttributeProvider>? ProcessXml (
			XDocument doc,
			string filename,
			Action<DiagnosticId, IXmlLineInfo?, string[]> reportDiagnostic,
			// How do I properly report the different kinds of errors in resolution.
			Func<AttributeTargetNode, TCustomAttributeProvider?> resolveAttributeProvider,
			Func<AttributeNode, TCustomAttribute?> resolveAttribute)
		{
			var injectedAttributes = new Dictionary<TCustomAttributeProvider, HashSet<TCustomAttribute>> ();
			var context = new LinkAttributesContext (filename, reportDiagnostic, resolveAttributeProvider, resolveAttribute, injectedAttributes);
			try {
				XPathNavigator nav = doc.CreateNavigator ();
				var topLevelNodes = NodeBase.ProcessRootNodes (nav, context);
				return new LinkAttributes<TCustomAttribute, TCustomAttributeProvider> (context.InjectedAttributes, topLevelNodes);
			}
			// TODO: handle the correct exceptions correctly
			catch (ArgumentException) {
				// XML doesn't have a <linker> tag and is invalid. The document should have been validated before 
				return null;
			}
		}

		internal record LinkAttributesContext
		{
			public readonly string FileName;
			public readonly Action<DiagnosticId, IXmlLineInfo?, string[]> ReportDiagnostic;
			public readonly Func<AttributeTargetNode, TCustomAttributeProvider?> ResolveProvider;
			public readonly Func<AttributeNode, TCustomAttribute?> ResolveAttribute;
			public readonly Dictionary<TCustomAttributeProvider, HashSet<TCustomAttribute>> InjectedAttributes;
			public LinkAttributesContext (
				string fileName,
				Action<DiagnosticId, IXmlLineInfo?, string[]> reportDiagnostic,
				Func<AttributeTargetNode, TCustomAttributeProvider?> resolveProvider,
				Func<AttributeNode, TCustomAttribute?> resolveAttribute,
				Dictionary<TCustomAttributeProvider, HashSet<TCustomAttribute>> injectedAttributes)
			{
				FileName = fileName;
				ReportDiagnostic = reportDiagnostic;
				ResolveProvider = resolveProvider;
				ResolveAttribute = resolveAttribute;
				InjectedAttributes = injectedAttributes;
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
			public TCustomAttribute? ResolvedAttribute;

			internal AttributeNode (XPathNavigator nav, LinkAttributesContext context) : base (nav, context)
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
				if (string.IsNullOrEmpty (FullName) && string.IsNullOrEmpty (Internal))
					context.ReportDiagnostic (DiagnosticId.XmlElementDoesNotContainRequiredAttributeFullname, nav as IXmlLineInfo, Array.Empty<string> ());

				if (string.IsNullOrEmpty (FullName) && !string.IsNullOrEmpty (Internal)) {
					if (Internal == "RemoveAttributeInstances" || Internal == "RemoveAttributeInstancesAttribute") {
						var parent = nav.Clone ();
						parent.MoveToParent ();
						if (parent.Name != TypeElementName)
							context.ReportDiagnostic (DiagnosticId.XmlRemoveAttributeInstancesCanOnlyBeUsedOnType, nav as IXmlLineInfo, Array.Empty<string> ());
					} else {
						context.ReportDiagnostic (DiagnosticId.UnrecognizedInternalAttribute, nav as IXmlLineInfo, new string[] { Internal });
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

			void ResolveAttribute(LinkAttributesContext context)
			{
				ResolvedAttribute = context.ResolveAttribute (this);
			}

			internal static AttributeNode Create(XPathNavigator nav, LinkAttributesContext context)
			{
				var node = new AttributeNode (nav, context);
				node.ResolveAttribute (context);
				return node;
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

			internal TypeMemberNode (XPathNavigator nav, LinkAttributesContext context) : base (nav, context)
			{
				Name = GetName (nav);
				Signature = GetSignature (nav);
			}

			internal static TypeMemberNode Create(XPathNavigator nav, LinkAttributesContext context)
			{
				var node = new TypeMemberNode (nav, context);
				node.Resolve (context);
				return node;
			}
		}

		public partial record ParameterNode : AttributeTargetNode
		{
			public string Name;
			internal ParameterNode (XPathNavigator nav, LinkAttributesContext context) : base (nav, context)
			{
				Name = GetName (nav);
			}

			internal static ParameterNode Create(XPathNavigator nav, LinkAttributesContext context)
			{
				var node = new ParameterNode (nav, context);
				node.Resolve (context);
				return node;
			}
		}

		public partial record MethodNode : TypeMemberNode
		{
			public List<ParameterNode> Parameters;
			public List<AttributeNode> ReturnAttributes;
			internal MethodNode (XPathNavigator nav, LinkAttributesContext context) : base (nav, context)
			{
				Parameters = new List<ParameterNode> ();
				ReturnAttributes = new List<AttributeNode> ();
				foreach (XPathNavigator parameterNav in nav.SelectChildren (ParameterElementName, "")) {
					Parameters.Add (ParameterNode.Create (parameterNav, context));
				}
				foreach (XPathNavigator returnNav in nav.SelectChildren (ReturnElementName, "")) {
					foreach (XPathNavigator attributeNav in returnNav.SelectChildren (AttributeElementName, "")) {
						ReturnAttributes.Add (AttributeNode.Create (attributeNav, context));
					}
				}
			}

			internal static MethodNode Create(XPathNavigator nav, LinkAttributesContext context)
			{
				var node = new MethodNode (nav, context);
				node.Resolve (context);
				return node;
			}
		}

		public abstract record TypeBaseNode : FeatureSwitchedNode
		{
			public List<TypeMemberNode> Events;
			public List<TypeMemberNode> Fields;
			public List<TypeMemberNode> Properties;
			public List<MethodNode> Methods;
			public List<NestedTypeNode> Types;
			internal TypeBaseNode (XPathNavigator nav, LinkAttributesContext context) : base (nav, context)
			{
				Methods = new List<MethodNode> ();
				Properties = new List<TypeMemberNode> ();
				Fields = new List<TypeMemberNode> ();
				Events = new List<TypeMemberNode> ();
				foreach (XPathNavigator methodNav in nav.SelectChildren (MethodElementName, "")) {
					Methods.Add (MethodNode.Create (methodNav, context));
				}
				foreach (XPathNavigator propertyNav in nav.SelectChildren (PropertyElementName, "")) {
					Properties.Add (TypeMemberNode.Create (propertyNav, context));
				}
				foreach (XPathNavigator eventNav in nav.SelectChildren (EventElementName, "")) {
					Events.Add (TypeMemberNode.Create (eventNav, context));
				}
				foreach (XPathNavigator fieldNav in nav.SelectChildren (FieldElementName, "")) {
					Fields.Add (TypeMemberNode.Create (fieldNav, context));
				}
				Types = new List<NestedTypeNode> ();
				foreach (XPathNavigator nestedTypeNav in nav.SelectChildren (TypeElementName, "")) {
					Types.Add (NestedTypeNode.Create (nestedTypeNav, context));
				}
			}
		}
		public partial record NestedTypeNode : TypeBaseNode
		{
			public string Name;
			internal NestedTypeNode (XPathNavigator nav, LinkAttributesContext context) : base (nav, context)
			{
				Name = GetName (nav);
			}
			
			internal static NestedTypeNode Create(XPathNavigator nav, LinkAttributesContext context)
			{
				var node = new NestedTypeNode (nav, context);
				node.Resolve (context);
				return node;
			}
		}

		public partial record TypeNode : TypeBaseNode, ITopLevelNode
		{
			public string FullName;
			internal TypeNode (XPathNavigator nav, LinkAttributesContext context) : base (nav, context)
			{
				FullName = GetFullName (nav);
			}

			internal static TypeNode Create(XPathNavigator nav, LinkAttributesContext context)
			{
				var node = new TypeNode (nav, context);
				node.Resolve (context);
				return node;
			}
		}

		public record AssemblyNode : FeatureSwitchedNode, ITopLevelNode
		{
			public List<TypeNode> Types;
			public string FullName;
			internal AssemblyNode (XPathNavigator nav, LinkAttributesContext context) : base (nav, context)
			{
				FullName = GetFullName (nav);
				Types = new List<TypeNode> ();
				foreach (XPathNavigator typeNav in nav.SelectChildren (TypeElementName, "")) {
					Types.Add (TypeNode.Create (typeNav, context));
				}
			}
			internal static AssemblyNode Create(XPathNavigator nav, LinkAttributesContext context)
			{
				var node = new AssemblyNode (nav, context);
				node.Resolve (context);
				return node;
			}
		}

		public interface ITopLevelNode
		{
		}

		public abstract record NodeBase : XmlProcessorBase
		{
			public IXmlLineInfo? LineInfo;
			internal NodeBase (XPathNavigator nav, LinkAttributesContext context)
			{
				LineInfo = (nav is IXmlLineInfo lineInfo) ? lineInfo : null;
			}

			internal static List<ITopLevelNode> ProcessRootNodes (XPathNavigator nav, LinkAttributesContext context)
			{
				if (!nav.MoveToChild (LinkerElementName, XmlNamespace)) {
					context.ReportDiagnostic (DiagnosticId.XmlException, nav as IXmlLineInfo, new string[] { context.FileName, $"XML does not have <{LinkerElementName}> base tag" });
					return new List<ITopLevelNode> ();
				}
				var roots = new List<ITopLevelNode> ();
				foreach (XPathNavigator typeNav in nav.SelectChildren (TypeElementName, "")) {
					roots.Add (TypeNode.Create (typeNav, context));
				}
				foreach (XPathNavigator assemblyNav in nav.SelectChildren (AssemblyElementName, "")) {
					roots.Add (AssemblyNode.Create (assemblyNav, context));
				}
				return roots;
			}
		}

		public abstract record AttributeTargetNode : NodeBase
		{
			public List<AttributeNode> Attributes;
			public TCustomAttributeProvider? ResolvedProvider;
			internal void Resolve (LinkAttributesContext context)
			{
				ResolvedProvider = context.ResolveProvider (this);
				if (ResolvedProvider == null)
					return;
				foreach (var attributeNode in Attributes) {
					var resolvedAttribute = attributeNode.ResolvedAttribute;
					if (resolvedAttribute is null)
						return;
					if (context.InjectedAttributes.TryGetValue(ResolvedProvider, out var set)) {
						set.Add (resolvedAttribute);
					} else {
						var newSet = new HashSet<TCustomAttribute> ();
						newSet.Add (resolvedAttribute);
						context.InjectedAttributes.Add(ResolvedProvider, newSet);
					}
				}
			}

			internal AttributeTargetNode (XPathNavigator nav, LinkAttributesContext context) : base (nav, context)
			{
				var attributes = new List<AttributeNode> ();
				foreach (XPathNavigator attributeNav in nav.SelectChildren (AttributeElementName, "")) {
					var attr = AttributeNode.Create (attributeNav, context);
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

			internal FeatureSwitchedNode (XPathNavigator nav, LinkAttributesContext context) : base (nav, context)
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
					context.ReportDiagnostic (DiagnosticId.XmlFeatureDoesNotSpecifyFeatureValue, nav as IXmlLineInfo, new string[] { context.FileName, feature });
					valid = false;
				} else if (!bool.TryParse (featurevalue, out FeatureValue)) {
					context.ReportDiagnostic (DiagnosticId.XmlUnsupportedNonBooleanValueForFeature, nav as IXmlLineInfo, new string[] { context.FileName, featurevalue });
					valid = false;
				}

				featuredefault = GetAttribute (nav, "featuredefault");
				if (string.IsNullOrEmpty (featuredefault))
					FeatureDefault = false;
				else if (!bool.TryParse (featurevalue, out FeatureValue)) {
					context.ReportDiagnostic (DiagnosticId.XmlUnsupportedNonBooleanValueForFeature, nav as IXmlLineInfo, new string[] { context.FileName, featuredefault });
					valid = false;
				} else if (!FeatureDefault) {
					context.ReportDiagnostic (DiagnosticId.XmlDocumentLocationHasInvalidFeatureDefault, nav as IXmlLineInfo, new string[] { context.FileName });
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

