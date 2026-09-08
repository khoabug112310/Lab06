using Microsoft.AspNetCore.Identity;

var hasher = new PasswordHasher<object>();

var hasher1 = hasher.HashPassword(null!, "123");
var hasher2 = hasher.HashPassword(null!, "345");

Console.WriteLine($"Hasher1: {hasher1}");
Console.WriteLine("\n");
Console.WriteLine($"Hasher2: {hasher2}");