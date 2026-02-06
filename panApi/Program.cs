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
builder.Services.AddSingleton<OfflineDownloadQueue>();
builder.Services.AddHostedService<OfflineDownloadWorker>();

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
    // 先处理 OfflineDownloadTask（避免 CodeFirst 在已有表上失败）
    try
    {
        db.Ado.ExecuteCommand(@"
            IF OBJECT_ID('OfflineDownloadTask', 'U') IS NOT NULL
               AND EXISTS (
                   SELECT 1 FROM sys.columns
                   WHERE object_id = OBJECT_ID('OfflineDownloadTask')
                     AND name = 'Id'
                     AND is_identity = 0
               )
            DROP TABLE OfflineDownloadTask;

            IF OBJECT_ID('OfflineDownloadTask', 'U') IS NULL
            CREATE TABLE OfflineDownloadTask (
                Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                UserId INT NOT NULL,
                Url NVARCHAR(2048) NOT NULL,
                ParentId INT NULL,
                Status NVARCHAR(50) NOT NULL,
                Progress INT NOT NULL,
                Message NVARCHAR(4000) NULL,
                CreateTime DATETIME NOT NULL,
                UpdateTime DATETIME NOT NULL
            );

            IF OBJECT_ID('OfflineDownloadTask', 'U') IS NOT NULL
               AND COL_LENGTH('OfflineDownloadTask', 'ParentId') IS NOT NULL
               AND COLUMNPROPERTY(OBJECT_ID('OfflineDownloadTask'), 'ParentId', 'AllowsNull') = 0
            ALTER TABLE OfflineDownloadTask ALTER COLUMN ParentId INT NULL;

            IF OBJECT_ID('OfflineDownloadTask', 'U') IS NOT NULL
               AND COL_LENGTH('OfflineDownloadTask', 'Message') IS NOT NULL
               AND COLUMNPROPERTY(OBJECT_ID('OfflineDownloadTask'), 'Message', 'AllowsNull') = 0
            ALTER TABLE OfflineDownloadTask ALTER COLUMN Message NVARCHAR(4000) NULL;
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"数据库预处理错误: {ex.Message}");
    }

    // 预处理 UserInfo 兼容列，避免 CodeFirst 在非空旧表上直接加 NOT NULL 失败
    try
    {
        bool TableExists(string tableName)
        {
            var count = Convert.ToInt32(db.Ado.GetScalar($"SELECT COUNT(1) FROM sys.objects WHERE object_id = OBJECT_ID('{tableName}') AND type = 'U'"));
            return count > 0;
        }

        bool ColumnExists(string tableName, string columnName)
        {
            var count = Convert.ToInt32(db.Ado.GetScalar($"SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID('{tableName}') AND name = '{columnName}'"));
            return count > 0;
        }

        bool IsNullableColumn(string tableName, string columnName)
        {
            var allowsNull = Convert.ToInt32(db.Ado.GetScalar($"SELECT COLUMNPROPERTY(OBJECT_ID('{tableName}'), '{columnName}', 'AllowsNull')"));
            return allowsNull == 1;
        }

        if (TableExists("UserInfo"))
        {
            if (!ColumnExists("UserInfo", "CreateTime"))
            {
                db.Ado.ExecuteCommand("ALTER TABLE UserInfo ADD CreateTime DATETIME NULL;");
            }

            if (!ColumnExists("UserInfo", "UpdateTime"))
            {
                db.Ado.ExecuteCommand("ALTER TABLE UserInfo ADD UpdateTime DATETIME NULL;");
            }

            if (ColumnExists("UserInfo", "CreateTime"))
            {
                db.Ado.ExecuteCommand("UPDATE UserInfo SET CreateTime = ISNULL(CreateTime, GETDATE()) WHERE CreateTime IS NULL;");
            }

            if (ColumnExists("UserInfo", "UpdateTime"))
            {
                if (ColumnExists("UserInfo", "CreateTime"))
                {
                    db.Ado.ExecuteCommand("UPDATE UserInfo SET UpdateTime = ISNULL(UpdateTime, CreateTime) WHERE UpdateTime IS NULL;");
                }
                else
                {
                    db.Ado.ExecuteCommand("UPDATE UserInfo SET UpdateTime = ISNULL(UpdateTime, GETDATE()) WHERE UpdateTime IS NULL;");
                }
            }

            if (ColumnExists("UserInfo", "CreateTime") && IsNullableColumn("UserInfo", "CreateTime"))
            {
                db.Ado.ExecuteCommand("ALTER TABLE UserInfo ALTER COLUMN CreateTime DATETIME NOT NULL;");
            }

            if (ColumnExists("UserInfo", "UpdateTime") && IsNullableColumn("UserInfo", "UpdateTime"))
            {
                db.Ado.ExecuteCommand("ALTER TABLE UserInfo ALTER COLUMN UpdateTime DATETIME NOT NULL;");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"UserInfo 预处理错误: {ex.Message}");
    }
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
            IF COL_LENGTH('UserInfo', 'UpdateTime') IS NULL
            BEGIN
                ALTER TABLE UserInfo ADD UpdateTime DATETIME NULL;
                UPDATE UserInfo SET UpdateTime = ISNULL(CreateTime, GETDATE()) WHERE UpdateTime IS NULL;
                ALTER TABLE UserInfo ALTER COLUMN UpdateTime DATETIME NOT NULL;
            END
            IF COL_LENGTH('UserInfo', 'LastLoginTime') IS NULL
            ALTER TABLE UserInfo ADD LastLoginTime DATETIME NULL;
            IF COL_LENGTH('StorageItem', 'DeleteTime') IS NULL
            BEGIN
                ALTER TABLE StorageItem ADD DeleteTime DATETIME NULL;
                UPDATE StorageItem SET DeleteTime = UpdateTime WHERE IsDeleted = 1 AND DeleteTime IS NULL;
            END
            IF COL_LENGTH('StorageItem', 'FavoriteTime') IS NULL
            BEGIN
                ALTER TABLE StorageItem ADD FavoriteTime DATETIME NULL;
                UPDATE StorageItem SET FavoriteTime = UpdateTime WHERE IsFavorite = 1 AND FavoriteTime IS NULL;
            END
            IF COL_LENGTH('OfflineDownloadTask', 'ParentId') IS NULL
            ALTER TABLE OfflineDownloadTask ADD ParentId INT NULL;
            IF COL_LENGTH('OfflineDownloadTask', 'ParentId') IS NOT NULL
               AND COLUMNPROPERTY(OBJECT_ID('OfflineDownloadTask'), 'ParentId', 'AllowsNull') = 0
            ALTER TABLE OfflineDownloadTask ALTER COLUMN ParentId INT NULL;
            IF COL_LENGTH('OfflineDownloadTask', 'Message') IS NOT NULL
               AND COLUMNPROPERTY(OBJECT_ID('OfflineDownloadTask'), 'Message', 'AllowsNull') = 0
            ALTER TABLE OfflineDownloadTask ALTER COLUMN Message NVARCHAR(4000) NULL;
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
            UpdateTime = DateTime.Now,
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
