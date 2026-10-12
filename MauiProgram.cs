using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using ReScanny.IServices;
using ReScanny.Services;
using ReScanny.ViewModels;
using ReScanny.Views;

namespace ReScanny
{
	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.UseMauiCommunityToolkit()
				.UseMauiCommunityToolkitCamera()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				});

			builder.Services.AddSingleton<ICounterService, CounterService>();
			builder.Services.AddSingleton<IPhotoStorageService, PhotoStorageService>();
			builder.Services.AddSingleton<IPdfService, PdfService>();
			builder.Services.AddSingleton<IDownloadsService, DownloadsService>();

			builder.Services.AddSingleton<MainPage>();
			builder.Services.AddSingleton<MainViewModel>();

#if DEBUG
			builder.Logging.AddDebug();
#endif

			return builder.Build();
		}
	}
}
