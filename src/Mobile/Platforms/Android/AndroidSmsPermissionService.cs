using HasbeMaal.Presentation.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace HasbeMaal.Mobile.Services;

public sealed class AndroidSmsPermissionService : ISmsPermissionService
{
    public async Task<SmsPermissionState> GetReadPermissionStateAsync()
    {
        var status = await Permissions.CheckStatusAsync<ReadSmsPermission>();
        return ToState(status);
    }

    public async Task<SmsPermissionState> RequestReadPermissionAsync()
    {
        var status = await Permissions.RequestAsync<ReadSmsPermission>();
        return ToState(status);
    }

    public Task OpenAppSettingsAsync()
    {
        AppInfo.Current.ShowSettingsUI();
        return Task.CompletedTask;
    }

    private static SmsPermissionState ToState(PermissionStatus status) => status switch
    {
        PermissionStatus.Granted => SmsPermissionState.Granted,
        PermissionStatus.Unknown => SmsPermissionState.Unknown,
        _ => SmsPermissionState.Denied
    };

    private sealed class ReadSmsPermission : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        [
            (global::Android.Manifest.Permission.ReadSms, true)
        ];
    }
}