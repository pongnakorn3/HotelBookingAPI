namespace HotelBookingAPI.DTOs
{
    public class UpdateBookingCustomerDto
    {
        public string? CustomerFirstName { get; set; }
        public string? CustomerLastName { get; set; }
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
    }
}
