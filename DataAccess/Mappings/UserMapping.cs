using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Mappings;

public class UserMapping : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasData(
            new
            {
                Id = 1L,
                IsBlocked = false,
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                EmailConfirmed = false,
                PasswordHash =
                    "AQAAAAEAACcQAAAAENn3RdGmZi1mnC1K+5imUc7JVHPEComSttTB4Ptj1PwpLC/ZLrgWCpVmK3wd2R3MHQ==",
                SecurityStamp = "VSL3YXXEZ4TCYMKDLDDMKZPJ52S55AWB",
                ConcurrencyStamp = "6cb8a2a3-ad07-4dfe-88a8-ef0a8a8a10a0",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                AccountId = 1L
            },
            new
            {
                Id = 2L,
                IsBlocked = false,
                UserName = "guest",
                NormalizedUserName = "GUEST",
                EmailConfirmed = false,
                PasswordHash =
                    "AQAAAAEAACcQAAAAEMhgkd+hBV/VpW5TU6FXehBOAhJvyNc/VsMoiLzSwpIAvlYgyMhZvJnreZOeTombSQ==",
                SecurityStamp = "HLSI4JFSNHUEGVSUE2ZXAJFQA4LL2J4Z",
                ConcurrencyStamp = "976e2e7c-75b7-4d16-9920-de88481c2f0c",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                AccountId = 2L
            });
    }
}