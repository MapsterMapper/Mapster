using System.Linq;
using System.Threading.Tasks;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sample.SourceGenerator.Models;

namespace Sample.SourceGenerator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SchoolController : ControllerBase
    {
        private readonly SchoolContext _context;
        public SchoolController(SchoolContext context)
        {
            _context = context;
        }

        //// OData Sample
        //[HttpGet("course")]
        //[EnableQuery]
        //public IQueryable<CourseDto> GetCourses()
        //{
        //    return _context.Courses.Select(c.ProjectToDto);
        //}

        // Add sample
        [HttpPost("student")]
        public async Task AddStudent([FromBody] StudentAdd data)
        {
            var student = data.MapToStudent();
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
        }

        // Update sample
        [HttpPut("student/{id}")]
        public async Task UpdateStudent([FromRoute] int id, [FromBody] StudentUpdate data)
        {
            var student = await _context.Students.FindAsync(id);
            if (student is null)
                throw new ProblemDetailsException(ProblemDetailsFactory.CreateProblemDetails(this.HttpContext, 404));
            student = data.MapToStudent();
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
        }

        // Update sample
        [HttpPatch("student/{id}")]
        public async Task MergeStudent([FromRoute] int id, [FromBody] StudentMerge data)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                throw new ProblemDetailsException(ProblemDetailsFactory.CreateProblemDetails(this.HttpContext, 404));
            student = data.MapToStudent();
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
        }
    }
}
