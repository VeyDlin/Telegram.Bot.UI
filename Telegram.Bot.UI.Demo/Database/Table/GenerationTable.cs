using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Telegram.Bot.UI.Demo.Database.Tables;


[Index(nameof(userId))]
public class GenerationTable {
    [Key]
    public long id { get; set; }
    public required long userId { get; set; }
    public required TimeSpan waitingTime { get; set; }
    public DateTime create { get; set; } = DateTime.UtcNow;
}