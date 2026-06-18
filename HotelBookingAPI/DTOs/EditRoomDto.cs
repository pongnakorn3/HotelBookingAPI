namespace HotelBookingAPI.DTOs
{
    public class EditRoomDto
    {
        public int RoomId { get; set; }
        public string RoomType { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public bool IsAvailable { get; set; }
        public string? Description { get; set; }
        public int RoomCount { get; set; }
        public string Details { get; set; } = string.Empty;
    }
}
