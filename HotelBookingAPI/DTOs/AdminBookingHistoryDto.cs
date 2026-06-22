namespace HotelBookingAPI.DTOs
{
    public class AdminBookingHistoryDto
    {
        public int BookingId { get; set; }
        public string BookingNumber { get; set; }
        public string CustomerFirstName { get; set; }
        public string CustomerLastName { get; set; }

        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
            
        public string Status { get; set;  }
    }
}
