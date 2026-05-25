using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Xml.Linq;

namespace Mapster.Tool
{
    internal static class XmlDocumentationReader
    {
        public static bool HasDocumentation(Assembly assembly, MemberInfo member)
        {
            var assemblyPath = assembly.Location;
            if (string.IsNullOrEmpty(assemblyPath))
                return false;

            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            if (!File.Exists(xmlPath))
                return false;

            var memberId = DocumentationCommentId.CreateMemberId(member);
            var element = XDocument.Load(xmlPath)
                .Root?
                .Element("members")?
                .Elements("member")
                .FirstOrDefault(it => (string?)it.Attribute("name") == memberId);

            return element != null &&
                   element.Elements().Any(it => !string.IsNullOrWhiteSpace(it.Value));
        }
    }
}
