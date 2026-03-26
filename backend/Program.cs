using ArangoDBNetStandard;
using ArangoDBNetStandard.Transport.Http;
using backend.Repositories;
using IocNodes.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NT208_Project.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

// ── ArangoDB ────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IArangoDBClient>(_ =>
{
    // Đọc chuẩn tên section "ArangoDb" từ appsettings.json
    var cfg = builder.Configuration.GetSection("ArangoDb");

    var url = cfg["Url"] ?? "http://localhost:8529/";
    var database = cfg["Database"] ?? "_system";
    // Đọc chuẩn key "User" từ appsettings.json
    var username = cfg["User"] ?? "root";
    var password = cfg["Password"] ?? "";

    var transport = HttpApiTransport.UsingBasicAuth(
        new Uri(url),
        database,
        username,
        password
    );

    return new ArangoDBClient(transport);
});

// ── Application layers ───────────────────────────────────────────────────────
builder.Services.AddScoped<IIocNodeRepository, IocNodeRepository>();
builder.Services.AddScoped<IIocNodeService, IocNodeService>();
// Nhớ thêm using NT208_Project.Services; (hoặc namespace tương ứng chứa file đó) ở trên cùng
builder.Services.AddHostedService<DatabaseInitializerService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cho Swagger vào để chạy
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cấu hình Policy để React ping tới
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();