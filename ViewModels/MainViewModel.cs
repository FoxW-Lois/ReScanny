using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReScanny.Helpers;
using ReScanny.IServices;
using ReScanny.Models;
using System.Collections.ObjectModel;

namespace ReScanny.ViewModels;

public partial class MainViewModel : ObservableObject
{
	private readonly IPhotoStorageService _photoStorageService;
	private readonly ICounterService _counterService;
	private readonly IPdfService _pdfService;
	private readonly IDownloadsService _downloadsService;

	public MainViewModel(IPhotoStorageService photoStorageService, ICounterService counterService, IPdfService pdfService,
		IDownloadsService downloadsService)
	{
		_photoStorageService = photoStorageService;
		_counterService = counterService;
		_pdfService = pdfService;
		_downloadsService = downloadsService;

		Photos.CollectionChanged += (_, _) => OnPhotosChanged();
	}

	#region Properties

	public int PhotoCount => Photos.Count;

	public bool CanTakePhoto => !IsBusy && Photos.Count < AppConstants.MaxPhotos;

	public bool CanPreview => Photos.Count > 0 && !IsBusy;

	public string PhotoCounterText => $"{Photos.Count} / {AppConstants.MaxPhotos}";

	public int NextId => _counterService.NextId;

	public string? LastPdfPath { get; private set; }

	public string NextIdText => $"Prochain ID : {_counterService.NextId}";

	// Reset possible uniquement sans photo en cours et sans traitement
	public bool CanReset => Photos.Count == 0 && !IsBusy;

	#endregion Properties

	#region Observable Properties

	public ObservableCollection<CapturedPhoto> Photos { get; } = [];

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanTakePhoto))]
	public partial bool IsBusy { get; set; }

	[ObservableProperty]
	public partial bool IsPreviewVisible { get; set; }

	[ObservableProperty]
	public partial ImageSource? PreviewImage { get; set; }

	[ObservableProperty]
	public partial string PreviewSummary { get; set; } = string.Empty;

	#endregion Observable Properties

	#region Properties methods

	private void OnPhotosChanged()
	{
		OnPropertyChanged(nameof(PhotoCount));
		OnPropertyChanged(nameof(CanTakePhoto));
		OnPropertyChanged(nameof(CanPreview));
		OnPropertyChanged(nameof(PhotoCounterText));
		OpenPreviewCommand.NotifyCanExecuteChanged();

		OnPropertyChanged(nameof(NextIdText));
		OnPropertyChanged(nameof(CanReset));
		ResetCounterCommand.NotifyCanExecuteChanged();
	}

	partial void OnIsBusyChanged(bool value)
	{
		OnPropertyChanged(nameof(CanPreview));
		OpenPreviewCommand.NotifyCanExecuteChanged();

		OnPropertyChanged(nameof(CanReset));
		ResetCounterCommand.NotifyCanExecuteChanged();
	}

	#endregion Properties methods

	// Appelée par la page après chaque capture (octets JPEG déjà recadrés en 3:4).
	public async Task AddPhotoAsync(byte[] jpegBytes)
	{
		if (Photos.Count >= AppConstants.MaxPhotos)
			return;

		var path = await _photoStorageService.SaveAsync(jpegBytes);
		Photos.Add(new CapturedPhoto { Path = path });
	}

	// Recharge les photos présentes sur le disque (si l'app a été fermée avant l'envoi).
	public void LoadExistingPhotos()
	{
		Photos.Clear();
		foreach (var path in _photoStorageService.GetAll().Take(AppConstants.MaxPhotos))
			Photos.Add(new CapturedPhoto { Path = path });
	}

	[RelayCommand]
	private void DeletePhoto(CapturedPhoto? photo)
	{
		if (photo is null) return;

		_photoStorageService.Delete(photo.Path);
		Photos.Remove(photo);
	}

	[RelayCommand(CanExecute = nameof(CanReset))]
	private async Task ResetCounterAsync()
	{
		var confirmed = await Shell.Current.DisplayAlertAsync(
			"Remettre le compteur à zéro ?",
			$"Prochain ID actuel : {_counterService.NextId}.\n\n" +
			"Le prochain objet photographié aura l'ID 1. Cette action est irréversible.",
			"Remettre à zéro",
			"Annuler"
		);

		if (!confirmed) return;

		_counterService.Reset();
		OnPropertyChanged(nameof(NextIdText));
	}

	[RelayCommand(CanExecute = nameof(CanPreview))]
	private async Task OpenPreviewAsync()
	{
		IsBusy = true;
		try
		{
			// Attribution des IDs dans l'ordre de prise de vue (le compteur n'avance pas ici).
			var id = _counterService.NextId;
			foreach (var photo in Photos)
				photo.AssignedId = id++;

			var items = Photos
				.Select(p => (p.Path, p.AssignedId!.Value))
				.ToList();

			var bytes = await Task.Run(() => PdfPageRenderer.RenderPreview(items));

			PreviewImage = ImageSource.FromStream(() => new MemoryStream(bytes));
			PreviewSummary = items.Count == 1
				? $"Objet n°{items[0].Value}"
				: $"Objets n°{items[0].Value} à {items[^1].Value}";

			IsPreviewVisible = true;
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task ValidatePreviewAsync()
	{
		if (IsBusy || Photos.Count == 0)
			return;

		IsBusy = true;
		try
		{
			var items = Photos
				.Select(p => (p.Path, p.AssignedId!.Value))
				.ToList();

			// Génération + enregistrement local
			LastPdfPath = await _pdfService.CreatePdfAsync(items);

			// Quand PDF enregistré, IDs définitivement consommés.
			_counterService.Advance(items.Count);

			// Copie dans Téléchargements
			var copied = await _downloadsService.CopyPdfAsync(LastPdfPath);

			var mailOpened = true;
			try
			{
				var message = new EmailMessage
				{
					Subject = "PDF Objets trouvés",
					Body = "Ci-joint le PDF des objets trouvés."
				};
				message.Attachments!.Add(new EmailAttachment(LastPdfPath));

				// Rend la main quand l'utilisateur revient dans l'application.
				await Email.Default.ComposeAsync(message);
			}
			catch (FeatureNotSupportedException)
			{
				mailOpened = false;
			}

			// Nettoyage des photos
			ClearAll();

			if (!mailOpened)
			{
				await Shell.Current.DisplayAlertAsync(
					"Aucune application mail",
					copied
						? "Le PDF a été enregistré dans Téléchargements."
						: "Le PDF a été créé mais n'a pas pu être copié dans Téléchargements.",
					"OK"
				);
			}
			else if (!copied)
			{
				await Shell.Current.DisplayAlertAsync(
					"Téléchargements",
					"Le PDF n'a pas pu être copié dans Téléchargements.",
					"OK"
				);
			}
		}
		catch (Exception ex)
		{
			await Shell.Current.DisplayAlertAsync("Erreur", $"Une erreur est survenue : {ex.Message}", "OK");
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private void ClosePreview() => IsPreviewVisible = false;

	// Appelée après l'envoi du mail : supprime toutes les photos, le compteur reste.
	public void ClearAll()
	{
		_photoStorageService.Clear();
		Photos.Clear();
		IsPreviewVisible = false;
	}
}
