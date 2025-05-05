using Microsoft.EntityFrameworkCore;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Persistence.Seed;

public static class DataSeeder
{
    public static void SeedInitialData(TakeServusDbContext context)
    {
        // Ensure the database is created
        context.Database.EnsureCreated();
        if (!context.Users.Any())
        {
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Admin User",
                Email = "mustafa.unal@takeservus.com",
                PhoneNumber = "+905382725555",
                Role = "Admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Ab!23456"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            var users = new List<User>
            {
                //adminUser,
                new User
                {
                    Id = Guid.NewGuid(),
                    FullName = "Owner User",
                    Email = "owner@takeservus.com",
                    PhoneNumber = "+905382725556",
                    Role = "Owner",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("owner123"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    FullName = "Dispatcher User",
                    Email = "dispatcher@takeservus.com",
                    PhoneNumber = "+905382725557",
                    Role = "Dispatcher",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("dispatcher123"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    FullName = "Technician User",
                    Email = "technician@takeservus.com",
                    PhoneNumber = "+905382725558",
                    Role = "Technician",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("technician123"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                }
            };

            context.Users.AddRange(users);
            context.SaveChanges();
        }

        if (!context.Technicians.Any())
        {
            var technician = new Technician
            {
                Id = Guid.NewGuid(),
                IsAvailable = true,
                UserId = context.Users.First(u => u.Role == "Technician").Id,
                CurrentLatitude = null,
                CurrentLongitude = null,
            };

            context.Technicians.Add(technician);
            context.SaveChanges();
        }
    }
}