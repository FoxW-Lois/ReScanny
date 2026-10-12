using ReScanny.Helpers;
using ReScanny.ViewModels;

namespace ReScanny.Views;

public partial class MainPage : ContentPage
{
	private readonly MainViewModel _viewModel;
	private bool _initialized;

	public MainPage(MainViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (!_initialized)
		{
			_viewModel.LoadExistingPhotos();
			_initialized = true;
		}

		var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
		if (status != PermissionStatus.Granted)
			status = await Permissions.RequestAsync<Permissions.Camera>();

		if (status != PermissionStatus.Granted)
		{
			await DisplayAlertAsync("Caméra", "L'accès à la caméra est nécessaire pour prendre des photos.", "OK");
			return;
		}

		await Camera.StartCameraPreview(CancellationToken.None);
	}

	protected override void OnDisappearing()
	{
		Camera.StopCameraPreview();
		base.OnDisappearing();
	}

	// Force le cadre de la caméra au ratio 3:4, centré dans la zone disponible.
	private void OnCameraHostSizeChanged(object? sender, EventArgs e)
	{
		if (CameraHost.Width <= 0 || CameraHost.Height <= 0)
			return;

		var frameWidth = Math.Min(CameraHost.Width, CameraHost.Height * AppConstants.PhotoAspectRatio);

		CameraFrame.WidthRequest = frameWidth;
		CameraFrame.HeightRequest = frameWidth / AppConstants.PhotoAspectRatio;
	}

	private async void OnCaptureClicked(object? sender, EventArgs e)
	{
		if (!_viewModel.CanTakePhoto)
			return;

		_viewModel.IsBusy = true;
		try
		{
			// Capture (flux JPEG)
			await using var stream = await Camera.CaptureImage(CancellationToken.None);

			// Recadrage 3:4 hors du thread UI
			var bytes = await Task.Run(() =>
				ImageHelper.CropToRatio(stream, AppConstants.PhotoAspectRatio));

			await _viewModel.AddPhotoAsync(bytes);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Erreur", $"La photo n'a pas pu être prise : {ex.Message}", "OK");
		}
		finally
		{
			_viewModel.IsBusy = false;
		}
	}
}
