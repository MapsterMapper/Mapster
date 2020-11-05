using Sample.CodeGen.Models;

namespace Sample.CodeGen.Models
{
    public static partial class PersonMapper
    {
        public static Person AdaptToPerson(this Person p1)
        {
            return p1 == null ? null : new Person()
            {
                ID = p1.ID,
                LastName = p1.LastName,
                FirstMidName = p1.FirstMidName
            };
        }
        public static Person AdaptTo(this Person p2, Person p3)
        {
            if (p2 == null)
            {
                return null;
            }
            Person result = p3 ?? new Person();
            
            result.ID = p2.ID;
            result.LastName = p2.LastName;
            result.FirstMidName = p2.FirstMidName;
            return result;
            
        }
    }
}