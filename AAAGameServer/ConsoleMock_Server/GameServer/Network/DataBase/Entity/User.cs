using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Network.DataBase.Entity
{
    [Table("users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set;  }

        [Required]
        [Column("Username")]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [Column("Password")]
        [MaxLength(255)]
        public string Password { get; set; }

        [Column("Is_banned")]
        public bool Is_banned { get; set; }

        [Column("register_time")]
        public DateTime RegisterTime { get; set; }

        [Column("last_login_time")]
        public DateTime? LastLoginTime { get; set; }
    }
}
