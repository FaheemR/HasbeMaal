using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Services;

public sealed class UnsupportedSmsPermissionService : ISmsPermissionService
{
    public Task<SmsPermissionState> GetReadPermissionStateAsync() => Task.FromResult(SmsPermissionState.Unsupported);

    public Task<SmsPermissionState> RequestReadPermissionAsync() => Task.FromResult(SmsPermissionState.Unsupported);

    public Task OpenAppSettingsAsync() => Task.CompletedTask;
}