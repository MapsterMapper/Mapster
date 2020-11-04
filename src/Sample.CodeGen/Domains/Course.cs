using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Mapster;
using Sample.CodeGen.Attributes;

namespace Sample.CodeGen.Domains
{
    [GenerateDto, GenerateAdd, GenerateUpdate, GenerateMerge, GenerateMapper]
    public class Course
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CourseID { get; set; }
        public string Title { get; set; }
        public int Credits { get; set; }

        public ICollection<Enrollment> Enrollments { get; set; }
    }
}
