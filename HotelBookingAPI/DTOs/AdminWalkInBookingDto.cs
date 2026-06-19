using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingAPI.DTOs
{
    public class AdminWalkInBookingDto
    {
        // 1. ข้อมูลผู้เข้าพักหน้าเคาน์เตอร์
        public int RoomId { get; set; }

        
        public string? CustomerFirstName { get; set; }
        public string? CustomerLastName { get; set; }
        public string CustomerPhone { get; set; } = string.Empty; // เบอร์โทรศัพท์
        public string CustomerEmail { get; set; } = string.Empty; // อีเมล (ถ้ามี)
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int RoomCount { get; set; }
        public int Adult { get; set; }
        public int Child { get; set; }
        public bool HasExtraBed { get; set; }

        // 2. ข้อมูลการจ่ายเงินที่แอดมินเลือกจากหน้างาน
        public string PaymentMethod { get; set; } = string.Empty; // "Cash", "Transfer", "Credit Card"
        public decimal TotalPrice { get; set; } // ยอดเงินที่แอดมินคำนวณและเก็บเงินมาจริง

        
    }
}