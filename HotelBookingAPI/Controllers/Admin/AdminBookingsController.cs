using HotelBookingAPI.Data;
using HotelBookingAPI.DTOs;
using HotelBookingAPI.Helpers;
using HotelBookingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAPI.Controllers.Admin
{

    [ApiController]
    [Route("api/admin/bookings")]
    [Authorize(Roles = "Admin")]
    public class AdminBookingsController : Controller
    {
        private readonly AppDbContext _context; // เปลี่ยนเป็นชื่อ DbContext ของคุณ

        public AdminBookingsController(AppDbContext context)

        {
            _context = context;
        }

        [HttpPost("walk-in")]
        public async Task<IActionResult> CreateWalkInBooking([FromBody] AdminWalkInBookingDto dto)
        {

            // ใช้ Transaction เพื่อความปลอดภัย หากบันทึกจองผ่านแต่เบสพังส่วนอื่น จะถูกโรลแบ็คคืนค่าทั้งหมด
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                //ดึงข้อมูลห้องพักเพื่อตรวจสอบว่ามีจริง และดึงราคาต่อคืนมาคำนวณ

                var room = await _context.Rooms.FindAsync(dto.RoomId);
                if (room == null)
                {
                    return NotFound(new { Message = "ไม่พบประเภทห้องพักที่ระบุในระบบ" });
                }

                //เช็คการจองซ้อน (คำนวนห้องว่างจริง ตอนที่แอดมินกำลังจอง)
                // ค้นหาใบจองทั้งหมดในระบบที่มีช่วงเวลา "คาบเกี่ยว" กับวันที่แอดมินกำลังจะจอง
                // และคำนวณว่าในระบบถูกจองไปแล้วกี่ห้องในช่วงเวลานั้น
                var bookedRoomsCount = await _context.Bookings
                    .Where(b => b.RoomId == dto.RoomId &&
                                (b.Status == "Confirmed" || b.Status == "Pending") &&
                                b.CheckInDate < dto.CheckOutDate && // วันเข้าพักใหม่ มาก่อนวันออกของคนเก่า
                                b.CheckOutDate > dto.CheckInDate)  // วันออกใหม่ มาทีหลังวันเข้าของคนเก่า
                    .SumAsync(b => b.RoomCount);

                // ค้นหาว่าขีดจำกัดสูงสุดของห้องประเภทนี้ (Total Capacity) มีทั้งหมดกี่ห้อง
                // (สมมติว่าตาราง Rooms มีฟิลด์ชื่อ TotalRooms หรือใช้ RoomCount เป็นจำนวนห้องทั้งหมดของโรงแรม)
                int totalInventoryRooms = room.RoomCount;
                int availableRooms = totalInventoryRooms - bookedRoomsCount;

                // ตรวจสอบว่าห้องว่างเหลือพอให้จองไหม
                if (availableRooms < dto.RoomCount)
                {
                    return BadRequest(new
                    {
                        Message = $"ห้องพักไม่พอในช่วงเวลาดังกล่าว! " +
                                  $"ห้องประเภทนี้มีทั้งหมด {totalInventoryRooms} ห้อง " +
                                  $"ถูกจองไปแล้ว {bookedRoomsCount} ห้อง " +
                                  $"คงเหลือว่างให้จองเพียง {availableRooms} ห้อง"
                    });
                }

                //คำนวณราคาที่พัก 
                //หาจำนวนคืน
                int totalNights = (dto.CheckOutDate.Date - dto.CheckInDate).Days;
                if (totalNights <= 0) totalNights = 1; // ป้องกันข้อผิดพลาดกรณีเลือกวันเดียวกัน คิดเป็นอย่างน้อย 1 คืน

                // คำนวณ: (ราคาห้องต่อคืน * จำนวนห้องที่จอง) * จำนวนคืน
                decimal calculatadTotalPrice = (room.PricePerNight * dto.RoomCount) * totalNights;

                //คำนวณค่าบริการผู้ใหญ่เกิน
                int allowedAdults = dto.RoomCount * 2;
                if (dto.Adult > allowedAdults)
                {
                    int extraAdults = dto.Adult - allowedAdults;
                    calculatadTotalPrice += (extraAdults * 1000 * totalNights);
                }

                //คำนวณค่าบริการเด็กเกิน
                int allowedChildren = dto.RoomCount * 2;
                if (dto.Child > allowedChildren)
                {
                    int extraChildren = dto.Child - allowedChildren;
                    calculatadTotalPrice += (extraChildren * 749 * totalNights);
                }

                //บวกค่าเตียงเสริม
                if (dto.HasExtraBed)
                {
                    calculatadTotalPrice += (500 * totalNights);
                }

                //บันทึกข้อมูลใบจอง Walk-in ลงตาราง Bookings
                //เจน booking number สำหรับแอดมินหน้างานขึ้นมาใหม่
                string bookingNumber = $"WK-{DateTime.Now.ToString("yyyyMMdd")}-{new Random().Next(1000, 9999)}";

                var booking = new Booking
                {
                    RoomId = dto.RoomId,

                    CustomerId = 1,

                    // 🎯 บันทึกแยกคอลัมน์ลงฐานข้อมูลตรง ๆ ได้อย่างสวยงามแล้วครับ!
                    CustomerFirstName = dto.CustomerFirstName,
                    CustomerLastName = dto.CustomerLastName,
                    CustomerPhone = CryptoHelper.Encrypt(dto.CustomerPhone),
                    CustomerEmail = CryptoHelper.Encrypt(dto.CustomerEmail),

                    CheckInDate = dto.CheckInDate.Date,
                    CheckOutDate = dto.CheckOutDate.Date,
                    RoomCount = dto.RoomCount,
                    Adult = dto.Adult,
                    Child = dto.Child,
                    HasExtraBed = dto.HasExtraBed,

                    // บันทึกราคาที่ระบบหลังบ้านทำการคำนวณให้
                    TotalPrice = calculatadTotalPrice,

                    Status = "Confirmed", // แอดมินทำรายการเองหน้าเคาน์เตอร์ จ่ายเงินแล้วอัปเดตเป็นสิทธิ์นี้เลย
                    BookingNumber = bookingNumber,
                    CreatedAt = DateTime.UtcNow,
                    PaymentMethod = dto.PaymentMethod // จ่ายด้วยช่องทาง "Cash" / "Transfer" ที่รับมาจากหน้าบ้าน
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                //ยืนยันกระบวนการทำงานสำเร็จทั้งหมด
                await transaction.CommitAsync();

                return Ok(new
                {
                    Message = "บันทึกการจองแบบ Walk-in และล็อกสต็อกห้องพักสำเร็จ!",
                    BookingId = booking.BookingId,
                    BookingNumber = booking.BookingNumber,
                    TotalNights = totalNights,
                    TotalPrice = booking.TotalPrice,
                    PaymentMethod = booking.PaymentMethod,
                    Status = booking.Status,
                    CustomerPhone = booking.CustomerPhone,
                    CustomerEmail = booking.CustomerEmail
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { Message = $"เกิดข้อผิดพลาดในระบบ: {ex.Message}" });
            }


            //เปลี่ยนชื่อ นามสกุล เบอร์โทร



        }
        [HttpPost("{id}/update-customer")]
        public async Task<IActionResult> UpdateCustomerInfo(int id, [FromBody] UpdateBookingCustomerDto dto)
        {
            //ค้นหาใบจองจาก id
            var booking = await _context.Bookings.FindAsync(id);

            //ถ้าไม่เจอใบจองเลขนี้ ให้ส่ง error แจ้งเตือน
            if (booking == null)
            {
                return NotFound(new { Message = $"ไม่พบข้อมูลการจอง ID: {id} ในระบบ" });
            }

            //อัปเดรตข้อมูล ถ้าช่องว่างส่งมาเอาค่าเดิม
            if (!String.IsNullOrEmpty(dto.CustomerFirstName))
            {
                booking.CustomerFirstName = dto.CustomerFirstName;
            }
            if (!String.IsNullOrEmpty(dto.CustomerLastName))
            {

                booking.CustomerLastName = dto.CustomerLastName;
            }
            if (!String.IsNullOrEmpty(dto.CustomerPhone))
            {
                booking.CustomerPhone = dto.CustomerPhone;
            }
            if (!String.IsNullOrEmpty(dto.CustomerEmail))
            {
                booking.CustomerEmail = dto.CustomerEmail;
            }

            //บันทึกลงฐานข้อมูล
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();

            //ตอบกลับหน้าบ้าน
            return Ok(new
            {
                Message = "อัปเดตข้อมูลผู้เข้าพักสำเร็จเรียบร้อยแล้ว",
                BookingId = booking.BookingId,
                BookingNumber = booking.BookingNumber,
                UpdatedDetails = new
                {
                    CustomerFirstName = booking.CustomerFirstName,
                    CustomerLastName = booking.CustomerLastName,
                    CustomerPhon = booking.CustomerPhone,
                    CustomerEmail = booking.CustomerEmail,


                }

            });

        }

        [HttpGet("bookings-all")]
        public async Task<IActionResult> GetAllBooking()
        {
            var bookings = await _context.Bookings.ToListAsync();
            return Ok(bookings);
        }


        [HttpGet("search")]
        public async Task<IActionResult> SearchBookings(
        [FromQuery] int? id = null,
        [FromQuery] string? name = null,
        [FromQuery] string? phone = null)
        {
            try
            {
                // 1. ตั้งต้นดึงข้อมูลจากตาราง Bookings ทั้งหมดขึ้นมาก่อน (แต่ยังไม่คัดกรอง)
                var query = _context.Bookings.AsQueryable();

                // 2. ถ้าแอดมินใส่ ID มา -> ให้กรองเฉพาะ ID นั้น
                if (id.HasValue)
                {
                    query = query.Where(b => b.BookingId == id.Value);
                }

                // 3. ถ้าแอดมินใส่ชื่อมา -> ให้กรองค้นหาจากชื่อจริง หรือ นามสกุล (ค้นหาแบบคำพ้องคล้ายๆ ได้)
                if (!string.IsNullOrEmpty(name))
                {
                    query = query.Where(b => b.CustomerFirstName.Contains(name) ||
                                             b.CustomerLastName.Contains(name));
                }

                // 4. ถ้าแอดมินใส่เบอร์โทรมา -> ให้กรองค้นหาจากเบอร์โทร
                if (!string.IsNullOrEmpty(phone))
                {
                    query = query.Where(b => b.CustomerPhone.Contains(phone));
                }

                // 5. สั่งให้ระบบ Query ดึงข้อมูลออกมาเป็น List
                var results = await query.ToListAsync();

                // ถ้าค้นหาแล้วไม่พบข้อมูลที่ตรงกับเงื่อนไขเลย
                if (results == null || !results.Any())
                {
                    return NotFound(new { Message = "ไม่พบข้อมูลการจองที่ตรงกับเงื่อนไขการค้นหาของคุณ" });
                }

                // ส่งรายการจองทั้งหมดที่ค้นพบกลับไปให้หน้าบ้าน
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"เกิดข้อผิดพลาดในการค้นหา: {ex.Message}" });
            }
        }

    }


    }
    

