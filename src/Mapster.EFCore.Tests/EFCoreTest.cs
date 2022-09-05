using System;
using System.Collections.Generic;
using System.Linq;
using Mapster.EFCore.Tests.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.EFCore.Tests
{
    [TestClass]
    public class EFCoreTest
    {
        [TestMethod]
        public void TestFindObject()
        {
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;
            var context = new SchoolContext(options);
            DbInitializer.Initialize(context);

            var dto = new StudentDto
            {
                ID = 7,
                Enrollments = new List<EnrollmentItemDto>
                {
                    new EnrollmentItemDto
                    {
                        EnrollmentID = 12,
                        Grade = Grade.F,
                    }
                }
            };
            var poco = context.Students.Include(it => it.Enrollments)
                .First(it => it.ID == dto.ID);

            dto.BuildAdapter()
                .EntityFromContext(context)
                .AdaptTo(poco);

            var first = poco.Enrollments.First();
            first.CourseID.ShouldBe(3141);
            first.Grade.ShouldBe(Grade.F);
        }

        [TestMethod]
        public void MapperInstance_From_OrderBy()
        {
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;
            var context = new SchoolContext(options);
            DbInitializer.Initialize(context);

            var mapsterInstance = new Mapper();

            var query = context.Students.Include(it => it.Enrollments);
            var orderedQuery = mapsterInstance.From(query)
                .ProjectToType<StudentDto>()
                .OrderBy(s => s.LastName);

            var first = orderedQuery.First();
            first.LastName.ShouldBe("Alexander");

            var last = orderedQuery.Last();
            last.LastName.ShouldBe("Olivetto");
        }
    }

    public class StudentDto
    {
        public int ID { get; set; }
        public string LastName { get; set; }
        public ICollection<EnrollmentItemDto> Enrollments { get; set; }
    }

    public class EnrollmentItemDto
    {
        public int EnrollmentID { get; set; }
        public Grade? Grade { get; set; }
    }
}
