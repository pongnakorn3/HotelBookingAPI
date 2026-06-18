using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelBookingAPI.Data;
using HotelBookingAPI.DTOs;
using HotelBookingAPI.Models;
using Microsoft.AspNetCore.Authorization;

namespace HotelBookingAPI.Controllers.Admin
{
    [ApiController]
    [Route("api/admin")]
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


        //POST: api/admin/login (เข้าสู่ระบบสำหรับพนักงานทุกระดับ)

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


        //POST: api/admin/register-employee (แอดมินสร้างบัญชีพนักงานใหม่)

        [Authorize(Roles = "Admin")] //ล็อกสิทธิ์เฉพาะคนที่มี Token เป็น Admin เท่านั้น
        [HttpPost("register-employee")]
        public async Task<IActionResult> RegisterEmployee([FromBody] EmployeeRegisterDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
                {
                    return BadRequest(new { Message = "กรุณากรอก Email และ Password ให้ครบถ้วน" });
                }

                //ตรวจสอบว่า Email พนักงานคนนี้ซ้ำในระบบแล้วหรือยัง
                var isEmailExist = await _context.Employees.AnyAsync(e => e.Email.ToLower().Trim() == dto.Email.ToLower().Trim());
                if (isEmailExist)
                {
                    return BadRequest(new { Message = "Email นี้ถูกใช้งานในระบบพนักงานแล้ว" });
                }

                //แฮชรหัสผ่านของพนักงานใหม่ด้วย BCrypt
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                var newEmployee = new Employee
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PasswordHash = passwordHash,
                    Role = dto.Role ?? "Receptionist",
                    IsActive = true
                };

                _context.Employees.Add(newEmployee);
                await _context.SaveChangesAsync();

                return Ok(new { Message = $"สร้างบัญชีพนักงานตำแหน่ง {newEmployee.Role} สำเร็จเรียบร้อยแล้ว" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"เกิดข้อผิดพลาดภายในระบบ: {ex.Message}" });
            }
        }


        // GET: api/admin/employees (แอดมินเรียกดูรายชื่อพนักงานทั้งหมด)

        [Authorize(Roles = "Admin")]
        [HttpGet("employees")]
        public async Task<IActionResult> GetAllEmployees()
        {
            try
            {
                var employeeList = await _context.Employees
                    .Select(e => new
                    {
                        e.EmployeeId,
                        e.FirstName,
                        e.LastName,
                        e.Email,
                        e.Role,
                        e.IsActive
                    })
                    .ToListAsync();

                return Ok(employeeList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"เกิดข้อผิดพลาดภายในระบบ: {ex.Message}" });
            }
        }

        //POST: api/admin/edit-employee(แอดมินแก้ไขข้อมูลพนักงาน)
        [Authorize(Roles = "Admin")]
        [HttpPost("edit-employee")]
        public async Task<IActionResult> EditEmployee([FromBody] EmployeeEditDto dto)
        {
            try
            {
                if (dto.EmployeeId <= 0)
                {
                    return BadRequest(new { Message = "กรุณาระบุ EmployeeId ที่ต้องการแก้ไขให้ถูกต้อง" });
                }

                // ค้นหาพนักงานจาก ID
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == dto.EmployeeId);

                if (employee == null)
                {
                    return NotFound(new { Message = "ไม่พบข้อมูลพนักงานที่ต้องการแก้ไขในระบบ" });
                }

                // ตรวจสอบเรื่อง Email ซ้ำ
                if (employee.Email.ToLower().Trim() != dto.Email.ToLower().Trim())
                {
                    var isEmailExist = await _context.Employees
                        .AnyAsync(e => e.Email.ToLower().Trim() == dto.Email.ToLower().Trim() && e.EmployeeId != dto.EmployeeId);

                    if (isEmailExist)
                    {
                        return BadRequest(new { Message = "Email ใหม่นี้มีพนักงานคนอื่นใช้งานไปแล้ว" });
                    }
                }

                // 1. ตรวจสอบ FirstName (ถ้าส่งมาว่าง ให้คง FirstName เดิมใน DB ไว้)
                if (!string.IsNullOrEmpty(dto.FirstName))
                {
                    employee.FirstName = dto.FirstName;
                }

                // 2. ตรวจสอบ LastName (ถ้าส่งมาว่าง ให้คง LastName เดิมใน DB ไว้)
                if (!string.IsNullOrEmpty(dto.LastName))
                {
                    employee.LastName = dto.LastName;
                }

                // 3. ตรวจสอบ Email (ถ้าส่งมาว่าง ให้คง Email เดิมใน DB ไว้)
                if (!string.IsNullOrEmpty(dto.Email))
                {
                    employee.Email = dto.Email;
                }

                // 4. ตรวจสอบ Role (ถ้าส่งมาว่าง ให้คง Role เดิมใน DB ไว้)
                if (!string.IsNullOrEmpty(dto.Role))
                {
                    employee.Role = dto.Role;
                }

                // หมายเหตุ: สำหรับ IsActive เนื่องจากเป็นประเภท bool (true/false) มันจะไม่มีทางเป็น "ค่าว่าง" (null/empty string) 
                // หน้าบ้านจึงต้องส่งค่า true หรือ false มาให้ระบบเสมอ ซึ่งตรงนี้เราปล่อยให้อัปเดตตามที่ส่งมาได้เลยครับ
                employee.IsActive = dto.IsActive;

                _context.Employees.Update(employee);
                await _context.SaveChangesAsync();

                return Ok(new { Message = $"แก้ไขข้อมูลของพนักงาน {employee.FirstName} เรียบร้อยแล้ว" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"เกิดข้อผิดพลาดภายในระบบ: {ex.Message}" });
            }
        }
    }
}      


        // ================================================================
        // POST: api/admin/register-test (ยิงเพื่อชุบชีวิต Admin คนแรกของระบบ)
        // ================================================================
        /*[AllowAnonymous] // เปิดให้ยิงเส้นนี้ได้โดยไม่ต้องแนบ Token (เพราะตอนแรกสุดเรายังไม่มี Admin ไว้คอยกดสร้างใคร)
        [HttpPost("register-test")]
        public async Task<IActionResult> RegisterTest()
        {
            var isExist = await _context.Employees.AnyAsync(e => e.Email == "test@hotel.com");
            if (isExist) return BadRequest(new { Message = "มีบัญชีทดสอบนี้ในระบบแล้ว" });

            var newEmployee = new Employee
            {
                FirstName = "Somchai",
                LastName = "AdminSystem",
                Email = "test@hotel.com",
                Role = "Admin",
                IsActive = true,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456")
            };

            _context.Employees.Add(newEmployee);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "สร้างแอดมินสูงสุดทดสอบ (test@hotel.com / 123456) สำเร็จแล้ว! สามารถนำไอดีนี้ไป Login เอา Token แอดมินมาใช้งานได้เลย" });
        }*/

    

    
