using Microsoft.EntityFrameworkCore;
using MANDADITOS_MORELOS.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;

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

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);
builder.Services.AddSingleton<JwtTokenService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]);
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

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
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors(AllowExpoApp);
app.UseWebSockets();
app.UseAuthorization();
app.MapControllers();
app.Run();
