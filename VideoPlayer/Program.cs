using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System.Configuration;
using VideoPlayer.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddCors(options => options.AddPolicy("CorsPolicy", policy =>
            {
                policy.AllowAnyMethod().AllowAnyOrigin()
                    //.SetIsOriginAllowed(_ => true)
                    .AllowAnyHeader();
                   //.AllowCredentials();
            }));

builder.Services.Configure<FormOptions>(options =>
            {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = int.MaxValue;
                options.MultipartHeadersLengthLimit = int.MaxValue; 
            });

builder.Services.AddControllersWithViews();

// อ่านค่า Configuration จาก appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// ดึง Connection String จาก appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// คุณสามารถใช้ connectionString ที่ดึงได้ในโปรเจกต์ ASP.NET Core ของคุณ
builder.Services.AddSingleton(connectionString);

// ลงทะเบียน VideoRepository ใน Dependency Injection Container
builder.Services.AddSingleton<IVideoRepository>(provider => new VideoRepository(connectionString));

var app = builder.Build();

app.UseWhen(context => context.Request.Path.StartsWithSegments("/uploads"), appBuilder =>
            {
                appBuilder.UseCors("CorsPolicy");

                var provider = new FileExtensionContentTypeProvider();
                // Add new mappings
                provider.Mappings[".m3u8"] = "application/x-mpegURL";
                provider.Mappings[".ts"] = "video/MP2T";

                appBuilder.UseStaticFiles(new StaticFileOptions
                {
                    ContentTypeProvider = provider,
                    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.WebRootPath, "uploads")),
                    RequestPath = "/uploads"
                });
            });

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

app.UseCors("CorsPolicy");

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
