using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PanSystem.Services;
using PanSystem.Utils;
using PanSystem.Models;
using SqlSugar;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpClient();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PanSystem API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Background Services
builder.Services.AddHostedService<MaintenanceBackgroundService>();

// Configure SqlSugar
builder.Services.AddScoped<ISqlSugarClient>(s =>
{
    SqlSugarClient db = new SqlSugarClient(new ConnectionConfig()
    {
        ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection"),
        DbType = DbType.SqlServer,
        IsAutoCloseConnection = true,
    });

    return db;
});

// Configure Storage Service
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Configure JWT Authentication
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]!);
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                path.HasValue && (path.Value.Contains("/file/download") || path.Value.Contains("/file/thumbnail")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
        });
});

var app = builder.Build();

// 数据库初始化与管理员用户创建
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

    Console.WriteLine("正在执行数据库差异同步...");
    // 基础建表
    db.CodeFirst.InitTables(
        typeof(PanSystem.Models.UserInfo),
        typeof(PanSystem.Models.StorageItem),
        typeof(PanSystem.Models.ShareLink),
        typeof(PanSystem.Models.AuditLog)
    );

    // 强制检查并补全列 (针对已存在的表)
    try
    {
        db.Ado.ExecuteCommand(@"
            IF COL_LENGTH('UserInfo', 'Phone') IS NULL
            ALTER TABLE UserInfo ADD Phone NVARCHAR(50) NULL;
            IF COL_LENGTH('UserInfo', 'Email') IS NULL
            ALTER TABLE UserInfo ADD Email NVARCHAR(100) NULL;
        ");
        Console.WriteLine("数据库列维护检查完成");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"数据库维护错误: {ex.Message}");
    }

    if (!db.Queryable<UserInfo>().Any(u => u.UserName == "admin"))
    {
        var adminUser = new UserInfo
        {
            UserName = "admin",
            Password = HashHelper.ComputeMd5("123456"),
            IsAdmin = true,
            CreateTime = DateTime.Now,
            TotalSpace = 1024L * 1024 * 1024 * 100 // 100GB
        };
        db.Insertable(adminUser).ExecuteCommand();
        Console.WriteLine("已初始化默认管理员账号");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
