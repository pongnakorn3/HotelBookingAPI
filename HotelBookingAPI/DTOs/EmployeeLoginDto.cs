using System.ComponentModel.DataAnnotations;

namespace HotelBookingAPI.DTOs
{
    public class EmployeeLoginDto
    {
        [Required(ErrorMessage = "กรุณากรอกอีเมลพนักงาน")]
        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกรหัสผ่านน")]
        public string Password { get; set; } = string.Empty;
    }
}
