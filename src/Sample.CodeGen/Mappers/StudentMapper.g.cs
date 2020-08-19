using System;
using System.Linq.Expressions;
using Sample.CodeGen.Domains;
using Sample.CodeGen.Mappers;
using Sample.CodeGen.Models;

namespace Sample.CodeGen.Mappers
{
    public partial class StudentMapper : IStudentMapper
    {
        public Expression<Func<Student, Person>> StudentProjection => p1 => new Person()
        {
            ID = p1.ID,
            LastName = p1.LastName,
            FirstMidName = p1.FirstMidName
        };
        public Person Map(Student p2)
        {
            return p2 == null ? null : new Person()
            {
                ID = p2.ID,
                LastName = p2.LastName,
                FirstMidName = p2.FirstMidName
            };
        }
        public Person Map(Student p3, Person p4)
        {
            if (p3 == null)
            {
                return null;
            }
            Person result = p4 ?? new Person();
            
            result.ID = p3.ID;
            result.LastName = p3.LastName;
            result.FirstMidName = p3.FirstMidName;
            return result;
            
        }
    }
}