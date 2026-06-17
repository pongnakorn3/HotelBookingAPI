using HotelBookingAPI.Data; //เชื่อมที่อยู่ฐานข้อมูล
using Microsoft.EntityFrameworkCore;

public class BookingTimeoutService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BookingTimeoutService> _logger;

    public BookingTimeoutService(IServiceProvider serviceProvider, ILogger<BookingTimeoutService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // สั่งให้พ่นข้อความทันทีที่บอทเริ่มสตาร์ทเปิดเครื่องสำเร็จ
        _logger.LogInformation("บอทตรวจเวลา: เริ่มสตาร์ทระบบตรวจเช็คเวลาชำระเงินสำเร็จแล้ว!");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    // เรียกใช้ AppDbContext ของคุณผ่านทาง Scope
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // จำลองเวลาหมดอายุ: ตอนนี้เอาแค่ 10 วินาที เพื่อให้เห็นผลทันทีในการทดสอบ
                    var timeoutTime = DateTime.UtcNow.AddMinutes(-20);

                    // ดึงรายการที่ค้างชำระ (Pending) และเวลาสร้างบิล เก่ากว่าเวลา timeout
                    var expiredBookings = await context.Bookings
                        .Where(b => b.Status == "Pending" && b.CreatedAt < timeoutTime)
                        .ToListAsync(stoppingToken);

                    if (expiredBookings.Any())
                    {
                        foreach (var booking in expiredBookings)
                        {
                            _logger.LogWarning($"บิลหมายเลข {booking.BookingId} หมดเวลาชำระเงิน! กำลังทำรายการคืนห้อง...");

                            booking.Status = "Expired";

                            var room = await context.Rooms.FindAsync(booking.RoomId);
                            if (room != null)
                            {
                                room.RoomCount += booking.RoomCount;
                                context.Rooms.Update(room);
                            }
                        }

                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"คืนสต็อกห้องพักสำเร็จจำนวน {expiredBookings.Count} รายการ");
                    }
                    else
                    {
                        // พ่นข้อความบอกเราหน่อยว่าบอทไม่ได้หลับ แต่ออกไปเดินตรวจแล้วยังไม่เจอบิลหมดอายุ
                        _logger.LogInformation("บอทตรวจเวลา: เดินตรวจตาราง Bookings แล้ว... (ยังไม่พบรายการหมดเวลา)");
                    }
                }
            }
            catch (Exception ex)
            {
                // ถ้าบอททำงานพลาดจุดไหน มันจะพ่น Error สีแดงบอกเราตรงนี้ทันทีแทนการนิ่งเงียบ
                _logger.LogError($"เกิดข้อผิดพลาดในบอทตรวจเวลา: {ex.Message}");
            }

            // สั่งให้ตื่นมาวิ่งตรวจถี่ๆ ทุก 5 วินาทีเพื่อการเทสอบ
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}