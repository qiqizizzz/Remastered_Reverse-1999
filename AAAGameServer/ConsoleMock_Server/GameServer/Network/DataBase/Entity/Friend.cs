using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Network.DataBase.Entity
{
    [Table("friends")]
    public class Friend
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("username")]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [Column("friend_username")]
        [MaxLength(50)]
        public string FriendUsername { get; set; }

        [Column("create_time")]
        public DateTime CreateTime { get; set; } // 对应数据库的成为好友时间
    }
}