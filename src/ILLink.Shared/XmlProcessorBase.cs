using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ILLink.Shared
{
    public abstract class XmlProcessorBase
    {
		protected const string FullNameAttributeName = "fullname";
		protected const string LinkerElementName = "linker";
		protected const string TypeElementName = "type";
		protected const string SignatureAttributeName = "signature";
		protected const string NameAttributeName = "name";
		protected const string FieldElementName = "field";
		protected const string MethodElementName = "method";
		protected const string EventElementName = "event";
		protected const string PropertyElementName = "property";
		protected const string AllAssembliesFullName = "*";
		protected const string XmlNamespace = "";

		protected readonly string _xmlDocumentLocation;
		protected readonly XPathNavigator _document;

		protected XmlProcessorBase (string xmlDocumentLocation, Stream documentStream)
		{
			_xmlDocumentLocation = xmlDocumentLocation;
			using (documentStream) {
				_document = XDocument.Load (documentStream, LoadOptions.SetLineInfo).CreateNavigator ();
			}
		}

		protected static string GetFullName (XPathNavigator nav)
		{
			return GetAttribute (nav, FullNameAttributeName);
		}

		protected static string GetName (XPathNavigator nav)
		{
			return GetAttribute (nav, NameAttributeName);
		}

		protected static string GetSignature (XPathNavigator nav)
		{
			return GetAttribute (nav, SignatureAttributeName);
		}

		protected static string GetAttribute (XPathNavigator nav, string attribute)
		{
			return nav.GetAttribute (attribute, XmlNamespace);
		}
	}
}
