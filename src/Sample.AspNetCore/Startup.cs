using System.Linq.Expressions;
using ExpressionDebugger;
using Hellang.Middleware.ProblemDetails;
using Mapster;
using Sample.AspNetCore.Controllers;
using Sample.AspNetCore.Models;
using MapsterMapper;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.AspNetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(opts => opts.EnableEndpointRouting = false)
                .AddNewtonsoftJson();
            services.AddDbContext<SchoolContext>(options => 
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
            services.AddSingleton(GetConfiguredMappingConfig());
            services.AddScoped<IMapper, ServiceMapper>();
            services.AddSingleton<NameFormatter>();
            services.AddProblemDetails();
            services.AddOData();
        }

        private static TypeAdapterConfig GetConfiguredMappingConfig()
        {
            var config = new TypeAdapterConfig
            {
                Compiler = exp => exp.CompileWithDebugInfo(new ExpressionCompilationOptions { EmitFile = true, ThrowOnFailedCompilation = true})
            };

            config.NewConfig<Enrollment, EnrollmentDto>()
                .AfterMappingAsync(async dto =>
                {
                    var context = MapContext.Current.GetService<SchoolContext>();
                    var course = await context.Courses.FindAsync(dto.CourseID);
                    if (course != null)
                        dto.CourseTitle = course.Title;
                    var student = await context.Students.FindAsync(dto.StudentID);
                    if (student != null)
                        dto.StudentName = MapContext.Current.GetService<NameFormatter>().Format(student.FirstMidName, student.LastName);
                });
            config.NewConfig<Student, StudentDto>()
                .Map(dest => dest.Name, src => MapContext.Current.GetService<NameFormatter>().Format(src.FirstMidName, src.LastName));
            config.NewConfig<Course, CourseDto>()
                .Map(dest => dest.CourseIDDto, src => src.CourseID)
                .Map(dest => dest.CreditsDto, src => src.Credits)
                .Map(dest => dest.TitleDto, src => src.Title)
                .Map(dest => dest.EnrollmentsDto, src => src.Enrollments);

            return config;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseProblemDetails();
            app.UseRouting();
            app.UseAuthorization();
            app.UseMvc(builder =>
            {
                builder.EnableDependencyInjection();
                builder.Select().Expand().Filter().OrderBy().MaxTop(1000).Count();
            });
        }
    }
}
