using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Telegram.Bot.UI.Demo.Database.Tables;


public enum UserRole {
    user = 0,
    admin = 1,
    banned = 2,
    betaTester = 3,
    vip = 4
}


[Index(nameof(telegramId))]
[Index(nameof(registration))]
[Index(nameof(lastBotInvoke))]
public class UserTable {
    [Key]
    public long id { get; set; }
    public long? telegramId { get; set; }
    public UserRole role { get; set; } = UserRole.user;
    public string language { get; set; } = "en";
    public bool acceptLicense { get; set; } = false;
    public DateTime registration { get; set; } = DateTime.UtcNow;
    public DateTime lastBotInvoke { get; set; } = DateTime.UtcNow;


    public bool HasRole(params UserRole[] roles) {
        return roles.Contains(role);
    }
}