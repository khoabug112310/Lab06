using System;
using System.Collections.Generic;

namespace Lab06WebApi.Core.Entities;

public partial class Account
{
    public int Id { get; set; }

    public string AccountNumber { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public decimal Balance { get; set; }

    public string Email { get; set; } = null!;

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public virtual ICollection<Transfer> TransferFromAccounts { get; set; } = new List<Transfer>();

    public virtual ICollection<Transfer> TransferToAccounts { get; set; } = new List<Transfer>();
}
