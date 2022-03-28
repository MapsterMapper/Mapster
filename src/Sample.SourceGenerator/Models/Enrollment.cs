using Mapster.SourceGenerator;

namespace Sample.SourceGenerator.Models
{
    public enum Grade
    {
        A, B, C, D, F
    }

    [AdaptTo("[name]Dto")]
    [AdaptFrom("[name]Add")]
    [AdaptFrom("[name]Update")]
    [AdaptFrom("[name]Merge")]
    public class Enrollment
    {
        public int EnrollmentID { get; set; }
        public int CourseID { get; set; }
        public int StudentID { get; set; }
        public Grade? Grade { get; set; }
        public Course Course { get; set; }
        public Student Student { get; set; }
    }
}
