using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingAPI.Models
{
    [Table("rooms")] // บอกให้ C# รู้ว่าคลาสนี้แมปกับตารางชื่อ "rooms" ใน Postgres
    public class Room
    {
        [Key] // กำหนดว่าเป็น Primary Key
        [Column("room_id")] // แมปกับชื่อคอลัมน์ในฐานข้อมูลจริง (ที่เป็นตัวเล็ก)
        public int RoomId { get; set; }

        [Column("room_type")]
        public string RoomType { get; set; } = string.Empty;

        [Column("price_per_night")]
        public decimal PricePerNight { get; set; }

        [Column("is_available")]
        public bool IsAvailable { get; set; }

        [Column("description")]
        public string? Description { get; set; } // ใส่ ? เพื่อบอกว่าเป็นค่าว่าง (null) ได้

        [Column("roomcount")]
        public int RoomCount { get; set; }

        [Column("details")]
        public string Details { get; set; } = string.Empty;
    }
}
