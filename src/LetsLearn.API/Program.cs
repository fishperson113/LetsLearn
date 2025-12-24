using LetsLearn.API.Middleware;
using LetsLearn.Core.Interfaces;
using LetsLearn.Infrastructure.Data;
using LetsLearn.Infrastructure.Redis;
using LetsLearn.Infrastructure.Repository;
using LetsLearn.Infrastructure.UnitOfWork;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using LetsLearn.UseCases.Services;
using LetsLearn.UseCases.Services.AssignmentResponseService;
using LetsLearn.UseCases.Services.Auth;
using LetsLearn.UseCases.Services.CommentService;
using LetsLearn.UseCases.Services.ConversationService;
using LetsLearn.UseCases.Services.CourseClone;
using LetsLearn.UseCases.Services.CourseSer;
using LetsLearn.UseCases.Services.MessageService;
using LetsLearn.UseCases.Services.QuestionSer;
using LetsLearn.UseCases.Services.QuizResponseService;
using LetsLearn.UseCases.Services.SectionSer;
using LetsLearn.UseCases.Services.UserSer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "LetsLearn.API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, 
        Scheme = "bearer",                                 
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter Access Token here"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<LetsLearnContext>(options =>
        options.UseNpgsql(connectionString));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "LetsLearn";
});
builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

// DI for custom repositories
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IAssignmentResponseRepository, AssignmentResponseRepository>();
builder.Services.AddScoped<IQuizResponseRepository, QuizResponseRepository>();
builder.Services.AddScoped<IQuizResponseAnswerRepository, QuizResponseAnswerRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

//DI for custom services
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IAssignmentResponseService, AssignmentResponseService>();
builder.Services.AddScoped<IQuizResponseService, QuizResponseService>();
builder.Services.AddScoped<ISectionService, SectionService>();
builder.Services.AddScoped<ITopicService, TopicService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICourseCloneService, CourseCloneService>();

builder.Services.AddSingleton<CourseFactory>();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "ManualJwt";
    options.DefaultChallengeScheme = "ManualJwt";
    options.DefaultForbidScheme = "ManualJwt";
})
.AddScheme<AuthenticationSchemeOptions, JwtAuthHandler>("ManualJwt", null);

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder => builder
            .WithOrigins("http://localhost:4200") // FE port
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(origin => true)
            .AllowCredentials());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
//Only uncomment this if you want to auto apply migrations in dev environment
await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<LetsLearnContext>();
    dbContext.Database.EnsureCreated();

    // Seed admin user
    var authService = services.GetRequiredService<IAuthService>();
    var userRepo = services.GetRequiredService<IUserRepository>();
    
    var adminEmail = "admin@letslearn.com";
    var existingAdmin = await userRepo.GetByEmailAsync(adminEmail);
    
    if (existingAdmin == null)
    {
        var adminRequest = new SignUpRequest
        {
            Email = adminEmail,
            Password = "admin",
            Username = "admin",
            Role = LetsLearn.Core.Shared.AppRoles.Admin
        };
        
        try
        {
            // Create a minimal HttpContext for the registration
            var httpContext = new DefaultHttpContext();
            await authService.RegisterAsync(adminRequest, httpContext);
            Console.WriteLine("Admin user created successfully!");
            Console.WriteLine($"Email: {adminEmail}");
            Console.WriteLine("Password: admin");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create admin user: {ex.Message}");
        }
    }
}

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseJwtAuth();

app.UseAuthorization();

