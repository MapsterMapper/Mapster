#if NET6_0
using Hellang.Middleware.ProblemDetails;
#endif
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.CodeGen.Domains;

namespace Sample.CodeGen
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(opts => opts.EnableEndpointRouting = false)
                .AddOData(options => options.Select().Filter().OrderBy())
                .AddNewtonsoftJson();
            services.AddDbContext<SchoolContext>(options => 
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
            
            services.AddProblemDetails();
            services.Scan(selector => selector.FromCallingAssembly()
                .AddClasses().AsMatchingInterface().WithSingletonLifetime());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            #if NET6_0
            app.UseProblemDetails();
            #endif
            app.UseRouting();
            app.UseAuthorization();
            app.UseMvc();
        }
    }
}
