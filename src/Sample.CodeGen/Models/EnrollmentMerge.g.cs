namespace Sample.CodeGen.Models
{
    public partial record EnrollmentMerge
    {
        public int? EnrollmentID { get; set; }
        public int? CourseID { get; set; }
        public int? StudentID { get; set; }
        public string Grade { get; set; }
    }
}