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
using NewsAggregation.Data.UnitOfWork;
using NewsAggregation.Services.Cached;
using Stripe;
using SourceService = NewsAggregation.Services.SourceService;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using NewsAggregation.Services.ServiceJobs.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Allow CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", builder =>
    {
        builder.WithOrigins("http://localhost:5500", "https://localhost:5500", "http://localhost:5173", "https://sapientia.life", "https://grafana.sapientia.life/", "https://news.erzen.tk")
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

builder.Services.AddAutoMapper(typeof(AutoMapperConfiguration));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetSection("Redis:Configuration").Value;
    //options.InstanceName = builder.Configuration.GetSection("Redis:InstanceName").Value;
});


builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserValidationService, UserValidationService>();


builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISourceService, SourceService>();
builder.Services.AddScoped<IUnitOfWork,UnitOfWork>();

builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.Decorate<IArticleService, CachedArticleService>();

builder.Services.AddScoped<ICommentService,CommentService>();
builder.Services.Decorate<ICommentService, CachedCommentService>();

builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.Decorate<IPaymentService, CachedPaymentService>();

builder.Services.AddScoped<IPlansService, PlansService>();
builder.Services.Decorate<IPlansService, CachedPlansService>();

builder.Services.AddScoped<ISubscriptionsService, SubscriptionsService>();
builder.Services.Decorate<ISubscriptionsService, CachedSubscriptionService>();

builder.Services.AddScoped<IAdsService, AdsService>();
builder.Services.Decorate<IAdsService, CachedAdsService>();

builder.Services.AddScoped<IBookmarkService, BookmarkService>();
builder.Services.Decorate<IBookmarkService, CachedBookmarkService>();

builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.Decorate<ICategoryService, CachedCategoryService>();

builder.Services.AddScoped<IUserPreferenceService, UserPreferenceService>();

builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.EnableDetailedErrors = true;
    hubOptions.KeepAliveInterval = TimeSpan.FromSeconds(10);
    hubOptions.HandshakeTimeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddSingleton<RssService>();

// Increse the max theards for the ThreadPool

ThreadPool.SetMinThreads(100, 100);
ThreadPool.SetMaxThreads(1000, 1000);

builder.Services.AddControllers();

//builder.Services.AddHostedService<BackgroundNotificationService>();
//builder.Services.AddHostedService<BackgroundArticleService>();
builder.Services.AddHostedService<ScapeNewsSourcesService>();

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>(sp => new BackgroundTaskQueue(2000));

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("AppSecurity:Secret").Value!)),
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var claimsIdentity = context.Principal.Identity as ClaimsIdentity;
            var userName = claimsIdentity.FindFirst(ClaimTypes.Name)?.Value;
            var email = claimsIdentity.FindFirst(ClaimTypes.Email)?.Value;
            var role = claimsIdentity.FindFirst(ClaimTypes.Role)?.Value;

            var userValidationService = context.HttpContext.RequestServices.GetRequiredService<IUserValidationService>();
            var isValidUser = await userValidationService.ValidateUserAsync(userName, email, role);

            if (!isValidUser)
            {
                context.Fail("Unauthorized");
            }
        }
    };
}).AddGoogle(options =>
{
    options.ClientId = "285450690747-af4hbh7ueknchu5lfjf2mu5hoate80d1.apps.googleusercontent.com";
    options.ClientSecret = "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";
}).AddGitHub(options =>
{
    options.ClientId = "Ov23list4GG30Kih8HFw";
    options.ClientSecret = "a9302bb44a6c9e8a14e7c77195e5c4654f2b8b35";
}).AddFacebook(options =>
{
    options.AppId = "1190384618829985";
    options.AppSecret = "4efe052abd2829dc3eaa45930e5285a5";
});


var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

string accessToken = configuration["AWS:AccessKey"];
string secret = configuration["AWS:SecretKey"];
var connectionString = configuration["ConnectionStrings:DefaultConnection"];

StripeConfiguration.ApiKey = configuration["StripeSettings:SecretKey"];

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
    endpoints.MapHub<NewsHub>("/news");
});

app.Run();
