using System;

namespace Sample.CodeGen.Models
{
    public partial record StudentMerge
    {
        public string? LastName { get; set; }
        public string FirstMidName { get; set; }
        public DateTime? EnrollmentDate { get; set; }
    }
}