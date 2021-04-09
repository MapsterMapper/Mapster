using Sample.CodeGen.Models;

namespace Sample.CodeGen.Models
{
    public partial record EnrollmentDto
    {
        public int EnrollmentID { get; set; }
        public int CourseID { get; set; }
        public int StudentID { get; set; }
        public string Grade { get; set; }
        public Person Student { get; set; }
    }
}