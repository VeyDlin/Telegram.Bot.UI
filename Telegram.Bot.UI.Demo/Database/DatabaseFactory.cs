namespace Telegram.Bot.UI.Demo.Database;


public class DatabaseFactory {
    public AppDatabaseContext Context() {
        return new AppDatabaseContext();
    }
}