using Microsoft.EntityFrameworkCore;
using Telegram.Bot.UI.Demo.Database.Tables;

namespace Telegram.Bot.UI.Demo.Database;


public class AppDatabaseContext : DbContext {
    public DbSet<GenerationTable> generationTable { get; set; }
    public DbSet<UserTable> userTable { get; set; }

    public bool EnsureCreated() => Database.EnsureCreated();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        optionsBuilder.UseSqlite($"Data Source=db.sqllite");
    }
}