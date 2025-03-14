﻿using Jose;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QUANLYVANHOA.Interfaces;
using QUANLYVANHOA.Repositories.HeThong;
using QUANLYVANHOA.Utilities;
using System.Text;
using System.Text.Json;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Cấu hình JWT từ appsettings.json
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));


// Tự động đăng ký tất cả các repository trong assembly
// Assembly là một đơn vị mã đã được biên dịch (compiled unit)
// => contain: Types(classes, interfaces, enums), Methods, properties, events
builder.Services.AddRepositoriesAndServices(typeof(HeThongNguoiDungRepository).Assembly); // typeof(SysUserRepository) đại diện, object chứa metadata

////Register repository and service
//builder.Services.AddScoped<ISysUserRepository, SysUserRepository>();
//builder.Services.AddScoped<IUserService, UserService>();
//builder.Services.AddScoped<IDanhMucDiTichXepHangRepository, DanhMucDiTichXepHangRepository>();
//builder.Services.AddScoped<IDanhMucTieuChiRepository, DanhMucTieuChiRepository>();
//builder.Services.AddScoped<IDanhMucChiTieuRepository, DanhMucChiTieuRepository>();
//builder.Services.AddScoped<IDanhMucKyBaoCaoRepository, DanhMucKyBaoCaoRepository>();
//builder.Services.AddScoped<ISysGroupRepository, SysGroupRepository>();
//builder.Services.AddScoped<ISysFunctionRepository, SysFunctionRepository>();
//builder.Services.AddScoped<ISysFunctionInGroupRepository, SysFunctionInGroupRepository>();
//builder.Services.AddScoped<ISysUserInGroupRepository, SysUserInGroupRepository>();
//builder.Services.AddScoped<IDanhMucLoaiMauPhieuRepository, DanhMucLoaiMauPhieuRepository>();
//builder.Services.AddScoped<IDanhMucLoaiDiTichRepository, DanhMucLoaiDiTichRepository>();
//builder.Services.AddScoped<IDanhMucDonViTinhRepository, DanhMucDonViTinhRepository>();


//builder.Services.AddSwaggerGen(c =>
//{
//    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        In = ParameterLocation.Header,
//        Description = "Please enter a valid token",
//        Name = "Authorization",
//        Type = SecuritySchemeType.Http,
//        Scheme = "Bearer"
//    });

//    c.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference
//                {
//                    Type = ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                }
//            },
//            new List<string>()
//        }
//    });
//});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Professional API - created by LâmHT",
        Version = "1.0",
        Description = "An ASP.NET 9 Web API for QLVH Production",
        TermsOfService = new Uri("https://www.facebook.com/hatung.lam.589"),
        Contact = new OpenApiContact
        {
            Name = "Lâm HT",
            Email = "hatunglambg2003@gmail.com",
            Url = new Uri("https://github.com/Lam-Ht-IT"),            
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
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
            new List<string>()
        }
    });
});
// Register PermissionService

// Authentication and Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };

        // Event handling for automatic token refresh
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                // Bỏ qua thử thách mặc định để tránh trả về 401 khi token hết hạn
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new { error = "You are not authorized" });
                return context.Response.WriteAsync(result);
            }
        };
    });

// Add Authorization Policies
//builder.Services.AddAuthorization(options =>
//{
//    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("admin@example.com"));
//    options.AddPolicy("UserPolicy", policy => policy.RequireRole("user1@example.com"));
//});

// Thêm dịch vụ CORS vào DI container
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost3000",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000") // Thay thế bằng địa chỉ frontend
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    );
});


var app = builder.Build();
app.UseStaticFiles();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QUANLYVANHOA v1");
        c.InjectStylesheet("/css/swagger-custom.css"); // Inject custom CSS
        c.InjectJavascript("/js/swagger-custom.js"); // Inject custom JavaScript
    });

}
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "QUANLYVANHOA v1");
    c.InjectStylesheet("/css/swagger-custom.css"); // Inject custom CSS
    c.InjectJavascript("/js/swagger-custom.js"); // Inject custom JavaScript
});

app.UseHttpsRedirection();
app.UseCors("AllowLocalhost3000");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
