using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Mapster.SourceGenerator;

namespace Sample.SourceGenerator.Models
{
    [AdaptTo("[name]Dto")]
    [AdaptFrom("[name]Add")]
    [AdaptFrom("[name]Update")]
    [AdaptFrom("[name]Merge")]
    public class Course
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CourseID { get; set; }
        public string Title { get; set; }
        public int Credits { get; set; }

        public ICollection<Enrollment> Enrollments { get; set; }
    }
}
