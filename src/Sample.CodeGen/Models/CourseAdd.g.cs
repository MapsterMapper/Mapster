using System.Collections.Generic;
using Sample.CodeGen.Models;

namespace Sample.CodeGen.Models
{
    public partial record CourseAdd
    {
        public int CourseID { get; set; }
        public string Title { get; set; }
        public int Credits { get; set; }
        public ICollection<EnrollmentAdd> Enrollments { get; set; }
    }
}