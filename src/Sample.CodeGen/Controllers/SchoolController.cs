using System.Linq;
using System.Threading.Tasks;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sample.CodeGen.Domains;
using Sample.CodeGen.Mappers;
using Sample.CodeGen.Models;

namespace Sample.CodeGen.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SchoolController : ControllerBase
    {
        private readonly SchoolContext _context;
        private readonly IStudentMapper _studentMapper;
        public SchoolController(SchoolContext context, IStudentMapper studentMapper)
        {
            _context = context;
            _studentMapper = studentMapper;
        }

        // OData Sample
        [HttpGet("course")]
        [EnableQuery]
        public IQueryable<CourseDto> GetCourses()
        {
            return _context.Courses.Select(CourseMapper.ProjectToDto);
        }


        // OData Sample
        [HttpGet("student")]
        [EnableQuery]
        public IQueryable<Person> GetStudents()
        {
            return _context.Students.Select(_studentMapper.StudentProjection);
        }

        // Add sample
        [HttpPost("student")]
        public async Task AddStudent([FromBody] StudentAdd data)
        {
            var student = data.AdaptToStudent();
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
        }

        // Update sample
        [HttpPut("student/{id}")]
        public async Task UpdateStudent([FromRoute] int id, [FromBody] StudentUpdate data)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(it => it.ID == id);
            if (student == null)
                throw new ProblemDetailsException(ProblemDetailsFactory.CreateProblemDetails(this.HttpContext, 404));
            data.AdaptTo(student);
            await _context.SaveChangesAsync();
        }

        // Update sample
        [HttpPatch("student/{id}")]
        public async Task MergeStudent([FromRoute] int id, [FromBody] StudentMerge data)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(it => it.ID == id);
            if (student == null)
                throw new ProblemDetailsException(ProblemDetailsFactory.CreateProblemDetails(this.HttpContext, 404));
            data.AdaptTo(student);
            await _context.SaveChangesAsync();
        }
    }
}
