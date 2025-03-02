using System;
using System.Collections.Generic;

namespace DoAnWebTMDT.Data;

public partial class Order
{
    public int OrderId { get; set; }

    public int? AccountId { get; set; }

    public string? GuestEmail { get; set; }

    public string? GuestPhone { get; set; }

    public decimal TotalAmount { get; set; }

    public string? OrderStatus { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? AddressId { get; set; }

    public virtual Account? Account { get; set; }

    public virtual Address? Address { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
