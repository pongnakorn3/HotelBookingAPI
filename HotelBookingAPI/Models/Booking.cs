using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingAPI.Models
{
    [Table("bookings")] //บอกให้รู้จักตารางของ bookings ใน Docker
    public class Booking
    {
        [Key]
        [Column("booking_id")]
        public int BookingId { get; set; }

        [Column("room_id")]
        public int RoomId { get; set; }

        [Column("customer_id")]
        public int CustomerId { get; set; }

        [Column("check_in_date")]
        public DateTime CheckInDate { get; set; }

        [Column("check_out_date")]
        public DateTime CheckOutDate { get; set; }

        [Column("total_price")]
        public decimal TotalPrice { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Pending";

        [Column("room_count")]
        public int RoomCount { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("bookingnumber")]
        public string BookingNumber { get; set; } = string.Empty;

        [Column("hasextrabed")]
        public bool HasExtraBed { get; set; } = false;
        
        [Column("adult")]
        public int Adult { get; set; } = 2; // กำหนดค่าเริ่มต้นเป็น 2 คนฟรี
        [Column("child")]
        public int Child { get; set; } = 0;
    }
}
