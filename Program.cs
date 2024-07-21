using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NewsAggregation;
using NewsAggregation.Data;
using NewsAggregation.Services;
using NewsAggregation.Services.Interfaces;
using NewsAggregation.Services.ServiceJobs.Email;
using ServiceStack;
using Swashbuckle.AspNetCore.Filters;
using System.Configuration;
using System.Text;
using AutoMapper;
using NewsAggregation.Helpers;
using NewsAggregation.Services.ServiceJobs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Allow CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", builder =>
    {
        builder.WithOrigins("http://localhost:5500", "https://localhost:5500", "http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Content-Range");
    });
});

var mapperConfiguration = new MapperConfiguration(
    mc => mc.AddProfile(new AutoMapperConfiguration()));

IMapper mapper = mapperConfiguration.CreateMapper();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 5242880000; // 500 MB File Upload Limit
});


builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IAuthService, AuthService>();
//builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPlansService, PlansService>();
builder.Services.AddScoped<IAdsService, AdsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISubscriptionsService, SubscriptionsService>();
builder.Services.AddScoped<ISourceService, SourceService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IBookmarkService, BookmarkService>();

builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.EnableDetailedErrors = true;
    hubOptions.KeepAliveInterval = TimeSpan.FromSeconds(10);
    hubOptions.HandshakeTimeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddSingleton<RssService>();

// Add Notification Hub and map it to /notification

//builder.Services.AddSignalR().AddHubOptions<NotificationHub>(options =>
//{
//    options.EnableDetailedErrors = true;
//});

builder.Services.AddControllers();

builder.Services.AddHostedService<BackgroundNotificationService>();
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>(sp => new BackgroundTaskQueue(100));
builder.Services.AddTransient<SecureMail>();
builder.Services.AddHostedService<QueueEmailService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
    });

    options.OperationFilter<SecurityRequirementsOperationFilter>();
});


builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("AppSecurity:Secret").Value!))
    };
});


var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

string accessToken = configuration["AWS:AccessKey"];
string secret = configuration["AWS:SecretKey"];
var connectionString = configuration["ConnectionStrings:DefaultConnection"];


var credentials = new Amazon.Runtime.BasicAWSCredentials(accessToken, secret);
var config = new AmazonS3Config
{
    RegionEndpoint = Amazon.RegionEndpoint.EUWest3
};

builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, config));

builder.Services.AddDbContext<DBContext>(options =>
{
    options.UseNpgsql(connectionString);
});

var app = builder.Build();

// Enable middleware to get FORWARDED headers

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
c.DisplayRequestDuration();
c.DefaultModelExpandDepth(0);
c.SwaggerEndpoint("/swagger/v1/swagger.json", "News Aggregation");
});

app.MapScalarUi();

app.UseHttpsRedirection();

app.UseCors("AllowedOrigins");



app.MapControllers();

app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<NotificationHub>("/notifications");
});

app.Run();
