using BCrypt.Net;
using Npgsql;

namespace QAsist.Tools;

/// <summary>
/// Utility to generate password hashes and create admin user
/// Run this once to setup the admin user with correct password hash
/// </summary>
public class PasswordHashGenerator
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== QAsist Password Hash Generator ===\n");

        // Generate hash for Admin@123
        var password = "Admin@123";
        var hash = BCrypt.Net.BCrypt.HashPassword(password, 11);

        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"Hash: {hash}");
        Console.WriteLine();

        // Verify the hash
        var isValid = BCrypt.Net.BCrypt.Verify(password, hash);
        Console.WriteLine($"Verification: {isValid}");
        Console.WriteLine();

        Console.WriteLine("Copy this SQL to create admin user:");
        Console.WriteLine("=".PadRight(50, '='));
        Console.WriteLine($@"
DELETE FROM users WHERE email = 'admin@qasist.com';

INSERT INTO users (
    id, email, first_name, last_name, password_hash, 
    role, is_active, created_by, created_at
)
VALUES (
    '00000000-0000-0000-0000-000000000001'::uuid,
    'admin@qasist.com',
    'Super',
    'Admin',
    '{hash}',
    1,
    true,
    '00000000-0000-0000-0000-000000000000'::uuid,
    NOW()
);

SELECT * FROM users WHERE email = 'admin@qasist.com';
");
        Console.WriteLine("=".PadRight(50, '='));
    }
}