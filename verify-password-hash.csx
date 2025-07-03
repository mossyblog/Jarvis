#!/usr/bin/env dotnet-script
#r "nuget: BCrypt.Net-Next, 4.0.3"

using BCrypt.Net;

var password = "test123";
var storedHash = "$2a$12$QnOKnn.PumrtVscPkO3C.ONHR/5NANzEbqMoLQOyUFhQMhynyVoe.";

Console.WriteLine($"Password: {password}");
Console.WriteLine($"Stored Hash: {storedHash}");

var isValid = BCrypt.Net.BCrypt.Verify(password, storedHash);
Console.WriteLine($"Password verification result: {isValid}");

// Also generate a new hash to compare
var newHash = BCrypt.Net.BCrypt.HashPassword(password, 12);
Console.WriteLine($"New hash for comparison: {newHash}");