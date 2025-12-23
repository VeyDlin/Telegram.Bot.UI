using Telegram.Bot.UI.Demo.Database.Tables;

namespace Telegram.Bot.UI.Demo.Database;


public static class Utils {
    public static UserTable GetUserOrCreate(this AppDatabaseContext db, long telegramId, Func<UserTable> create) {
        if (db.GetUser(telegramId) is UserTable user) {
            return user;
        }
        user = create.Invoke();
        db.userTable.Add(user);
        return user;
    }


    public static UserTable? GetUser(this AppDatabaseContext db, long telegramId) {
        return db.userTable.FirstOrDefault(u => u.telegramId == telegramId);
    }
}