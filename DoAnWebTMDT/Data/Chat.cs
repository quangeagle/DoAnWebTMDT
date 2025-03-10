using System;
using System.Collections.Generic;

namespace DoAnWebTMDT.Data;

public partial class Chat
{
    public int ChatId { get; set; }

    public int SenderId { get; set; }

    public int ReceiverId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime? SentAt { get; set; }

    public bool? IsRead { get; set; }

    public Guid? ChatRoomId { get; set; }

    public virtual Account Receiver { get; set; } = null!;

    public virtual Account Sender { get; set; } = null!;
}
