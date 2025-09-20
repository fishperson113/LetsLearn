using LetsLearn.API.Middleware;
using LetsLearn.Core.Interfaces;
using LetsLearn.Infrastructure.Services.Auth;
using LetsLearn.Infrastructure.Data;
using LetsLearn.Infrastructure.Redis;
using LetsLearn.Infrastructure.Repository;
using LetsLearn.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<RefreshTokenService>();
builder.Services.AddSingleton<AuthService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

//DEFAULT JwtBearer
//var secret = builder.Configuration["Jwt:Secret"] ?? "your-super-secret-key";
//var key = Encoding.ASCII.GetBytes(secret);

//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(options =>
//{
//    options.RequireHttpsMetadata = false;
//    options.SaveToken = true;
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuerSigningKey = true,
//        IssuerSigningKey = new SymmetricSecurityKey(key),
//        ValidateIssuer = false,
//        ValidateAudience = false,
//        ClockSkew = TimeSpan.Zero
//    };
//});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
//Only uncomment this if you want to auto apply migrations in dev environment
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var dbContext = services.GetRequiredService<LetsLearnContext>();
//    dbContext.Database.Migrate();
//}
app.UseHttpsRedirection();

app.UseJwtAuth();

app.UseAuthorization();

app.MapControllers();

app.Run();
