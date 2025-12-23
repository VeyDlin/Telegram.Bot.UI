using Telegram.Bot.UI;

namespace Telegram.Bot.UI.Tests.ViewModels;

public class CounterViewModel : IPropsReceiver {
    public int Count { get; set; } = 0;

    public void ReceiveProps(Dictionary<string, object?> props) {
        if (props.TryGetValue("initialCount", out var value)) {
            if (value is int intVal) {
                Count = intVal;
            } else if (value is long longVal) {
                Count = (int)longVal;
            } else if (int.TryParse(value?.ToString(), out var parsed)) {
                Count = parsed;
            }
        }
    }

    public void Increment() {
        Count++;
    }

    public void Decrement() {
        Count--;
    }

    public void Reset() {
        Count = 0;
    }

    public string GetStatus() {
        return Count switch {
            0 => "Zero",
            > 0 => "Positive",
            < 0 => "Negative"
        };
    }
}
