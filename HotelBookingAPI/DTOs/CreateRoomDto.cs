namespace HotelBookingAPI.DTOs
{
    public class CreateRoomDto
    {
        public string RoomType { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public string? Description { get; set; }
        public int RoomCount { get; set; }
        public string Details { get; set; } = string.Empty;
    }
}
