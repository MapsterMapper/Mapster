using System.Collections.Generic;
using Sample.CodeGen.Models;

namespace Sample.CodeGen.Models
{
    public partial record CourseUpdate
    {
        public string Title { get; set; }
        public int Credits { get; set; }
        public ICollection<EnrollmentUpdate> Enrollments { get; set; }
    }
}