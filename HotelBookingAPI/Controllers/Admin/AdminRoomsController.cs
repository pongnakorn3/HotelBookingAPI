using HotelBookingAPI.Controllers.Customer;
using HotelBookingAPI.Data;
using HotelBookingAPI.DTOs;
using HotelBookingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop.Infrastructure;

namespace HotelBookingAPI.Controllers.Admin
{
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    public class AdminRoomsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AdminRoomsController(AppDbContext context)
        {
            _context = context;
        }

        //api/admin/create-rooms
        [HttpPost("create-rooms")]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { Message = "ข้อมูลห้องพักไม่ถูกต้อง" });
            }

            if (string.IsNullOrWhiteSpace(dto.RoomType))
            {
                return BadRequest(new { Message = "กรุณาระบุประเภทห้องพัก (Room Type)" });
            }

            if (dto.PricePerNight <= 0)
            {
                return BadRequest(new { Message = "ราคากลางต่อคืนต้องมากกว่า 0 บาท" });
            }

            // 2. เช็คว่าประเภทห้องพัก (RoomType) นี้มีอยู่ในระบบแล้วหรือยัง (ป้องกันการเพิ่มซ้ำซ้อน)
            var isRoomTypeExist = _context.Rooms
                .AsEnumerable() // สลับมาประมวลผลบนหน่วยความจำเพื่อหลบข้อจำกัด Async ตัวนี้
                .Any(r => r.RoomType.Equals(dto.RoomType, StringComparison.OrdinalIgnoreCase));

            if (isRoomTypeExist)
            {
                return BadRequest(new { Message = $"ประเภทห้องพัก '{dto.RoomType}' มีอยู่ในระบบแล้ว หากต้องการเปลี่ยนราคาหรือจำนวน กรุณาใช้ระบบแก้ไขข้อมูล" });
            }

            // 3. ผูกข้อมูลจาก DTO เข้ากับ Model ของฐานข้อมูล Postgres
            var newRoom = new Room
            {
                RoomType = dto.RoomType,
                PricePerNight = dto.PricePerNight,
                IsAvailable = true, // ตั้งค่าเริ่มต้นให้พร้อมใช้งานทันทีเมื่อสร้าง
                Description = dto.Description,
                RoomCount = dto.RoomCount,
                Details = dto.Details
            };

            // 4. บันทึกลงตาราง "rooms" ในฐานข้อมูล
            _context.Rooms.Add(newRoom);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "สร้างประเภทห้องพักใหม่เข้าสู่ระบบเรียบร้อยแล้ว!",
                RoomId = newRoom.RoomId
            });
        }



        //api/admin/edit-rooms แก้ไขข้อมูลห้องพัก
        [HttpPost("edit-rooms")]
        public async Task<IActionResult> EditRooms([FromBody] EditRoomDto dto)
        {
            try
            {
                if (dto.RoomId <= 0)
                {
                    return BadRequest(new { Message = "กรุณาระบุ RoomType ที่ต้องการแก้ไขให้ถูกต้อง" });
                }

                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == dto.RoomId);

                if (room is null)
                {
                    return NotFound(new { Message = "ไม่พบข้อมูลที่จะแก้ไข" });
                }
                if (!string.IsNullOrEmpty(dto.RoomType) && room.RoomType.ToLower().Trim() != dto.RoomType.ToLower().Trim())
                {
                    var isTypeExsit = _context.Rooms
                        .AsEnumerable()
                        .Any(r => r.RoomType.Equals(dto.RoomType.Trim(), StringComparison.OrdinalIgnoreCase) && r.RoomId != dto.RoomId);
                    if (isTypeExsit)
                    {
                        return BadRequest(new { Message = "ห้องพักประเภทนี้มีในระบบแล้ว" });
                    }
                }

                // 1. ตรวจสอบค่าว่างถ้าส่งมาว่าง ให้คงค่าเดิมใน DB ไว้) RoomType PricePerNight Description RoomCount Details
                if (!string.IsNullOrEmpty(dto.RoomType))
                {
                    room.RoomType = dto.RoomType;
                }
                if (dto.PricePerNight > 0)
                {
                    room.PricePerNight = dto.PricePerNight;
                }

                room.IsAvailable = dto.IsAvailable;

                if (!string.IsNullOrEmpty(dto.Description))
                {
                    room.Description = dto.Description;
                }
                if (dto.RoomCount > 0)
                {
                    room.RoomCount = dto.RoomCount;
                }
                if (!string.IsNullOrEmpty(dto.Details))
                {
                    room.Details = dto.Details;
                }

                _context.Rooms.Update(room);
                _context.SaveChanges();

                return Ok(new { Message = $"แก้ไขข้อมูลของห้องพักประเภท {room.RoomType} นี้เรียบร้อยแล้ว" });
            }

            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"เกิดข้อผิดพลาดภายในระบบ: {ex.Message}" });
            }
        }


        //api/admin/room-all
        [HttpGet("room-all")]
        public async Task<IActionResult> GetRoom()
        {
            try
            {
                var rooms = await _context.Rooms.ToListAsync();
                return Ok(rooms);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"เกิดข้อผิดพลาดในการดึงข้อมูล: {ex.Message}" });
            }



        }
    }
}
       
            

    



