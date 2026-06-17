using System.ComponentModel.DataAnnotations;

namespace HotelBookingAPI.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "กรุณากรอกอีเมล")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง ต้องเป็นภาษาอังกฤษและมีรูปแบบ เช่น example@domain.com")]
        public string Email { get; set; } = string.Empty;
        
        
        
        [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$",
        ErrorMessage = "รหัสผ่านต้องมีความยาวอย่างน้อย 6 ตัวอักษร, มีตัวพิมพ์ใหญ่ อย่างน้อย 1 ตัว, ตัวเลข 1 ตัว และอักขระพิเศษ (@$!%*?&) อย่างน้อย 1 ตัว")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกยืนยันรหัสผ่าน")]
        //ใช้แท็ก [Compare] สั่งให้เปรียบเทียบค่ากับฟิลด์ "Password" ด้านบนอัตโนมัติ
        [Compare("Password", ErrorMessage = "รหัสผ่านและรหัสผ่านยืนยันไม่ตรงกัน")]
        public string ConfirmPassword { get; set; } = string.Empty;


        [Required(ErrorMessage = "กรุณากรอกชื่อ")]
        public string First_Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "กรุณากรอกนามสกุล")]
        public string Last_Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "กรุณากรอกเบอร์")]
        [RegularExpression(@"^0[0-9]{8,9}$",
        ErrorMessage = "รูปแบบเบอร์โทรศัพท์ไม่ถูกต้อง ต้องเป็นตัวเลขล้วน ขึ้นต้นด้วย 0 และมีความยาว 9-10 หลัก")]
        public string Phone { get; set; } = string.Empty;
    }
}
