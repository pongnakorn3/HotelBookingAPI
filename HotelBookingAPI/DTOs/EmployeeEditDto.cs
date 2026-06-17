namespace HotelBookingAPI.DTOs
{
    public class EmployeeEditDto
    {
        public int EmployeeId { get; set; }//ไอดีพนักงานคนที่จะถูกแก้ไข
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Receptionist"; // ปรับตำแหน่งใหม่ได้
        public bool IsActive { get; set; } = true; // เปิด-ปิด การใช้งานบัญชี (เช่น พนักงานลาออก)
    }
}