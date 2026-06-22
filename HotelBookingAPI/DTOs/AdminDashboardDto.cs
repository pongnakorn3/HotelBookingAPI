namespace HotelBookingAPI.DTOs
{
    public class AdminDashboardDto
    {
        public int AvailableRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public double OccupancyRate { get; set; }

        public decimal TotalRevenueToday { get; set; }
        public decimal TotalRevenueMonth { get; set; }
        public decimal TotalRevenueYear { get; set; }

        public int TotalBookings { get; set; }
        public int TodayCheckIns { get; set; }
        public int TodayCheckOuts { get; set; }
    }
}