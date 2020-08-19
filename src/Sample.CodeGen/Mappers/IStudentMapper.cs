using System;
using System.Linq.Expressions;
using Mapster;
using Sample.CodeGen.Domains;
using Sample.CodeGen.Models;

namespace Sample.CodeGen.Mappers
{
    [Mapper]
    public interface IStudentMapper
    {
        Expression<Func<Student, Person>> StudentProjection { get; }
        Person Map(Student student);
        Person Map(Student student, Person person);
    }
}
