using HotelBookingAPI.Data;
using HotelBookingAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAPI.Controllers
{
    [Route("api/rooms")] // ตัวนี้จะกลายเป็น url: api/rooms
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly AppDbContext _context;

        // ดึง AppDbContext เข้ามาใช้งานใน Controller ผ่าน Constructor
        public RoomsController(AppDbContext context)
        {
            _context = context;
        }

        // API สำหรับดึงข้อมูลห้องพักทั้งหมด: GET api/rooms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Room>>> GetRooms()
        {
            // ใช้ Entity Framework ดึงข้อมูลจากตาราง rooms ออกมาเป็นลิสต์แบบ Async
            var rooms = await _context.Rooms.ToListAsync();
            return Ok(rooms);
        }

        // API สำหรับดึงข้อมูลห้องพักด้วย ID: GET api/rooms/{id}
        [HttpGet("search")]
        public async Task<ActionResult<Room>> SearchRooms(
                int? id,
                DateTime? checkIn,
                DateTime? checkOut,
                string? roomType,
                int? roomCount,
                int adults = 0,
                int children = 0
                )
        {
            // 1. เริ่มต้นด้วยการดึงคำสั่งฐานข้อมูลห้องพักทั้งหมดมาเตรียมไว้ก่อน (ยังไม่ได้สั่งรัน)
            var query = _context.Rooms.AsQueryable();

            // 2. ถ้าผู้ใช้งานกรอก id เข้ามา (ค่าไม่เป็น null) ให้สั่งกรองข้อมูลเฉพาะ id นั้นเพิ่มเข้าไป
            if (id.HasValue)
            {
                query = query.Where(r => r.RoomId == id.Value);
            }

            // 3. ถ้าผู้ใช้งานกรอก roomType เข้ามา (ค่าไม่เป็นค่าว่าง) ให้สั่งกรองเพิ่ม
            if (!string.IsNullOrEmpty(roomType)&& roomType.ToLower() != "all")
            {
                // ใช้คำสั่ง ToLower() ทั้งคู่ เพื่อให้ค้นหาได้โดยไม่สนใจตัวพิมพ์เล็กพิมพ์ใหญ่ (เช่นพิมพ์ deluxe หรือ Deluxe ก็เจอ)
                query = query.Where(r => r.RoomType.ToLower() == roomType.ToLower());
            }

            //กรองตามจำนวนนที่เข้าพัก
            /*if (guests.HasValue)
            {
                //สมมติว่าตาราง Room มี Property ชื่อ MaxGuests (ความจุสูงสุดที่ห้องรับได้)
                query = query.Where(r => r.MaxGuests >= guests.Value);
            }*/

            List<int> bookedRoomIds = new List<int>();

            //ลอจิกการตรวจสอบห้องว่าง/เต็ม ด้วย วันเช็คอิน และ วันเช็คเอาท์
            if (checkIn.HasValue && checkOut.HasValue)
            {
                // ค้นหา ID ของห้องพักทั้งหมดที่ "ติดจอง" ในช่วงเวลาที่เลือก
                // ลอจิกคือ: วันเช็คอินที่ลูกค้าเลือก < วันเช็คเอาท์ในระบบ AND วันเช็คเอาท์ที่ลูกค้าเลือก > วันเช็คอินในระบบ
                bookedRoomIds = _context.Bookings
                    .Where(b => checkIn.Value < b.CheckOutDate && checkOut.Value > b.CheckInDate)
                    .Select(b => b.RoomId)
                    .Distinct()
                    .ToList();

                // นำมาดึงข้อมูลโดย "เรียงลำดับ" ห้องที่ไม่ได้อยู่ในรายการติดจอง (ห้องว่าง) ขึ้นก่อน
                // และห้องที่ติดจอง (ห้องเต็ม) จะถูกดันลงไปอยู่ด้านหลัง
                query = query.OrderBy(r => bookedRoomIds.Contains(r.RoomId) ? 1 : 0);
            }
            else
            {
                // ถ้าผู้ใช้ไม่ได้ระบุวัน ให้เรียงตาม ID ปกติไปก่อน
                query = query.OrderBy(r => r.RoomId);
            }
        
            // 4. สั่งให้ฐานข้อมูลทำงานกรองจริงแล้วส่งค่ากลับมาเป็นลิสต์
            var rooms = await query.ToListAsync();


            // 5. ถ้าผลลัพธ์การกรองออกมาแล้วไม่เจออะไรเลยในตาราง
            if (!rooms.Any())
            {
                return NotFound(new { message = "ไม่พบห้องพักที่ตรงกับเงื่อนไขที่ค้นหาครับ" });
            }

            var results = rooms.Select(r => new
            {
                r.RoomId,
                r.RoomType,
                r.Description,
                r.Details,
                r.RoomCount,
                r.PricePerNight,
                IsAvailable = !bookedRoomIds.Contains(r.RoomId), // True คือว่าง, False คือเต็ม

                // ส่งจำนวนคนที่ลูกค้าเลือกกลับไปด้วย เพื่อให้หน้าบ้านเอาไปจำ ข้ามไปใช้ต่อในหน้าจอง
                SearchCriteria = new
                {
                    Adults = adults,
                    Children = children
                }
            });
            // ถ้าเจอข้อมูล ให้ส่งลิสต์ห้องพักที่กรองสำเร็จกลับไป
            return Ok(results);
        }

        
    }
 }   