// ========== DEBUG ENDPOINTS FOR K6 TESTING (NO AUTH) ==========
if (app.Environment.IsDevelopment())
{
    // Student workflow endpoints
    app.MapGet("/debug/courses", () => Results.Ok(new[]
    {
        new { id = "CS101", title = "Introduction to Programming", instructor = "John Doe" },
        new { id = "CS102", title = "Data Structures", instructor = "Jane Smith" }
    }));

    app.MapGet("/debug/course/{courseId}", (string courseId) => Results.Ok(new
    {
        id = courseId,
        title = "Sample Course",
        description = "Course description",
        instructor = "Test Teacher",
        sections = new[] { new { id = Guid.NewGuid(), title = "Section 1" } }
    }));

    app.MapGet("/debug/course/{courseId}/work", (string courseId, string? type) => Results.Ok(new[]
    {
        new { id = Guid.NewGuid(), title = "Assignment 1", type = "assignment", dueDate = DateTime.UtcNow.AddDays(7) },
        new { id = Guid.NewGuid(), title = "Quiz 1", type = "quiz", dueDate = DateTime.UtcNow.AddDays(3) }
    }));

    app.MapGet("/debug/topic/{topicId}", (Guid topicId) => Results.Ok(new
    {
        id = topicId,
        title = "Sample Topic",
        type = "quiz",
        content = "Topic content here",
        questions = new[] { new { id = Guid.NewGuid(), text = "Sample question?" } }
    }));

    app.MapPost("/debug/topic/{topicId}/quiz-response", (Guid topicId) => Results.Ok(new
    {
        id = Guid.NewGuid(),
        topicId = topicId,
        score = 85.5,
        submittedAt = DateTime.UtcNow
    }));

    app.MapGet("/debug/topic/{topicId}/quiz-response", (Guid topicId) => Results.Ok(new[]
    {
        new { id = Guid.NewGuid(), topicId = topicId, score = 85.5, submittedAt = DateTime.UtcNow }
    }));

    app.MapPost("/debug/topic/{topicId}/assignment-response", (Guid topicId) => Results.Ok(new
    {
        id = Guid.NewGuid(),
        topicId = topicId,
        status = "submitted",
        submittedAt = DateTime.UtcNow
    }));

    app.MapGet("/debug/notifications", () => Results.Ok(new[]
    {
        new { id = Guid.NewGuid(), title = "New Assignment", message = "Assignment posted", isRead = false },
        new { id = Guid.NewGuid(), title = "Quiz Graded", message = "Your quiz has been graded", isRead = true }
    }));

    app.MapGet("/debug/user/me/report", (string courseId) => Results.Ok(new
    {
        courseId = courseId,
        quizzesTaken = 5,
        averageQuizScore = 87.5,
        assignmentsSubmitted = 3,
        averageAssignmentScore = 92.0
    }));

    app.MapPost("/debug/course/{courseId}/topic/{topicId}/comments", (string courseId, Guid topicId) => Results.Ok(new
    {
        id = Guid.NewGuid(),
        topicId = topicId,
        text = "Sample comment",
        createdAt = DateTime.UtcNow
    }));

    // Teacher workflow endpoints
    app.MapGet("/debug/users", () => Results.Ok(new[]
    {
        new { id = Guid.NewGuid(), name = "Student One", email = "student1@test.com", role = "Student" },
        new { id = Guid.NewGuid(), name = "Student Two", email = "student2@test.com", role = "Student" }
    }));

    app.MapGet("/debug/topic/{topicId}/quiz-report", (Guid topicId) => Results.Ok(new
    {
        topicId = topicId,
        totalResponses = 25,
        averageScore = 82.5,
        highestScore = 100,
        lowestScore = 45
    }));

    app.MapGet("/debug/topic/{topicId}/assignment-report", (Guid topicId) => Results.Ok(new
    {
        topicId = topicId,
        totalSubmissions = 20,
        graded = 15,
        pending = 5,
        averageScore = 88.0
    }));

    app.MapGet("/debug/topic/{topicId}/quiz-response/getAll", (Guid topicId) => Results.Ok(new[]
    {
        new { id = Guid.NewGuid(), studentId = Guid.NewGuid(), score = 90, submittedAt = DateTime.UtcNow },
        new { id = Guid.NewGuid(), studentId = Guid.NewGuid(), score = 75, submittedAt = DateTime.UtcNow }
    }));

    app.MapPut("/debug/topic/{topicId}/assignment-response/{id}", (Guid topicId, Guid id) => Results.Ok(new
    {
        id = id,
        topicId = topicId,
        grade = 95,
        feedback = "Excellent work!",
        gradedAt = DateTime.UtcNow
    }));

    app.MapGet("/debug/course/{courseId}/quiz-report", (string courseId) => Results.Ok(new
    {
        courseId = courseId,
        totalQuizzes = 10,
        totalResponses = 150,
        averageScore = 83.2
    }));

    app.MapGet("/debug/course/{courseId}/assignment-report", (string courseId) => Results.Ok(new
    {
        courseId = courseId,
        totalAssignments = 8,
        totalSubmissions = 120,
        averageScore = 86.5
    }));

    app.MapGet("/debug/questions", (string courseId) => Results.Ok(new[]
    {
        new { id = Guid.NewGuid(), courseId = courseId, text = "What is polymorphism?", type = "multiple_choice" },
        new { id = Guid.NewGuid(), courseId = courseId, text = "Explain inheritance", type = "essay" }
    }));
}
// ========== END DEBUG ENDPOINTS ==========

app.MapControllers();

app.Run();
