using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Tests;

[TestClass]
public sealed class SmsPermissionConsentViewModelTests
{
    [TestMethod]
    public async Task RefreshAsync_GrantedStatus_UpdatesStatusAndActions()
    {
        var service = new FakeSmsPermissionService
        {
            CurrentState = SmsPermissionState.Granted
        };
        var viewModel = new SmsPermissionConsentViewModel(service);

        await viewModel.RefreshAsync();

        Assert.AreEqual(SmsPermissionState.Granted, viewModel.PermissionState);
        Assert.AreEqual("SMS access is allowed.", viewModel.StatusText);
        Assert.IsFalse(viewModel.CanRequestPermission);
        Assert.IsTrue(viewModel.CanOpenAppSettings);
        Assert.IsFalse(viewModel.RequestPermissionCommand.CanExecute(null));
        Assert.IsTrue(viewModel.OpenAppSettingsCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task RequestReadPermissionAsync_DeniedThenGranted_RequestsThroughService()
    {
        var service = new FakeSmsPermissionService
        {
            CurrentState = SmsPermissionState.Denied,
            RequestState = SmsPermissionState.Granted
        };
        var viewModel = new SmsPermissionConsentViewModel(service);

        await viewModel.RefreshAsync();
        await viewModel.RequestReadPermissionAsync();

        Assert.AreEqual(1, service.RequestCount);
        Assert.AreEqual(SmsPermissionState.Granted, viewModel.PermissionState);
        Assert.IsFalse(viewModel.CanRequestPermission);
    }

    [TestMethod]
    public async Task RequestReadPermissionAsync_Unsupported_DoesNotRequestPermission()
    {
        var service = new FakeSmsPermissionService
        {
            CurrentState = SmsPermissionState.Unsupported,
            RequestState = SmsPermissionState.Granted
        };
        var viewModel = new SmsPermissionConsentViewModel(service);

        await viewModel.RefreshAsync();
        await viewModel.RequestReadPermissionAsync();

        Assert.AreEqual(0, service.RequestCount);
        Assert.AreEqual(SmsPermissionState.Unsupported, viewModel.PermissionState);
        Assert.AreEqual("SMS access is only available on Android.", viewModel.StatusText);
        Assert.IsFalse(viewModel.CanRequestPermission);
        Assert.IsFalse(viewModel.CanOpenAppSettings);
    }

    [TestMethod]
    public async Task OpenAppSettingsAsync_Supported_OpensSettingsAndRefreshesState()
    {
        var service = new FakeSmsPermissionService
        {
            CurrentState = SmsPermissionState.Denied,
            StateAfterOpeningSettings = SmsPermissionState.Granted
        };
        var viewModel = new SmsPermissionConsentViewModel(service);

        await viewModel.RefreshAsync();
        await viewModel.OpenAppSettingsAsync();

        Assert.AreEqual(1, service.OpenSettingsCount);
        Assert.AreEqual(SmsPermissionState.Granted, viewModel.PermissionState);
    }

    private sealed class FakeSmsPermissionService : ISmsPermissionService
    {
        public SmsPermissionState CurrentState { get; set; } = SmsPermissionState.Unknown;

        public SmsPermissionState RequestState { get; set; } = SmsPermissionState.Denied;

        public SmsPermissionState StateAfterOpeningSettings { get; set; } = SmsPermissionState.Denied;

        public int RequestCount { get; private set; }

        public int OpenSettingsCount { get; private set; }

        public Task<SmsPermissionState> GetReadPermissionStateAsync() => Task.FromResult(CurrentState);

        public Task<SmsPermissionState> RequestReadPermissionAsync()
        {
            RequestCount++;
            CurrentState = RequestState;
            return Task.FromResult(CurrentState);
        }

        public Task OpenAppSettingsAsync()
        {
            OpenSettingsCount++;
            CurrentState = StateAfterOpeningSettings;
            return Task.CompletedTask;
        }
    }
}