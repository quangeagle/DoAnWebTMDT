using System;
using System.Collections.Generic;

namespace DoAnWebTMDT.Data;

public partial class GuestCart
{
    public int GuestCartId { get; set; }

    public string SessionId { get; set; } = null!;

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
