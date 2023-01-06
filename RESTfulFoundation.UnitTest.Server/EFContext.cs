using Microsoft.EntityFrameworkCore;
using RESTfulFoundation.UnitTest.Server.Entities;
using System;

namespace RESTfulFoundation.UnitTest.Server
{
    public class EFContext : DbContext
    {

        public EFContext(DbContextOptions<EFContext>
            options) : base(options)
        {

        }

        public DbSet<Player> Players { get; set; }
    }
}
