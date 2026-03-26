using ArangoDBNetStandard;
using ArangoDBNetStandard.Transport.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Cấu hình để React truy cập được API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Port Vite
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});


// Cấu hình để kết nối ArangoDB
var arangodUri = builder.Configuration["ArangoDB:Url"];
var arangoDb = builder.Configuration["ArangoDB:Database"];
var arangoUser = builder.Configuration["ArangoDB:User"];
var arangoPassword = builder.Configuration["ArangoDB:Password"];

var transport = HttpApiTransport.UsingBasicAuth(new Uri(arangodUri), arangoDb, arangoUser, arangoPassword);

var arangoClient = new ArangoDBClient(transport);
// Dùng để cho DB sài chung hệ thống, tối ưu hiệu năng
builder.Services.AddSingleton<IArangoDBClient>(arangoClient);
// Kích hoạt tính năng viết API
builder.Services.AddControllers();

builder.Services.AddHostedService<NT208_Project.Services.DatabaseInitializerService>();

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

app.UseCors("AllowReactApp");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();


app.Run();

