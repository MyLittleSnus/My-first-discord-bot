using System;
using Microsoft.EntityFrameworkCore;
using BotConfiguration.Entities;

namespace BotConfiguration.Context
{
    public class BotDbContext : DbContext
    {
        public DbSet<Participant> Participants { get; set; }

        public BotDbContext(DbContextOptions options) : base(options) { }
    }
}