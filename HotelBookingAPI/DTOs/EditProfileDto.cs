namespace HotelBookingAPI.DTOs
{
    public class EditProfileDto
    {
        public int CustomerId { get; set; } // ใช้ระบุว่ากำลังแก้ของลูกค้าคนไหน
        public string Email { get; set; } = string.Empty; //แก้ได้แต่ต้องยืนยัน email ในนาคต
        public string First_Name { get; set; }
        public string Last_Name { get; set; }
        public string phone { get; set; }
    }
}
