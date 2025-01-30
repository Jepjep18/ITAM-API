using IT_ASSET.Services.ComputerService;
using IT_ASSET.Services.NewFolder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);
var corsPolicyName = "AllowSpecificOrigins";

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// Add services to the container.
builder.Services.AddControllers();

// Register the services
//builder.Services.AddScoped<IAssetService, AssetService>();
//builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<AssetImportService>();
builder.Services.AddScoped<AssetService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ComputerService>();


builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Replace with your Angular app's URL
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Optional: if you need to allow cookies/auth tokens
    });
});

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



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

app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
        Console.WriteLine($"Exception: {exception?.Message}");
        await context.Response.WriteAsJsonAsync(new { error = exception?.Message });
    });
});



app.UseHttpsRedirection();
app.UseCors(corsPolicyName);
app.UseAuthorization();
app.MapControllers();

app.Run();