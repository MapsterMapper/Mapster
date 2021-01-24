using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Mapster.Utils;
using Sample.CodeGen.Domains;
using Sample.CodeGen.Models;

namespace Sample.CodeGen.Models
{
    public static partial class CourseMapper
    {
        public static CourseDto AdaptToDto(this Course p1)
        {
            return p1 == null ? null : new CourseDto()
            {
                CourseID = p1.CourseID,
                Title = p1.Title,
                Credits = p1.Credits,
                Enrollments = funcMain1(p1.Enrollments)
            };
        }
        public static CourseDto AdaptTo(this Course p3, CourseDto p4)
        {
            if (p3 == null)
            {
                return null;
            }
            CourseDto result = p4 ?? new CourseDto();
            
            result.CourseID = p3.CourseID;
            result.Title = p3.Title;
            result.Credits = p3.Credits;
            result.Enrollments = funcMain2(p3.Enrollments, result.Enrollments);
            return result;
            
        }
        public static Expression<Func<Course, CourseDto>> ProjectToDto => p7 => new CourseDto()
        {
            CourseID = p7.CourseID,
            Title = p7.Title,
            Credits = p7.Credits,
            Enrollments = p7.Enrollments.Select<Enrollment, EnrollmentDto>(p8 => new EnrollmentDto()
            {
                EnrollmentID = p8.EnrollmentID,
                CourseID = p8.CourseID,
                StudentID = p8.StudentID,
                Grade = p8.Grade == null ? null : Enum<Grade>.ToString((Grade)p8.Grade),
                Student = p8.Student == null ? null : new Person()
                {
                    ID = p8.Student.ID,
                    LastName = p8.Student.LastName,
                    FirstMidName = p8.Student.FirstMidName
                }
            }).ToList<EnrollmentDto>()
        };
        public static Course AdaptToCourse(this CourseAdd p9)
        {
            return p9 == null ? null : new Course()
            {
                CourseID = p9.CourseID,
                Title = p9.Title,
                Credits = p9.Credits,
                Enrollments = funcMain3(p9.Enrollments)
            };
        }
        public static Course AdaptTo(this CourseUpdate p11, Course p12)
        {
            if (p11 == null)
            {
                return null;
            }
            Course result = p12 ?? new Course();
            
            result.Title = p11.Title;
            result.Credits = p11.Credits;
            result.Enrollments = funcMain4(p11.Enrollments, result.Enrollments);
            return result;
            
        }
        public static Course AdaptTo(this CourseMerge p15, Course p16)
        {
            if (p15 == null)
            {
                return null;
            }
            Course result = p16 ?? new Course();
            
            if (p15.Title != null)
            {
                result.Title = p15.Title;
            }
            
            if (p15.Credits != null)
            {
                result.Credits = (int)p15.Credits;
            }
            
            if (p15.Enrollments != null)
            {
                result.Enrollments = funcMain5(p15.Enrollments, result.Enrollments);
            }
            return result;
            
        }
        
        private static ICollection<EnrollmentDto> funcMain1(ICollection<Enrollment> p2)
        {
            if (p2 == null)
            {
                return null;
            }
            ICollection<EnrollmentDto> result = new List<EnrollmentDto>(p2.Count);
            
            IEnumerator<Enrollment> enumerator = p2.GetEnumerator();
            
            while (enumerator.MoveNext())
            {
                Enrollment item = enumerator.Current;
                result.Add(item == null ? null : new EnrollmentDto()
                {
                    EnrollmentID = item.EnrollmentID,
                    CourseID = item.CourseID,
                    StudentID = item.StudentID,
                    Grade = item.Grade == null ? null : Enum<Grade>.ToString((Grade)item.Grade),
                    Student = item.Student == null ? null : new Person()
                    {
                        ID = item.Student.ID,
                        LastName = item.Student.LastName,
                        FirstMidName = item.Student.FirstMidName
                    }
                });
            }
            return result;
            
        }
        
