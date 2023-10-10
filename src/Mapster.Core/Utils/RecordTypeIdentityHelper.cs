using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mapster.Utils
{
    /// <summary>
    /// CheckTools from Distinctive features of RecordType according to specification:
    /// https://github.com/dotnet/docs/blob/main/docs/csharp/language-reference/builtin-types/record.md
    /// </summary>
    public static class RecordTypeIdentityHelper
    {
        private static bool IsRecordСonstructor(Type type)
        {
            var ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).ToList();

            var isRecordTypeCtor = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.IsFamily == true || (type.IsSealed && x.IsPrivate == true)) // add target from Sealed record
                .Any(x => x.GetParameters()
                         .Any(y => y.ParameterType == type));


            if (ctors.Count >= 2 && isRecordTypeCtor) 
                return true;


            return false;

        }

        private static bool IsIncludedRecordCloneMethod(Type type)
        {
           if( type.GetMethod("<Clone>$")?.MethodImplementationFlags.HasFlag(MethodImplAttributes.IL) == true)
                return true;

            return false;
        }

        public static bool IsRecordType(Type type)
        {
            if (IsRecordСonstructor(type) && IsIncludedRecordCloneMethod(type))
                return true;


            return false;
        }

    }
}
