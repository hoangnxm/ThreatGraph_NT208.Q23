using ArangoDBNetStandard;
using ArangoDBNetStandard.Transport.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using NT208_Project.Middlewares;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình CORS cho React
builder.Services.AddCors(options => {
    options.AddPolicy("AllowReactApp", policy => {
        policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod();
    });
});

// 2. Cấu hình ArangoDB
var arangodUri = builder.Configuration["ArangoDB:Url"];
var arangoDb = builder.Configuration["ArangoDB:Database"];
var arangoUser = builder.Configuration["ArangoDB:User"];
var arangoPassword = builder.Configuration["ArangoDB:Password"];
var transport = HttpApiTransport.UsingBasicAuth(new Uri(arangodUri), arangoDb, arangoUser, arangoPassword);
builder.Services.AddSingleton<IArangoDBClient>(new ArangoDBClient(transport));

builder.Services.AddHostedService<NT208_Project.Services.DatabaseInitializerService>();

// 3. Cấu hình JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// ĐĂNG KÝ SWAGGER VÀO HỆ THỐNG
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Thêm nút Authorize
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header dùng scheme Bearer. \r\n\r\n Cú pháp: 'Bearer [khoảng trắng] [Token_của_bạn]'\r\n VD: Bearer eyJhbGciOiJIUzI1...",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Áp dụng ổ khóa cho tất cả các API
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

builder.Services.AddControllers();
var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();

// 4. Đăng ký Middleware ghi Log
app.UseMiddleware<AuditLogMiddleware>();

app.MapControllers();
app.Run();