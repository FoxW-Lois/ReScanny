using ReScanny.Helpers;
using ReScanny.IServices;
using SkiaSharp;

namespace ReScanny.Services;

public class PdfService : IPdfService
{
	private readonly string _folder;

	public PdfService()
	{
		// Dossier interne à l'app : accessible à Email.ComposeAsync via le FileProvider de MAUI.
		_folder = Path.Combine(FileSystem.AppDataDirectory, "pdfs");
		Directory.CreateDirectory(_folder);
	}

	public Task<string> CreatePdfAsync(IReadOnlyList<(string Path, int Id)> items)
	{
		if (items.Count == 0)
			throw new ArgumentException("Aucune photo à exporter.", nameof(items));

		return Task.Run(() =>
		{
			var range = items.Count == 1
				? $"{items[0].Id}"
				: $"{items[0].Id}-{items[^1].Id}";

			var path = Path.Combine(_folder, $"Objets_{range}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

			using (var stream = File.Create(path))
			using (var document = SKDocument.CreatePdf(stream))
			{
				// Le canvas appartient au document : ne pas le libérer soi-même.
				var canvas = document.BeginPage(PdfPageRenderer.PageWidth, PdfPageRenderer.PageHeight);
				PdfPageRenderer.DrawPage(canvas, items);

				document.EndPage();
				document.Close();
			}

			return path;
		});
	}
}
