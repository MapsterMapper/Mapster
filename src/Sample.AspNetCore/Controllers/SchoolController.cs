using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hellang.Middleware.ProblemDetails;
using Mapster;
using Sample.AspNetCore.Models;
using MapsterMapper;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Sample.AspNetCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SchoolController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly SchoolContext _context;
        public SchoolController(IMapper mapper, SchoolContext context)
        {
            _mapper = mapper;
            _context = context;
        }

        // OData Sample
        [HttpGet("course")]
        [EnableQuery]
        public IQueryable<CourseDto> GetCourses()
        {
            return _mapper.From(_context.Courses).ProjectToType<CourseDto>();
        }

        // Async Sample
        [HttpGet("enrollment/{id}")]
        public async Task<EnrollmentDto> GetEnrollment([FromRoute] int id)
        {
            var enroll = await _context.Enrollments.FindAsync(id);
            return await _mapper.From(enroll)
                .AdaptToTypeAsync<EnrollmentDto>();
        }

        [HttpGet("student/{id}")]
        public Task<StudentDto> GetStudent([FromRoute] int id)
        {
            var query = _context.Students.Where(it => it.ID == id);
            return _mapper.From(query)
                .ProjectToType<StudentDto>()
                .FirstOrDefaultAsync();
        }

        // EF Core update sample
        [HttpPut("student/{id}")]
        public async Task UpdateStudent([FromRoute] int id, [FromBody] StudentDto data)
        {
            var student = await _context.Students.Include(it => it.Enrollments)
                .FirstOrDefaultAsync(it => it.ID == id);
            if (student == null)
                throw new ProblemDetailsException(ProblemDetailsFactory.CreateProblemDetails(this.HttpContext, 404));
            _mapper.From(data)
                .EntityFromContext(_context)
                .AdaptTo(student);
            await _context.SaveChangesAsync();
        }
    }

    public class EnrollmentDto
    {
        public int EnrollmentID { get; set; }
        public int CourseID { get; set; }
        public int StudentID { get; set; }
        public Grade? Grade { get; set; }
        public string CourseTitle { get; set; }
        public string StudentName { get; set; }
    }

    public class StudentDto
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public ICollection<EnrollmentItemDto> Enrollments { get; set; }
    }

    public class EnrollmentItemDto
    {
        public int EnrollmentID { get; set; }
        //public Grade? Grade { get; set; }
    }

    public class CourseDto
    {
        public int CourseIDDto { get; set; }
        public string TitleDto { get; set; }
        public int CreditsDto { get; set; }
        public ICollection<EnrollmentItemDto> EnrollmentsDto { get; set; }
    }
}
