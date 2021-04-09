using System;

namespace Sample.CodeGen.Models
{
    public partial record StudentDto
    {
        public int ID { get; set; }
        public string? LastName { get; set; }
        public string FirstMidName { get; set; }
        public DateTime EnrollmentDate { get; set; }
    }
}