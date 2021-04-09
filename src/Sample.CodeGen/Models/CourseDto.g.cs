using System.Collections.Generic;
using Sample.CodeGen.Models;

namespace Sample.CodeGen.Models
{
    public partial record CourseDto
    {
        public int CourseID { get; set; }
        public string Title { get; set; }
        public int Credits { get; set; }
        public ICollection<EnrollmentDto> Enrollments { get; set; }
    }
}