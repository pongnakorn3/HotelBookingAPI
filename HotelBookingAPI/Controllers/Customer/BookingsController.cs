using HotelBookingAPI.Data;
using HotelBookingAPI.Models;
using Microsoft.AspNetCore.Authorization; // kyc
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Omise;

namespace HotelBookingAPI.Controllers.Customer
{
    [Route("api/bookings")]
    [ApiController]
    [Authorize] // <--- 2. แปะป้ายนี้ตรงนี้เลยครับ! แปลว่า "ทุกฟังก์ชันในไฟล์นี้ ต้อง Login ด้วย JWT เท่านั้นไม่งั้นห้ามเข้า"
    public class BookingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        // ดึงตัวเชื่อมฐานข้อมูลเข้ามาใช้งาน
        public BookingsController(AppDbContext context)
        {
            _context = context;
        }
        // API สำหรับกดส่งข้อมูลจองห้องพัก (POST api/bookings)
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] Booking booking)
        {
            //ดึงข้อมูลไอดีลูกค้าที่แอบซ่อนอยู่ในพาสปอร์ต Jwt ออกมาใช้งานได้โดยตรง ไม่ต้องเชื่อมข้อมูลที่หน้าบ้านพิมพ์กรอกมาเดี่ยวๆ
            var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdFromToken == null)
            {
                return Unauthorized(new { Message = "กรุณาเข้าสู่ระบบก่อนทำการจอง" });
            }

            //บังคับให้ไอดีจอง ผูกกับไอดีของพาสปอร์ต JWT เสมอ (ป้องกันการแอบอ้างไอดีคนอื่นจอง)
            booking.CustomerId = int.Parse(userIdFromToken);

            if (booking.CheckInDate < DateTime.Today)
            {
                return BadRequest(new { Message = "ห้ามเช็คอินวันที่เป็น อดีต" });
            }

            if (booking.CheckInDate == DateTime.Today)
            {
                var currentTime = DateTime.Now;

                if (currentTime.Hour >= 11)
                {
                    return BadRequest(new { Message = "ไม่สามารถเช็คอินวันปัจจุบันหลังเวลา 11:00 น. ได้ครับ กรุณาเลือกวันใหม่" });
                }

            }

            if (booking.CheckOutDate <= booking.CheckInDate)
            {
                return BadRequest(new { Message = "วันเช็คเอาท์ ต้องเกิดขึ้นหลังจากวันเช็คอินอย่างน้อย 1 คืน" });
            }

            if (booking.RoomCount <= 0)
            {
                return BadRequest(new { Message = "ต้องเลือกห้องอย่างน้อย 1 ห้อง" });
            }

            // ================================================================
            // ขั้นตอนที่ 2.5: เพิ่มกลไกเคลียร์ใบจอง Pending เก่าของตัวเองทิ้งทันที
            // ================================================================
            var pendingGarbageBookings = await _context.Bookings
                .Where(b => b.CustomerId == int.Parse(userIdFromToken) && b.Status == "Pending")
                .ToListAsync();

            if (pendingGarbageBookings.Any())
            {
                foreach (var garbage in pendingGarbageBookings)
                {
                    garbage.Status = "Cancelled"; //เปลี่ยนให้เป็นยกเลิก เพื่อืนห้องเข้าระบบทันที
                }

                //สั่งบันทึกการเคลียร์ข้อมูลเก่าลง Database ก่อน 1 รอบเพื่ออัปเดตสเตตัส
                await _context.SaveChangesAsync();
                Console.WriteLine($"[Clean Garbage] ล้างใบจองค้างจ่ายอันเก่าของลูกค้า ID: {userIdFromToken} ไป {pendingGarbageBookings.Count} รายการ");
            }
            
            //วิ่งไปค้นหาข้อมูลของห้องพักที่ลูกค้ากำลังจะจอง (ตาม RoomId ที่ส่งมา)
            var roomData = await _context.Rooms.FindAsync(booking.RoomId);

            if (roomData is null)
            {
                return BadRequest(new { Message = "ไม่พบข้อมูลห้องพักนี้ในระบบ" });
            }

            


            if (roomData.RoomCount <= 0)
            {
                return BadRequest(new { Message = "ห้องพักประเภทนี้เต็มแล้ว" });
            }
            // นับว่าห้อง ID นี้ ในช่วงวันที่ลูกค้าเลือก มีคนจองไปแล้วทั้งหมดกี่ห้อง
            var bookedRoomsCount = await _context.Bookings
                .Where(b => b.RoomId == booking.RoomId &&
                            b.Status != "Cancelled" && // ไม่นับรายการที่ยกเลิกไปแล้ว
                            b.Status != "Expired" &&
                            booking.CheckInDate < b.CheckOutDate &&
                            booking.CheckOutDate > b.CheckInDate)
                .SumAsync(b => b.RoomCount);

            // คำนวณห้องที่เหลือว่างจริงๆ ณ ช่วงเวลานั้น (จำนวนห้องทั้งหมดที่มี - จำนวนห้องที่ติดจองอยู่)
            int availableRoomsNow = roomData.RoomCount - bookedRoomsCount;

            // ดักเคสที่คุณต้องการ: เช็คก่อนว่า "จำนวนห้องที่มีอยู่น้อยกว่าจำนวนที่ลูกค้าจะจองไหม" 
            // (เช่น เหลือ 1 ห้อง แต่ลูกค้ากรอกมา 2 ห้อง ... เงื่อนไข 1 < 2 จะทำงานทันที!)
            if (availableRoomsNow < booking.RoomCount)
            {
                return BadRequest(new { Message = "จำนวนห้องพักคงเหลือไม่พอสำหรับการจอง" });
            }

            //บังคับแปลงโซนเวลาของข้อมูลจอง ให้เป็นรูปแบบ UTC ที่ PostgreSQL ต้องการ(แก้เออเร่อ Kind = Unspecified)
            booking.CheckInDate = DateTime.SpecifyKind(booking.CheckInDate, DateTimeKind.Utc);
            booking.CheckOutDate = DateTime.SpecifyKind(booking.CheckOutDate, DateTimeKind.Utc);
            booking.CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc); // แก้ไขให้ใช้เวลาปัจจุบัน

            //บังคับตอนกดจองให้บันทึก Status = Pending เสมอ ค่าเริ่มต้้น
            booking.Status = "Pending";
            booking.BookingNumber = $"BK-{DateTime.UtcNow:yyMMddd}-{new Random().Next(1000, 9999)}";



            // 7. คำนวณราคา
            var totalNights = (booking.CheckOutDate - booking.CheckInDate).Days;
            if (totalNights <= 0) totalNights = 1;

            // คำนวณราคาห้องพื้นฐาน
            decimal basePrice = roomData.PricePerNight * booking.RoomCount * totalNights;

            // คำนวณเตียงเสริม
            decimal extraBedPrice = 0;
            if (booking.HasExtraBed)
            {
                extraBedPrice = 500 * booking.RoomCount * totalNights;
            }

            // คำนวณค่าบริการคนเกิน (ผู้ใหญ่ > 2 คนละ 1000, เด็ก > 2 คนละ 749 ต่อคืน)
            decimal extraPersonCharge = 0;
            if (booking.Adult > 2)
            {
                extraPersonCharge += (booking.Adult - 2) * 1000;
            }
            if (booking.Child > 2)
            {
                extraPersonCharge += (booking.Child - 2) * 749;
            }

            // คิดราคาคนเกินตามจำนวนห้องและจำนวนคืน
            decimal totalExtraPersonPrice = extraPersonCharge * booking.RoomCount * totalNights;

            // รวมราคาสุทธิ
            booking.TotalPrice = basePrice + extraBedPrice + totalExtraPersonPrice;

            // 8. บันทึกลงฐานข้อมูล
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "จองห้องพักสำเร็จ กรุณาชำระเงินภายใน 20 นาที", Data = booking });
        }


        // 🌟 ฟังก์ชันใหม่สำหรับดึงข้อมูลส่วนตัวมาเติมในฟอร์มอัตโนมัติ (แต่หน้าบ้านแก้ไขทับได้)
        [HttpGet("booking-summary")] // 🎯 ใช้เส้นนี้แทนเส้นเดิมได้เลยครับ
        public async Task<IActionResult> GetBookingSummary(
                [FromQuery] int roomId,
                [FromQuery] DateTime checkIn,
                [FromQuery] DateTime checkOut,
                [FromQuery] int roomCount = 1,
                [FromQuery] int adults = 2,
                [FromQuery] int children = 0)
        {
            try
            {
                // ====================================================
                // ส่วนเดิมที่ดึงข้อมูลลูกค้า (ยกมาจากเส้น prefill-customer-info ของคุณ)
                // ====================================================
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                {
                    return BadRequest(new { Message = "ไม่พบข้อมูล Email ใน Token กรุณาล็อกอินใหม่อีกครั้ง" });
                }

                var userProfile = await _context.Customers
                    .Where(u => u.Email == userEmail)
                    .Select(u => new
                    {
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        PhoneNumber = u.Phone // ใช้ u.Phone ตามตารางเดิมของคุณ
                    })
                    .FirstOrDefaultAsync();

                if (userProfile == null)
                {
                    return NotFound(new { Message = "ไม่พบข้อมูลโปรไฟล์ในระบบ" });
                }

                // ====================================================
                // ส่วนที่เพิ่มเข้ามาใหม่ เพื่อดึงข้อมูลห้องและคำนวณวันพัก
                // ====================================================
                var roomData = await _context.Rooms.FindAsync(roomId);
                if (roomData == null)
                {
                    return NotFound(new { Message = "ไม่พบข้อมูลห้องพักที่เลือก" });
                }

                var totalNights = (checkOut.Date - checkIn.Date).Days;
                if (totalNights <= 0) totalNights = 1;

                // ====================================================
                // มัดรวมส่งกลับไปในก้อนเดียว
                // ====================================================
                var response = new
                {
                    // ส่วนนี้หน้าบ้านเอาไปใส่กล่องข้อความธรรมดา (พิมพ์แก้ไขได้)
                    CustomerInfo = userProfile,

                    // ส่วนรายละเอียดการจองที่ส่งผ่านมาจากหน้าค้นหา
                    BookingDetails = new
                    {
                        RoomId = roomId,
                        RoomType = roomData.RoomType,
                        PricePerNight = roomData.PricePerNight,
                        TotalNights = totalNights,

                        // หน้าบ้านเอาไปทำแสดงผลแบบข้อความธรรมดา หรือกล่องที่สลับ disabled ไว้ (ห้ามแก้)
                        CheckInDate = checkIn.ToString("yyyy-MM-dd"),
                        CheckOutDate = checkOut.ToString("yyyy-MM-dd"),

                        // หน้าบ้านทำเป็นปุ่มบวกลบ หรือ Dropdown ให้เขาเปลี่ยนใจปรับแก้ได้อีกรอบ
                        RoomCount = roomCount,
                        Adults = adults,
                        Children = children,
                        HasExtraBed = false // ค่าเริ่มต้น
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"เกิดข้อผิดพลาดภายในระบบ: {ex.Message}" });
            }
        }



        //ฟังชันสำหรับการจ่ายตังผ่าน Omise (testMode)
        [HttpPost("{id}/pay")]
        public async Task<IActionResult> PayBooking(int id, [FromBody] string omiseToken)
        {
            try
            {
                //ค้นหาใบจองในฐานข้อมูลก่อนว่ามีจริงไหม
                var booking = await _context.Bookings.FindAsync(id);
                if (booking == null)
                {
                    return NotFound(new { Message = "ไม่พบข้อมูลใบจองนี้ในระบบ" });
                }

                //เช็คว่าบิลนี้จ่ายไปรึยัง ป้องกันการจ่ายซ้ำ
                if (booking.Status == "Confirmed")
                {
                    return BadRequest(new { Message = "ใบจองนี้ได้รับการชำระเงินเรียบร้อยแล้ว" });
                }
                //เรียกใช้งาน omise client ด้วย secret key 
                var omiseClient = new Omise.Client(skey: "skey_test_680vp9limgcpc6qva9b");

                // สั่งสร้างรายการเก็บเงิน (Charge) ส่งไปที่ Omise Server
                // ข้อควรระวัง: Omise บังคับให้ส่งยอดเงินเป็นหน่วย "สตางค์" (เช่น 100 บาท ต้องส่งเป็น 10000 สตางค์)
                // ดังนั้นเราต้องเอา TotalPrice คูณด้วย 100 เสมอครับ
                long amountInSatang = (long)(booking.TotalPrice * 100);

                // 🎯 สั่งตัดเงินโดยเรียกใช้คลาสสร้าง Charge อย่างเป็นทางการของ Omise
                var chargeRequest = new Omise.Models.CreateChargeRequest
                {
                    Amount = amountInSatang,
                    Currency = "thb",
                    Card = omiseToken
                };

                
                var charge = await omiseClient.Charges.Create(chargeRequest);

                if(booking.Status == "Expired")
                {
                    return BadRequest(new { Message = "รายการจองนี้หมดอายุแล้ว กรุณาทำรายการใหม่อีกครั้ง" });
                }

                //ตรวจสอบผลการชำระเงินจาก Omise (ใช้ตัวเล็ก charge.paid ตามคุณสมบัติของ SDK)
                if (charge.Paid)
                {
                    //ถ้าจ่ายเงินสำเร็จ อัปเดตสถานะบิลในฐานข้อมูลเราทันที!
                    booking.Status = "Confirmed";
                    _context.Bookings.Update(booking);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        Message = "ชำระเงินสำเร็จเรียบร้อยแล้ว!",
                        ChargeId = charge.Id, // 🎯 แก้ตัวพิมพ์ใหญ่ตาม Omise SDK เส้นสีแดงจะหายไปทันที
                        Status = booking.Status,
                        ReceiptInfo = new
                        {
                            BookingNumber = booking.BookingNumber,
                            BookingId = booking.BookingId,
                            RoomCount = booking.RoomCount,

                            // 🎯 รวมรายละเอียดจำนวนผู้เข้าพักทั้งหมดให้เป็นระเบียบ
                            GuestDetails = new
                            {
                                Adults = booking.Adult,
                                Children = booking.Child,
                                HasExtraBed = booking.HasExtraBed ? "ใช่ (เสริม 1 เตียง)" : "ไม่มี"
                            },

                            AmountPaid = booking.TotalPrice,
                            CardBrand = charge.Card.Brand,
                            CardLastDigits = charge.Card.LastDigits,
                            PaidAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss") // แสดงเวลาในโซนสากลอย่างแม่นยำ
                        }
                    });
                }
                else
                {
                    //ถ้าจ่ายเงินไม่ผ่าน (เช่น บัตรวงเงินเต็ม, รหัสผิด)
                    return BadRequest(new
                    {
                        Message = $"การชำrateเงินล้มเหลว: {charge.FailureMessage}",
                        Code = charge.FailureCode
                    });
                }
            }
            catch (Exception ex)
            {
                // 💡 เคล็ดลับ: ดึง InnerException ออกมาโชว์เพื่อดูว่าฐานข้อมูลฟ้องเรื่องอะไรกันแน่
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "No inner exception";

                return StatusCode(500, new
                {
                    Message = $"เกิดข้อผิดพลาดในการติดต่อ Omise: {ex.Message}",
                    Detail = innerMessage // ข้อความนี้จะบอกเลยว่าฟิลด์ไหนพัง หรือติดล็อกอะไร
                });
            }
        }

    


        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Booking>>> GetBooking()
        {
            var list = await _context.Bookings.ToListAsync();
            return Ok(list);
        }


    }
}






