using System;
using System.Linq.Expressions;
using Sample.CodeGen.Domains;
using Sample.CodeGen.Models;

namespace Sample.CodeGen.Models
{
    public static partial class StudentMapper
    {
        public static StudentDto AdaptToDto(this Student p1)
        {
            return p1 == null ? null : new StudentDto()
            {
                ID = p1.ID,
                LastName = p1.LastName,
                FirstMidName = p1.FirstMidName,
                EnrollmentDate = p1.EnrollmentDate
            };
        }
        public static StudentDto AdaptTo(this Student p2, StudentDto p3)
        {
            if (p2 == null)
            {
                return null;
            }
            StudentDto result = p3 ?? new StudentDto();
            
            result.ID = p2.ID;
            result.LastName = p2.LastName;
            result.FirstMidName = p2.FirstMidName;
            result.EnrollmentDate = p2.EnrollmentDate;
            return result;
            
        }
        public static Expression<Func<Student, StudentDto>> ProjectToDto => p4 => new StudentDto()
        {
            ID = p4.ID,
            LastName = p4.LastName,
            FirstMidName = p4.FirstMidName,
            EnrollmentDate = p4.EnrollmentDate
        };
        public static Student AdaptToStudent(this StudentAdd p5)
        {
            return p5 == null ? null : new Student()
            {
                ID = p5.ID,
                LastName = p5.LastName,
                FirstMidName = p5.FirstMidName,
                EnrollmentDate = p5.EnrollmentDate
            };
        }
        public static Student AdaptTo(this StudentUpdate p6, Student p7)
        {
            if (p6 == null)
            {
                return null;
            }
            Student result = p7 ?? new Student();
            
            result.LastName = p6.LastName;
            result.FirstMidName = p6.FirstMidName;
            result.EnrollmentDate = p6.EnrollmentDate;
            return result;
            
        }
        public static Student AdaptTo(this StudentMerge p8, Student p9)
        {
            if (p8 == null)
            {
                return null;
            }
            Student result = p9 ?? new Student();
            
            if (p8.LastName != null)
            {
                result.LastName = p8.LastName;
            }
            
            if (p8.FirstMidName != null)
            {
                result.FirstMidName = p8.FirstMidName;
            }
            
            if (p8.EnrollmentDate != null)
            {
                result.EnrollmentDate = (DateTime)p8.EnrollmentDate;
            }
            return result;
            
        }
    }
}