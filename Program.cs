using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using JoineryServer.Data;
using JoineryServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add Entity Framework
builder.Services.AddDbContext<JoineryDbContext>(options =>
    options.UseInMemoryDatabase("JoineryDatabase"));

builder.Services.AddControllers();

// Add HTTP client for Git repository service
builder.Services.AddHttpClient();

// Add Git repository service
builder.Services.AddScoped<IGitRepositoryService, GitRepositoryService>();

// Add team permission service
builder.Services.AddScoped<ITeamPermissionService, TeamPermissionService>();

// Add AWS IAM service
builder.Services.AddScoped<IAwsIamService, AwsIamService>();

// Configure authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtConfig = builder.Configuration.GetSection("JWT");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig["Issuer"],
            ValidAudience = jwtConfig["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["SecretKey"] ?? "default-secret-key"))
        };
    })
    .AddGitHub(options =>
    {
        var githubConfig = builder.Configuration.GetSection("Authentication:GitHub");
        options.ClientId = githubConfig["ClientId"] ?? "";
        options.ClientSecret = githubConfig["ClientSecret"] ?? "";
        options.CallbackPath = "/signin-github";
        options.Scope.Add("user:email");
    })
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("Authentication:Microsoft"));

// Configure authorization
builder.Services.AddAuthorization();

// Configure Swagger/OpenAPI with security definitions
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Joinery Server API",
        Version = "v1",
        Description = "A minimal MVP API for sharing database queries with authentication"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// Seed database in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<JoineryDbContext>();
    await context.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Joinery Server API V1");
        c.RoutePrefix = "swagger"; // Set Swagger UI at /swagger endpoint
    });
    app.UseCors("AllowAll");
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }
