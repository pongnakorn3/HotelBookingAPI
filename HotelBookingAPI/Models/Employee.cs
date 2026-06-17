using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingAPI.Models
{
    [Table("employees")] // บังคับชี้ไปที่ตารางพิมพ์เล็กใน pgAdmin
    public class Employee
    {
        [Key]
        [Column("employeeid")] // 🎯 แก้จุดนี้: บังคับให้ระบบมองเห็นเป็นคอลัมน์พิมพ์เล็กตรงกับฐานข้อมูล
        public int EmployeeId { get; set; }

        [Column("firstname")] // 🎯 แมปชื่อพิมพ์เล็กให้ตรงกับ PostgreSQL
        public string FirstName { get; set; } = string.Empty;

        [Column("lastname")] // 🎯 แมปชื่อพิมพ์เล็กให้ตรงกับ PostgreSQL
        public string LastName { get; set; } = string.Empty;

        [Column("email")] // 🎯 แมปชื่อพิมพ์เล็กให้ตรงกับ PostgreSQL
        public string Email { get; set; } = string.Empty;

        [Column("passwordhash")] // 🎯 แมปชื่อพิมพ์เล็กให้ตรงกับ PostgreSQL
        public string PasswordHash { get; set; } = string.Empty;

        [Column("role")] // 🎯 แมปชื่อพิมพ์เล็กให้ตรงกับ PostgreSQL
        public string Role { get; set; } = string.Empty;

        [Column("isactive")] // 🎯 แมปชื่อพิมพ์เล็กให้ตรงกับ PostgreSQL
        public bool IsActive { get; set; }
    }
}