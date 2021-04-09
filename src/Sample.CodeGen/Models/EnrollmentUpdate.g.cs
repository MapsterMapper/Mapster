namespace Sample.CodeGen.Models
{
    public partial record EnrollmentUpdate
    {
        public int EnrollmentID { get; set; }
        public int CourseID { get; set; }
        public int StudentID { get; set; }
        public string Grade { get; set; }
    }
}