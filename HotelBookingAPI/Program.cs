using System.Text;
using HotelBookingAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 1. ลงทะเบียนตั้งค่า Swagger (ตัดคำสั่งย่อยที่ทำพังออกทั้งหมด หน้าจอจะกลับมารันผ่านฉลุย)
builder.Services.AddSwaggerGen();

// 2. เชื่อมต่อฐานข้อมูล PostgreSQL 
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===================================================================
// 🔥 [จุดที่ขาดหายไปทำให้ออเร่อ] ต้องเติมระบบตัวตรวจพาสปอร์ต JWT ตัวนี้กลับเข้ามาครับ
// ===================================================================
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? "ThisIsMySuperSecretKeyForHotelBookingAPI2026!!!");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "HotelBookingServer",
        ValidAudience = jwtSettings["Audience"] ?? "HotelBookingFrontend",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// สั่งให้ระบบ .NET เปิดบอทตัวนี้ให้ทำงานเบื้องหลังทันทีที่เปิดเซิร์ฟเวอร์
builder.Services.AddHostedService<BookingTimeoutService>();

// ===================================================================
// ปลดล็อกกฎเรื่องโซนเวลาของ PostgreSQL (ช่วยให้บันทึกเวลาทั่วไปได้ ไม่บังคับเป็น UTC 100%)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var app = builder.Build();

// 3. เปิดใช้งานหน้าต่าง Swagger UI ให้แสดงผล
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 4. ลำดับระบบความปลอดภัยหลังบ้าน (เมื่อมีตัวสร้างด้านบนแล้ว ตรงนี้จะทำงานได้สมบูรณ์ ไม่ระเบิด)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();