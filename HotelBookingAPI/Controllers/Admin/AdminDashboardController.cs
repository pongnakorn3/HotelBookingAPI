using HotelBookingAPI.Data;
using HotelBookingAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAPI.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminDashboardController(AppDbContext context)
        {
            _context = context;

        }
        [HttpGet("summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var today = DateTime.Today; //จับเวลาปัจจุบันของวันนี้ (ปี 2026) ไว้คิวรี

            //นับจำนวนครั้งในระบบ
            var totalBookings = await _context.Bookings.CountAsync();

            //สรุปรายรับวันนี้ (นับแค่สถานะ จ่ายเงินแล้ว)
            var revenueToday = await _context.Bookings
                .Where(b => b.Status == "Confirmed" && b.CreatedAt.Date == today)
                .SumAsync(b => b.TotalPrice);

            //สรุปรายได้ประจำเดือน (นับตั้งแต่วันนี้)
            var revenueMonth = await _context.Bookings
                .Where(b => b.Status == "Confirmed" && b.CreatedAt.Year == today.Year && b.CreatedAt.Month == today.Month)
                .SumAsync(b => b.TotalPrice);

            //สรุปรายได้รายปี 
            var revenueYear = await _context.Bookings
                .Where(b => b.Status == "Confirmed" && b.CreatedAt.Year == today.Year)
                .SumAsync(b => b.TotalPrice);

            //งานปรจำวัน (check-in / check-out วันนี้)
            var todayCheckIns = await _context.Bookings
                .CountAsync(b => b.CheckInDate.Date == today);

            var todayCheckOuts = await _context.Bookings
                .CountAsync(b => b.CheckOutDate.Date == today);

            //สถานะห้องพักปัจจุบัน 
            var totalRooms = await _context.Rooms.SumAsync(r => r.RoomCount);

            // 2. หาจำนวนห้องที่มีแขกนอนพักอยู่ในโรงแรม "ณ วันนี้" จริง ๆ โดยจับเอา room_count มารวมกัน (Sum)
            var occupiedRooms = await _context.Bookings
                .Where(b => b.Status == "Confirmed"
                         && b.CheckInDate.Date <= today
                         && b.CheckOutDate.Date > today)
                .SumAsync(b => b.RoomCount);

            // 3. ห้องว่างปัจจุบัน = จำนวนห้องทั้งหมด (22) ลบออกด้วย ห้องที่มีแขกนอนพักอยู่
            var availableRooms = totalRooms - occupiedRooms;

            // 4. คำนวณอัตราการเข้าพักเป็นเปอร์เซ็นต์ (Occupancy Rate)
            double occupancyRate = totalRooms > 0
                ? Math.Round(((double)occupiedRooms / totalRooms) * 100, 2)
                : 0;

            //งส่งออกไปหน้าบ้าน
            var dashboardDto = new AdminDashboardDto
            {
                TotalBookings = totalBookings,
                TotalRevenueToday = revenueToday,
                TotalRevenueMonth = revenueMonth,
                TotalRevenueYear = revenueYear,
                TodayCheckIns = todayCheckIns,
                TodayCheckOuts = todayCheckOuts,
                AvailableRooms = availableRooms,
                OccupiedRooms = occupiedRooms,
                OccupancyRate = occupancyRate
            };

            return Ok(dashboardDto);


        }
    }
} 
    
