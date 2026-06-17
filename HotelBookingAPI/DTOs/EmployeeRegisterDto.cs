namespace HotelBookingAPI.DTOs
{
    public class EmployeeRegisterDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Receptionist"; // เช่น Admin, Manager, Receptionist, Housekeeper
    }
}