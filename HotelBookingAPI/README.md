คู่มือการสร้างระบบ JWT Authentication (.NET 9 + PostgreSQL)
คู่มือนี้แบ่งออกเป็น 3 ส่วนหลัก: การเตรียมข้อมูล (Appsettings), การตั้งค่าหลังบ้าน (Program.cs) และการทดสอบระบบ (Postman)

🔑 ส่วนที่ 1: การตั้งค่ารหัสลับและฐานข้อมูล (appsettings.json)
ก่อนเริ่มเขียนโค้ด เราต้องกำหนดกุญแจลับที่ใช้สำหรับพิมพ์พาสปอร์ต (Token) และเชื่อมฐานข้อมูลในไฟล์ appsettings.json ก่อน

---------------------------------------------------------------------------------------------------------------
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=HotelBookingDb;Username=postgres;Password=รหัสผ่านของคุณ"
  },
  "Jwt": {
    "Key": "ThisIsMySuperSecretKeyForHotelBookingAPI2026!!!", 
    "Issuer": "HotelBookingServer",
    "Audience": "HotelBookingFrontend"
  }
}
ข้อควรระวัง: ตัวแปร "Key" จะต้องมีความยาวอย่างน้อย 16-32 ตัวอักษรขึ้นไป เพื่อความปลอดภัยและป้องกันไม่ให้ระบบ Error
-----------------------------------------------------------------------------------------------------------------

ส่วนที่ 2: การเปิดระบบตรวจพาสปอร์ต (Program.cs)
โค้ดชุดนี้คือหัวใจหลัก ทำหน้าที่เปิดใช้ระบบตรวจสอบความปลอดภัย JWT และเชื่อมฐานข้อมูล โดยตัดส่วนประกอบที่ทำให้หน้าจอ Swagger ติดเส้นแดงออกเพื่อให้ระบบเบาและรันผ่านง่ายที่สุด

------------------------------------------------------------------------------------------------------------------
using System.Text;
using HotelBookingAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. ลงทะเบียน Controllers และระบบพื้นฐาน
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // เปิดใช้ Swagger แบบปกติเพื่อความเสถียร

// 2. เชื่อมต่อฐานข้อมูล PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. ตั้งค่าระบบยืนยันตัวตน JWT Bearer Authentication หลังบ้าน
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
        ValidateLifetime = true, // เปิดการตรวจเช็ควันหมดอายุพาสปอร์ต
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "HotelBookingServer",
        ValidAudience = jwtSettings["Audience"] ?? "HotelBookingFrontend",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

var app = builder.Build();

// 4. เปิดใช้งาน Swagger หน้าต่างสีฟ้าในโหมดกำลังพัฒนา
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); 
}

app.UseHttpsRedirection();

// 5. ลำดับ Middleware ความปลอดภัย (ห้ามสลับแถวเด็ดขาด!)
app.UseAuthentication(); // 1. ตรวจพาสปอร์ตก่อนว่าใครมา
app.UseAuthorization();  // 2. ตรวจสิทธิ์ว่าทำอะไรได้บ้าง

app.MapControllers();
app.Run();

------------------------------------------------------------------------------------------------------------------

ส่วนที่ 3: การล็อกประตูตารางข้อมูล (BookingsController.cs)
หากต้องการให้ตารางไหนจำเป็นต้อง Login ก่อนใช้งาน ให้ไปแปะป้ายคำว่า [Authorize] ไว้ที่หัว Controller นั้น ๆ

------------------------------------------------------------------------------------------------------------------
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // <--- แปะป้ายล็อกประตูตรงนี้ คนไม่ล็อกอินจะเข้าไม่ได้ทันที (401 Unauthorized)
    public class BookingsController : ControllerBase
    {
        // โค้ดสำหรับทำระบบ จอง/ดู/แก้ไข/ลบ ห้องพัก...
    }
}
------------------------------------------------------------------------------------------------------------------

วนที่ 4: ขั้นตอนการทดสอบระบบด้วย Postman (จากที่เราทำเมื่อกี้)
เมื่อเปิดรันโปรเจกต์ (ปุ่ม Play สีเขียว) ใน Visual Studio แล้ว ให้ย้ายมาเทสใน Postman ตามสเต็ปนี้ครับ:

สเต็ปที่ 1: เคลียร์ปัญหาเชื่อมต่อlocalhost
ไปที่เมนูรูป ฟันเฟือง (Settings) ขวาบนของ Postman

ปรับสวิตช์หัวข้อ SSL certificate verification ให้เป็น OFF (เพื่อไม่ให้ Postman บล็อกไอพี localhost ของเรา)

สเต็ปที่ 2: ไปเอาพาสปอร์ต (Token) มาก่อน
สร้าง Request ใหม่ใน Postman เปลี่ยนเป็นแบบ POST

ใส่ URL: https://localhost:รหัสพอร์ตของคุณ/api/auth/login

ไปที่แท็บ Body -> เลือก raw -> เปลี่ยนชนิดเป็น JSON

กรอกอีเมลและรหัสผ่านตัวอย่าง แล้วกด Send

ระบบจะตอบกลับมาพร้อมรหัสตัวอักษรยาวเหยียด ให้เรา ก๊อปปี้ (Copy) รหัส Token นั้นเก็บไว้

สเต็ปที่ 3: ใช้ Token เพื่อสั่งจองห้องพัก
เปิด Request ตัวที่ใช้จองห้องพัก (POST https://localhost:รหัสพอร์ต/api/bookings)

ใส่ข้อมูล JSON การจองห้องพักในแท็บ Body ตามปกติ

มองหาแท็บเมนูที่ชื่อว่า Auth (อยู่ข้างๆ แท็บ Body)

ในช่อง Type ให้เลือกเป็น Bearer Token

จะมีกล่องข้อความขวาโผล่ขึ้นมา ให้คุณ วาง (Paste) รหัส Token ตัวยาว ที่ได้มาจากสเต็ปเมื่อกี้ลงไปดื้อ ๆ ได้เลย

กดปุ่ม Send สีน้ำเงินขวาบน

🏆 ผลลัพธ์ความสำเร็จ
ถ้าส่ง Token ไปถูกต้อง: หลังบ้านจะเปิดไฟเขียว ตอบกลับมาเป็น 200 OK หรือ 201 Created พร้อมบันทึกข้อมูลการจองลงฐานข้อมูล PostgreSQL ทันที

ถ้าลืมใส่ Token หรือ Token ปลอม: หลังบ้านจะปัดตก ตอบกลับมาเป็น 401 Unauthorized ทันทีเพื่อความปลอดภัย 