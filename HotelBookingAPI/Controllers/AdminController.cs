using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelBookingAPI.Data;
using HotelBookingAPI.DTOs;
using HotelBookingAPI.Models;

namespace HotelBookingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        // Constructor ดึง context และ configuration เข้ามาร่วมงานตามปกติ
        public AdminController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        //POST: api/admin/login
        [HttpPost("login")]
        public async Task<IActionResult> EmployeeLogin([FromBody] EmployeeLoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // ค้นหาพนักงานและตรวจสอบความถูกต้อง
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Email.ToLower().Trim() == dto.Email.ToLower().Trim() && e.IsActive);

            if (employee == null)
            {
                return Unauthorized(new { Message = "อีเมลหรือรหัสผ่านพนักงานไม่ถูกต้อง" });
            }

            // ตรวจสอบรหัสผ่านพนักงานผ่าน BCrypt
            // ปรับให้เหลือแบบมาตรฐานและปลอดภัย
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, employee.PasswordHash.Trim());

            if (!isPasswordValid)
            {
                return Unauthorized(new { Message = "อีเมลหรือรหัสผ่านพนักงานไม่ถูกต้อง" });
            }

            // สร้างก้อนความปลอดภัย JWT Token
            var tokenHandler = new JwtSecurityTokenHandler();
            string jwtKey = _configuration["Jwt:Key"] ?? "YourSuperSecretKeyHere1234567890!";
            var key = Encoding.UTF8.GetBytes(jwtKey);
            var tokenKey = new SymmetricSecurityKey(key);

            // ระบุสิทธิ์ (Claims) โดยระบุชื่อ Namespace เต็มเพื่อไม่ให้ชนกับระบบอื่น
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()),
                new System.Security.Claims.Claim(ClaimTypes.Email, employee.Email),
                new System.Security.Claims.Claim(ClaimTypes.Role, employee.Role),
                new System.Security.Claims.Claim("FullName", $"{employee.FirstName} {employee.LastName}")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                SigningCredentials = new SigningCredentials(tokenKey, SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new
            {
                Token = tokenString,
                Role = employee.Role,
                FullName = $"{employee.FirstName} {employee.LastName}"
            });
        }
        
        /*[HttpPost("register-test")]
        public async Task<IActionResult> RegisterTest()
        {
            var newEmployee = new Employee
            {
                FirstName = "Somchai",
                LastName = "AdminSystem",
                Email = "test@hotel.com",
                Role = "Admin",
                IsActive = true,
                // ให้โค้ดในเครื่องของคุณเป็นคนแฮช "123456" เองกับมือ
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456")
            };

            _context.Employees.Add(newEmployee);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "สร้างพนักงานทดสอบสำเร็จแล้ว!" });
        }*/
    }
}

