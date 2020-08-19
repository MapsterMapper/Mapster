using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Mapster;
using Newtonsoft.Json;
using Sample.CodeGen.Attributes;
using Sample.CodeGen.Models;

namespace Sample.CodeGen.Domains
{
    [GenerateDto, GenerateAdd, GenerateUpdate, GenerateMerge, GenerateMapper, DtoPropertyType(typeof(Person))]
    public class Student
    {
        [Key]
        public int ID { get; set; }
        public string LastName { get; set; }
        public string FirstMidName { get; set; }
        public DateTime EnrollmentDate { get; set; }

        [JsonIgnore]
        public ICollection<Enrollment> Enrollments { get; set; }
    }
}
