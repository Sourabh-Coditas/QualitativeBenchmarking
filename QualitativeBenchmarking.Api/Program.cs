using KPMG.QualitativeBenchmarking.Application.Abstraction;
using KPMG.QualitativeBenchmarking.Api.Middleware;
using KPMG.QualitativeBenchmarking.Api.Services;
using KPMG.QualitativeBenchmarking.Infrastructure;
using KPMG.QualitativeBenchmarking.Infrastructure.Configuration;

namespace KPMG.QualitativeBenchmarking.Api;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUserContext, HttpUserContext>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.Configure<DummyDataFileSettings>(o =>
        {
            var fromConfig = builder.Configuration["DummyData:FilePath"];
            o.FilePath = !string.IsNullOrWhiteSpace(fromConfig)
                ? fromConfig
                : Path.Combine(builder.Environment.ContentRootPath, "Data", "dummy-data.json");
        });

        var app = builder.Build();

        app.Services.UseDummyData();

        app.UseMiddleware<UserContextMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.MapControllers();

        app.Run();
    }
}
