using System;
using System.Collections.Generic;

namespace DoAnWebTMDT.Data;

public partial class GuestLikeList
{
    public int GuestLikeId { get; set; }

    public string SessionId { get; set; } = null!;

    public int ProductId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
