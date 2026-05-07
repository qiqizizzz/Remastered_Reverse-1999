using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Network.DataBase.Entity
{
    [Table("chat_messages")]
    public class ChatMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("sender_name")]
        [MaxLength(50)]
        public string SenderName { get; set; }

        [Required]
        [Column("receiver_name")]
        [MaxLength(50)]
        public string ReceiverName { get; set; }

        [Required]
        [Column("content")]
        [MaxLength(1000)] // 聊天内容长度限制，可以根据需求调大
        public string Content { get; set; }

        [Column("send_time")]
        public long SendTime { get; set; } // 使用 long 存储时间戳(毫秒)，和 Protobuf 里定义的保持一致，方便直接转发
    }
}