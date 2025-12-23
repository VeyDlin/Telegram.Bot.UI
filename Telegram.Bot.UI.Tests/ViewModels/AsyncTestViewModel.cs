namespace Telegram.Bot.UI.Tests.ViewModels;

public class AsyncTestViewModel {
    public async Task<string> GetValueAsync() {
        await Task.Delay(10);
        return "ASYNC_RESULT";
    }

    public string GetValue() {
        return "SYNC_RESULT";
    }
}
