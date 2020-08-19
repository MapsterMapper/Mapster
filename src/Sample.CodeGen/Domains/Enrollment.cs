using Mapster;
using Newtonsoft.Json;
using Sample.CodeGen.Attributes;

namespace Sample.CodeGen.Domains
{
    public enum Grade
    {
        A, B, C, D, F
    }

    [GenerateDto, GenerateAdd, GenerateUpdate, GenerateMerge]
    public class Enrollment
    {
        public int EnrollmentID { get; set; }
        public int CourseID { get; set; }
        public int StudentID { get; set; }

        [PropertyType(typeof(string))]
        public Grade? Grade { get; set; }

        [JsonIgnore]
        public Course Course { get; set; }

        [NoModify]
        public Student Student { get; set; }
    }
}
