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

		var startTask = Camera.StartCameraPreview(CancellationToken.None);
		var done = await Task.WhenAny(startTask, Task.Delay(8000));
		if (done != startTask)
		{
			await DisplayAlertAsync("Caméra",
				"La caméra ne répond pas. Veuillez complètement fermer l'application et la rouvrir.", "OK");
		}
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
			var stream = await CaptureWithTimeoutAsync();

			if (stream is null)
			{
				await RestartCameraAsync();
				await DisplayAlertAsync("Caméra",
					"La mise au point a échoué, la photo n'a pas été prise. Veuillez réessayer.", "OK");
				return;
			}

			await using (stream)
			{
				// Recadrage 3:4 hors du thread UI
				var bytes = await Task.Run(() =>
					ImageHelper.CropToRatio(stream, AppConstants.PhotoAspectRatio));

				await _viewModel.AddPhotoAsync(bytes);
			}
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

	private const int CaptureTimeoutMs = 8000;

	// Retourne null si la capture ne se termine pas dans le délai.
	private async Task<Stream?> CaptureWithTimeoutAsync()
	{
		using var cts = new CancellationTokenSource();
		var captureTask = Camera.CaptureImage(cts.Token);

		var finished = await Task.WhenAny(captureTask, Task.Delay(CaptureTimeoutMs));
		if (finished == captureTask)
			return await captureTask;

		// Délai dépassé : on tente d'annuler, et on libère le flux s'il arrive plus tard.
		cts.Cancel();
		_ = captureTask.ContinueWith(
			t => { if (t.IsCompletedSuccessfully) t.Result.Dispose(); },
			TaskScheduler.Default);

		return null;
	}

	private async Task RestartCameraAsync()
	{
		try
		{
			Camera.StopCameraPreview();
			await Task.Delay(300);
			await Camera.StartCameraPreview(CancellationToken.None);
		}
		catch
		{
			// Si la caméra ne repart pas, l'utilisateur pourra relancer l'application.
		}
	}
}
