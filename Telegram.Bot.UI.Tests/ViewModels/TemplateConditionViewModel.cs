namespace Telegram.Bot.UI.Tests.ViewModels;

/// <summary>
/// ViewModel for testing template v-if conditions
/// </summary>
public class TemplateConditionViewModel {
    public bool isLoading { get; set; } = false;
    public bool hasResult { get; set; } = false;
    public bool hasError { get; set; } = false;
    public string? resultText { get; set; }
    public string? errorText { get; set; }

    public void StartLoading() {
        isLoading = true;
        hasResult = false;
        hasError = false;
    }

    public void SetResult(string text) {
        isLoading = false;
        hasResult = true;
        hasError = false;
        resultText = text;
    }

    public void SetError(string text) {
        isLoading = false;
        hasResult = false;
        hasError = true;
        errorText = text;
    }

    public void Reset() {
        isLoading = false;
        hasResult = false;
        hasError = false;
        resultText = null;
        errorText = null;
    }
}
