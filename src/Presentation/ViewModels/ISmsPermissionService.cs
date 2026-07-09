namespace HasbeMaal.Presentation.ViewModels;

public interface ISmsPermissionService
{
    Task<SmsPermissionState> GetReadPermissionStateAsync();

    Task<SmsPermissionState> RequestReadPermissionAsync();

    Task OpenAppSettingsAsync();
}