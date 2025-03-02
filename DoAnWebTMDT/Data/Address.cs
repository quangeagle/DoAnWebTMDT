using System;
using System.Collections.Generic;

namespace DoAnWebTMDT.Data;

public partial class Address
{
    public int AddressId { get; set; }

    public int AccountId { get; set; }

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string AddressDetail { get; set; } = null!;

    public bool? IsDefault { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
