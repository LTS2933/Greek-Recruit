using GreekRecruit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using GreekRecruit.Services;


var builder = WebApplication.CreateBuilder(args);

/// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<StripeService>();


var rawConnStr = builder.Configuration.GetConnectionString("Sql");
var sqlPassword = Environment.GetEnvironmentVariable("SQL_PASSWORD");

var finalConnStr = rawConnStr.Replace("__thiswontwork__", sqlPassword);


builder.Services.AddDbContext<SqlDataContext>(options =>
{
    options.UseSqlServer(finalConnStr);
});

builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Login/Login";
        options.AccessDeniedPath = "/Login/AccessDenied";
    });

builder.Services.AddAuthorization();

// AWS S3 Setup
var awsAccessKey = Environment.GetEnvironmentVariable("AWS_ACCESSKEY");
var awsSecretKey = Environment.GetEnvironmentVariable("AWS_SECRETKEY");
var awsRegion = RegionEndpoint.USEast2;

builder.Services.AddSingleton<IAmazonS3>(_ =>
    new AmazonS3Client(awsAccessKey, awsSecretKey, awsRegion));

builder.Services.AddSingleton<S3Service>();

builder.Services.AddSingleton(new OpenAIServiceWrapper(
    Environment.GetEnvironmentVariable("OpenAI__ApiKey")
));

// Force Kestrel to listen on port 8080 (Render uses this)
builder.WebHost.UseUrls("http://0.0.0.0:8080");


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Login}/{id?}");

app.Run();
