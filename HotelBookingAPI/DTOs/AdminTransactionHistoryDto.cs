namespace HotelBookingAPI.DTOs
{
    public class AdminTransactionHistoryDto
    {
        public int BookingId { get; set; }
        public string BookingNumber { get; set; }
        public string CustomerFirstName { get; set; }
        public string CustomerLastName { get; set; }

        public decimal TotalPrice { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
    }
}