using Microsoft.EntityFrameworkCore;
using MANDADITOS_MORELOS.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Configuration.AddUserSecrets<Program>();

var AllowExpoApp = "_AllowSpecificOriginsExpoApp";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: AllowExpoApp,
        policy =>
        {
            //policy.WithOrigins("exp://localhost:8081")
            policy.AllowAnyOrigin()
                       .AllowAnyHeader()
                       .AllowAnyMethod();
        });
});

builder.Services.AddControllers();


builder.Services.AddDbContext<MorelosContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("connection_to_mysql");
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)));
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(AllowExpoApp);

app.UseAuthorization();

app.MapControllers();

app.Run();