        private static ICollection<EnrollmentDto> funcMain2(ICollection<Enrollment> p5, ICollection<EnrollmentDto> p6)
        {
            if (p5 == null)
            {
                return null;
            }
            ICollection<EnrollmentDto> result = new List<EnrollmentDto>(p5.Count);
            
            IEnumerator<Enrollment> enumerator = p5.GetEnumerator();
            
            while (enumerator.MoveNext())
            {
                Enrollment item = enumerator.Current;
                result.Add(item == null ? null : new EnrollmentDto()
                {
                    EnrollmentID = item.EnrollmentID,
                    CourseID = item.CourseID,
                    StudentID = item.StudentID,
                    Grade = item.Grade == null ? null : Enum<Grade>.ToString((Grade)item.Grade),
                    Student = item.Student == null ? null : new Person()
                    {
                        ID = item.Student.ID,
                        LastName = item.Student.LastName,
                        FirstMidName = item.Student.FirstMidName
                    }
                });
            }
            return result;
            
        }
        
        private static ICollection<Enrollment> funcMain3(ICollection<EnrollmentAdd> p10)
        {
            if (p10 == null)
            {
                return null;
            }
            ICollection<Enrollment> result = new List<Enrollment>(p10.Count);
            
            IEnumerator<EnrollmentAdd> enumerator = p10.GetEnumerator();
            
            while (enumerator.MoveNext())
            {
                EnrollmentAdd item = enumerator.Current;
                result.Add(item == null ? null : new Enrollment()
                {
                    EnrollmentID = item.EnrollmentID,
                    CourseID = item.CourseID,
                    StudentID = item.StudentID,
                    Grade = item.Grade == null ? null : (Grade?)Enum<Grade>.Parse(item.Grade)
                });
            }
            return result;
            
        }
        
        private static ICollection<Enrollment> funcMain4(ICollection<EnrollmentUpdate> p13, ICollection<Enrollment> p14)
        {
            if (p13 == null)
            {
                return null;
            }
            ICollection<Enrollment> result = new List<Enrollment>(p13.Count);
            
            IEnumerator<EnrollmentUpdate> enumerator = p13.GetEnumerator();
            
            while (enumerator.MoveNext())
            {
                EnrollmentUpdate item = enumerator.Current;
                result.Add(item == null ? null : new Enrollment()
                {
                    EnrollmentID = item.EnrollmentID,
                    CourseID = item.CourseID,
                    StudentID = item.StudentID,
                    Grade = item.Grade == null ? null : (Grade?)Enum<Grade>.Parse(item.Grade)
                });
            }
            return result;
            
        }
        
        private static ICollection<Enrollment> funcMain5(ICollection<EnrollmentMerge> p17, ICollection<Enrollment> p18)
        {
            if (p17 == null)
            {
                return null;
            }
            ICollection<Enrollment> result = new List<Enrollment>(p17.Count);
            
            IEnumerator<EnrollmentMerge> enumerator = p17.GetEnumerator();
            
            while (enumerator.MoveNext())
            {
                EnrollmentMerge item = enumerator.Current;
                result.Add(funcMain6(item));
            }
            return result;
            
        }
        
        private static Enrollment funcMain6(EnrollmentMerge p19)
        {
            if (p19 == null)
            {
                return null;
            }
            Enrollment result = new Enrollment();
            
            if (p19.EnrollmentID != null)
            {
                result.EnrollmentID = (int)p19.EnrollmentID;
            }
            
            if (p19.CourseID != null)
            {
                result.CourseID = (int)p19.CourseID;
            }
            
            if (p19.StudentID != null)
            {
                result.StudentID = (int)p19.StudentID;
            }
            
            if (p19.Grade != null)
            {
                result.Grade = (Grade?)Enum<Grade>.Parse(p19.Grade);
            }
            return result;
            
        }
    }
}