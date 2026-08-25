using System;
using System.Text;
using static ExpressionDebugger.Helpers.RandomNamespaceGenerator;

namespace ExpressionDebugger.Helpers.GeneratedAttributes
{
    public class MapsterToolGeneratedMapperAttribute : GeneratedBase, IGeneratedAttribute
    {
        private readonly StringBuilder _Declaration;
        private readonly string _NameSpace;

        public string NameSpace => _NameSpace;

        public string Declaration => _Declaration.ToString();

        public string Implimentation => "[MapsterToolGeneratedMapper]";

        public string FileName => "MapsterToolGeneratedMapperAttribute";

       public MapsterToolGeneratedMapperAttribute(string extendedNameSpace)
       {
            if (String.IsNullOrEmpty(extendedNameSpace))
                throw new ArgumentNullException("Extended namespace not specified or is null/empty string");
            
            if(CheckNameSpace.IsMatch(extendedNameSpace))
                _NameSpace = $"Mapster.Generated.Attributes.{extendedNameSpace}";
            else
                _NameSpace = $"Mapster.Generated.Attributes.{Generate(extendedNameSpace,1,1)}";

            _Declaration = new StringBuilder();

            _Declaration.Append("using System;\r\n\r\n");
            _Declaration.Append($"namespace {NameSpace}");
            _Declaration.Append("\r\n{\r\n    public sealed class MapsterToolGeneratedMapperAttribute : Attribute\r\n    {\r\n\r\n    }\r\n} ");
        }

    }
}
