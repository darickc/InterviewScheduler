using MudBlazor;

namespace InterviewScheduler.Client.Services;

public interface IAppNotificationService
{
    Task ShowErrorAsync(string title, string message);
    Task ShowInfoAsync(string title, string message);
    Task<bool> ConfirmAsync(string title, string message, string confirmText = "Confirm", string cancelText = "Cancel");
    void ShowSuccess(string message);
    void ShowInfo(string message);
    void ShowWarning(string message);
}

public class AppNotificationService : IAppNotificationService
{
    private readonly IDialogService _dialogs;
    private readonly ISnackbar _snackbar;

    private static readonly DialogOptions MessageOptions = new()
    {
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        CloseButton = true,
        BackdropClick = false
    };

    public AppNotificationService(IDialogService dialogs, ISnackbar snackbar)
    {
        _dialogs = dialogs;
        _snackbar = snackbar;
    }

    public Task ShowErrorAsync(string title, string message)
        => ShowMessageAsync(title, message);

    public Task ShowInfoAsync(string title, string message)
        => ShowMessageAsync(title, message);

    public async Task<bool> ConfirmAsync(string title, string message, string confirmText = "Confirm", string cancelText = "Cancel")
    {
        var result = await _dialogs.ShowMessageBoxAsync(
            title,
            message,
            yesText: confirmText,
            noText: null,
            cancelText: cancelText,
            options: MessageOptions);

        return result == true;
    }

    public void ShowSuccess(string message) => _snackbar.Add(message, Severity.Success);

    public void ShowInfo(string message) => _snackbar.Add(message, Severity.Info);

    public void ShowWarning(string message) => _snackbar.Add(message, Severity.Warning);

    private async Task ShowMessageAsync(string title, string message)
    {
        await _dialogs.ShowMessageBoxAsync(
            title,
            message,
            yesText: "OK",
            noText: null,
            cancelText: null,
            options: MessageOptions);
    }
}
