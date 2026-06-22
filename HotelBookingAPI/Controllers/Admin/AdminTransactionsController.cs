using HotelBookingAPI.Data;
using HotelBookingAPI.DTOs;
using HotelBookingAPI.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omise.Models;


namespace HotelBookingAPI.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/transaction")]
    [Authorize(Roles = "Admin")]
    public class AdminTransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminTransactionsController (AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("transaction-history")]
        public async Task<IActionResult> GetTransactionHistory([FromQuery] string? bookingNumber = null)
        {
            try
            {
                //ตั้งต้นดึงข้อมูลจากตาราง Bookings (ยังไม่ได้สั่งยิงคิววรีจริงไปที่ DB)
                var query = _context.Bookings.AsQueryable();

                // [เพิ่มระบบค้นหา] ถ้าหน้้าบ้านส่งคำค้นหา bookingNumber มา (ถ้าไม่ใช่ค่าว่าง)
                if (!string.IsNullOrWhiteSpace(bookingNumber))
                {
                    //ทำการกรอองเอาเฉพาะตัวที่ BookingNumber มีคำที่ค้นหาอยู่ (ใช้ Contains เพื่อให้พิมพ์คำค้นหาบางส่วนก็เจอ)
                    query = query.Where(b => b.BookingNumber.Contains(bookingNumber));

                    //หมายเหตุ: หรือถ้าอยากให้แอดมินพิมพ์รหัสต้องตรงเป๊ะๆ 100% ค่อยเปลี่ยนเป็น: b.bookingnumber == bookingNumber
                }

                //นำข้อมูลที่กรองแล้วมาคัดเลือกเฉพาะฟิลด์ที่ต้องการเข้้า Dto และดึงข้อมูลจริงขึ้นมา
                var transactionHistory = await query
                    .OrderByDescending(b => b.CreatedAt) //เอาธุกรรมใหม่ล่าสุดขึ้นก่อน
                    .Select(b => new AdminTransactionHistoryDto
                    {
                        BookingId = b.BookingId,
                        BookingNumber = b.BookingNumber,
                        CustomerFirstName = b.CustomerFirstName,
                        CustomerLastName = b.CustomerLastName,
                        TotalPrice = b.TotalPrice,
                        PaymentMethod = b.PaymentMethod,
                        CreatedAt = b.CreatedAt,
                        Status = b.Status,

                        CustomerPhone = CryptoHelper.Decrypt(b.CustomerPhone),
                        CustomerEmail = CryptoHelper.Decrypt(b.CustomerEmail)

                    }).ToListAsync();

                //ส่งคำตอบกลับไปหน้าบ้าน
                return Ok(transactionHistory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }

        [HttpGet("booking-history")]
        public async Task<IActionResult> GetBookingHistory([FromQuery] string? BookingNumber = null)
        {
            try
            {
                var query = _context.Bookings.AsQueryable();
                if (!String.IsNullOrEmpty(BookingNumber))
                {
                    query = query.Where(b => b.BookingNumber.Contains(BookingNumber));
                }

                var bookinghistory = await query
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new AdminBookingHistoryDto 
                    {
                        BookingId = b.BookingId,
                        BookingNumber = b.BookingNumber,
                        CustomerFirstName = b.CustomerFirstName,
                        CustomerLastName = b.CustomerLastName,
                        CheckInDate = b.CheckInDate,
                        CheckOutDate = b.CheckOutDate,
                        Status = b.Status
                    }).ToListAsync();

                return Ok(bookinghistory);

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}
