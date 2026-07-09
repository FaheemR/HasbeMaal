using System.Windows.Input;

namespace HasbeMaal.Presentation.ViewModels;

public sealed class SmsPermissionConsentViewModel : ViewModelBase
{
    private readonly ISmsPermissionService smsPermissionService;
    private SmsPermissionState permissionState = SmsPermissionState.Unknown;
    private bool isBusy;

    public SmsPermissionConsentViewModel(ISmsPermissionService smsPermissionService)
    {
        this.smsPermissionService = smsPermissionService ?? throw new ArgumentNullException(nameof(smsPermissionService));
        RequestPermissionCommand = new RelayCommand(
            () => _ = RequestReadPermissionAsync(),
            () => CanRequestPermission);
        OpenAppSettingsCommand = new RelayCommand(
            () => _ = OpenAppSettingsAsync(),
            () => CanOpenAppSettings);
    }

    public SmsPermissionState PermissionState
    {
        get => permissionState;
        private set
        {
            if (SetProperty(ref permissionState, value))
            {
                OnPermissionStateChanged();
            }
        }
    }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                OnPropertyChanged(nameof(CanRequestPermission));
                OnPropertyChanged(nameof(CanOpenAppSettings));
                RaiseCommandStateChanged();
            }
        }
    }

    public bool IsSupported => PermissionState != SmsPermissionState.Unsupported;

    public bool CanRequestPermission => IsSupported && PermissionState != SmsPermissionState.Granted && !IsBusy;

    public bool CanOpenAppSettings => IsSupported && !IsBusy;

    public string StatusText => PermissionState switch
    {
        SmsPermissionState.Granted => "SMS access is allowed.",
        SmsPermissionState.Denied => "SMS access is not allowed.",
        SmsPermissionState.Unsupported => "SMS access is only available on Android.",
        _ => "SMS access status has not been checked."
    };

    public ICommand RequestPermissionCommand { get; }

    public ICommand OpenAppSettingsCommand { get; }

    public async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            PermissionState = await smsPermissionService.GetReadPermissionStateAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RequestReadPermissionAsync()
    {
        if (!CanRequestPermission)
        {
            return;
        }

        IsBusy = true;
        try
        {
            PermissionState = await smsPermissionService.RequestReadPermissionAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task OpenAppSettingsAsync()
    {
        if (!CanOpenAppSettings)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await smsPermissionService.OpenAppSettingsAsync();
            PermissionState = await smsPermissionService.GetReadPermissionStateAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnPermissionStateChanged()
    {
        OnPropertyChanged(nameof(IsSupported));
        OnPropertyChanged(nameof(CanRequestPermission));
        OnPropertyChanged(nameof(CanOpenAppSettings));
        OnPropertyChanged(nameof(StatusText));
        RaiseCommandStateChanged();
    }

    private void RaiseCommandStateChanged()
    {
        ((RelayCommand)RequestPermissionCommand).RaiseCanExecuteChanged();
        ((RelayCommand)OpenAppSettingsCommand).RaiseCanExecuteChanged();
    }
}