using System;
using System.Collections.Generic;

namespace Lab06WebApi.Core.Entities;

public partial class Transfer
{
    public int Id { get; set; }

    public int FromAccountId { get; set; }

    public int ToAccountId { get; set; }

    public decimal Amount { get; set; }

    public DateTime TransferDate { get; set; }

    public virtual Account FromAccount { get; set; } = null!;

    public virtual Account ToAccount { get; set; } = null!;
}
