using BCrypt.Net;
using HotelBookingAPI.Data;
using HotelBookingAPI.DTOs;
using HotelBookingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace HotelBookingAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        // ดึง IConfiguration เข้ามาเพื่อไปอ่านรหัสลับใน appsettings.json
        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        //register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); //ถ้าหน้าบ้านส่งข้อมูลผิดกฎ ตัวนี้จะดีดกลับไปทันที
            }


            //ตรวจอีเมลซ้ำ
            var isEmailExists = await _context.Customers.AnyAsync(c => c.Email.ToLower() == dto.Email.ToLower());
            if (isEmailExists)
            {
                return BadRequest(new { Messsage = "อีเมลนี้ถูกใช่งานไปแล้ว" });
            }

            //ทำการ Hash รหัสผ่านดิบให้กลายเป็นรหัสลับทันที
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            //ผูกข้อมูลลงโมเดล Customer 
            var newCustomer = new Customer
            {
                Email = dto.Email,
                PasswordHash = hashedPassword, //บันทึกรหัสลับ
                FirstName = dto.First_Name,
                LastName = dto.Last_Name,
                Phone = dto.Phone,
            };

            //บันทึกลง postgreSQL
            _context.Customers.Add(newCustomer);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "สมัครสมาชิกและทำการเข้ารหัสรหัสผ่านเรียบร้อยแล้ว!" });
        }


        //api login (POST api/auth/login)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            // 1. 🎯 ค้นหาผู้ใช้จาก Email (แก้ไขจาก request เป็น dto)
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email.ToLower() == dto.Email.ToLower());

            // 2. ถ้าไม่เจอ Email ให้เตือนทันที
            if (customer == null)
            {
                return BadRequest(new { Message = "อีเมลหรือรหัสผ่านไม่ถูกต้อง" });
            }

            // 3. 🔐 ตรวจสอบรหัสผ่านด้วย BCrypt (แทนการใช้เครื่องหมาย != แบบเดิม)
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, customer.PasswordHash);

            if (!isPasswordValid)
            {
                return BadRequest(new { Message = "อีเมลหรือรหัสผ่านไม่ถูกต้อง" });
            }

            // --------- เริ่มต้นกระบวนการปั๊มตราสร้างพาสปอร์ต JWT ---------
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            // Claim คือการฝังข้อมูลของลูกค้าคนนี้ซ่อนเข้าไปในพาสปอร์ต JWT
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, customer.CustomerId.ToString()), // ฝังไอดีลูกค้า
                    new Claim(ClaimTypes.Email, customer.Email), //ฝังอีเมล
                    new Claim(ClaimTypes.Name, $"{customer.FirstName} {customer.LastName}") //ฝังชื่อจริง
                }),
                Expires = DateTime.UtcNow.AddDays(1), //พาสปอร์ตนี้มีอายุใช้้งานได้แค่ 1 วัน นับตั้งแต่ตอนกดล็อกอิน
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],

                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)

            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token); // ได้สายอักขระยาวๆ มั่วๆ ออกมา
            // --------- สิ้นสุดกระบวนการสร้าง JWT ---------


            return Ok(new
            {
                message = "ลงชื่อเข้าใช้สำเร็จ",
                token = tokenString // ส่งพาสปอร์ตนี้กลับไปให้หน้าบ้านถือไว้ใช้งานต่อ

            });
        }

        //edit profile
        [HttpPost("edit-profile")]
        [Authorize]


        public async Task<IActionResult> Edit([FromBody] EditProfileDto dto)
        {
            //ค้นหาข้อมูลลูกค้าคนนี้ในฐานข้อมูลก่อนตาม Id ที่ส่งมา
            //  🔐 แกะอ่าน ID ของเราที่ซ่อนอยู่ใน Token (พาสปอร์ต) อัตโนมัติ โดยไม่ต้องพึ่งตัวเลขจากหน้าบ้าน
            var customerIdFromToken = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int customerId = int.Parse(customerIdFromToken);

            // 🎯 ค้นหาด้วย ID ที่แกะได้จาก Token จริง ๆ
            var customer = await _context.Customers.FindAsync(customerId);

            if (customer == null)
            {
                return NotFound(new { Message = "ไม่พบข้อมูลลูกค้าคนนี้ในระบบ" });
            }

            //ถ้ามีการส่งอีเมลใหม่มา และไม่เป็นค่าว่าง ค่อยเอามาเช็คและเปลี่ยน
            if (!string.IsNullOrEmpty(dto.Email) && dto.Email != "string" && customer.Email.ToLower() != dto.Email.ToLower())
            {
                var isEmailExists = await _context.Customers.AnyAsync(c => c.Email.ToLower() == dto.Email.ToLower());
                if (isEmailExists)
                {
                    return BadRequest(new { Message = "อีเมลนี้ถูกใช้โดยคนอื่นแล้ว" });
                }

                //เปลี่ยนเฉพาะตอนที่ส่งมาและไม่ซ้ำ
                customer.Email = dto.Email;
            }

            if (!string.IsNullOrEmpty(dto.First_Name) && dto.First_Name != "string")
            {
                customer.FirstName = dto.First_Name;
            }

            if (!string.IsNullOrEmpty(dto.Last_Name) && dto.Last_Name != "string")
            {
                customer.LastName = dto.Last_Name;
            }

            if (!string.IsNullOrEmpty(dto.phone) && dto.phone != "string")
            {
                customer.Phone = dto.phone;
            }


            //สั่งบันทึกการเปลี่ยนแปลงลง postgreSQL
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "แก้ไขข้อมูลส่วนตัวสำเร็จเรียบร้อยแล้ว!" });


        }


        [HttpGet("profile")]
        [Authorize]

        public async Task<IActionResult> GetProfile()
        {
            //แกะอ่าน ID ของเราที่ซ่อนอยู่ใน Token(โดยอิงระบบเดียวกับฟังก์ชัน Edit 
            var customerIdFromToken = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(customerIdFromToken))
            {
                return Unauthorized(new { Message = "โทเคนไม่ถูกต้องหรือไม่พบข้อมูลผู้ใช้" });
            }

            int customerId = int.Parse(customerIdFromToken);

            //ค้นหาข้อมูลลูกค้าจากฐานข้อมูล PostgreSQL
            var customer = await _context.Customers.FindAsync(customerId);


            if (customer == null)
            {
                return NotFound(new { Message = "ไม่พบข้อมูลผู้ใช้ในระบบ" });
            }

            //แพ็คข้อมูลส่งกลับไปให้หน้าบ้าน
            var profileData = new
            {
                Customer = customer.CustomerId,
                Email = customer.Email,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Phone = customer.Phone
            };

            return Ok(profileData);
        }

    

    [HttpGet("history")]
    [Authorize]
    public async Task<IActionResult> GetBookingHistory()
        {
            try
            {
                //แกะ id ของลูกค้าจาก token ที่กำลัง login อยู่
                var customerIdFFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if(string.IsNullOrEmpty(customerIdFFromToken))
                {
                    return Unauthorized(new { Message = "ไม่พบข้อมูลผู้ใช้" });
                }

                int customerId = int.Parse(customerIdFFromToken);

                // 2. ไปดึงประวัติการจองจากตาราง Bookings เฉพาะของลูกค้าคนนี้
                // 💡 มีการเชื่อมตาราง Rooms (ดูข้อมูลห้อง) และ Payments (ดูใบเสร็จ/สถานะเงิน) มาร่วมด้วย
                var bookingHistory = await _context.Bookings
                .Where(b => b.CustomerId == customerId) // 🎯 กรองเฉพาะของลูกค้าคนนี้
                .OrderByDescending(b => b.CheckInDate)  // เอาวันเข้าพักล่าสุดขึ้นก่อน
                .Select(b => new
                {
                    BookingId = b.BookingId,
                    RoomId = b.RoomId,                 // 🎯 ใช้ RoomId ตรงๆ แทนการวิ่งผ่าน Navigation Property
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Status = b.Status,              // สถานะการจอง (เช่น Pending, Confirmed, Cancelled)

                    // ส่วนของใบเสร็จและการชำระเงินที่คุณทำไว้
                    InvoiceNumber = b.BookingNumber,
                    PaymentStatus = b.Status
                })

                    .ToListAsync();

                return Ok(bookingHistory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "เกิดข้อผิดพลาดในการดึงประวัติการจอง", Details = ex.Message });
            }
        }
        }




}



