using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sample.CodeGen.Domains
{
    public class Student
    {
        [Key]
        public int ID { get; set; }
        public string? LastName { get; set; }
        public string FirstMidName { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }
    }
}
