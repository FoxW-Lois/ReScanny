using SkiaSharp;

namespace ReScanny.Helpers;

public static class PdfPageRenderer
{
	// A4 en points (1 pt = 1/72 pouce)
	public const float PageWidth = 595f;
	public const float PageHeight = 842f;

	private const float Margin = 20f;
	private const float Gap = 10f;
	private const int Columns = 3;

	// Dessine la page (grille de photos + ID blancs à contour noir) sur un canvas de 595 x 842 unités. Utilisé pour la preview et pour le PDF.
	public static void DrawPage(SKCanvas canvas, IReadOnlyList<(string Path, int Id)> items)
	{
		canvas.Clear(SKColors.White);

		var cellW = (PageWidth - 2 * Margin - (Columns - 1) * Gap) / Columns;
		var cellH = cellW / (float)AppConstants.PhotoAspectRatio;

		using var font = new SKFont(SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Bold), cellH * 0.11f);

		using var fillPaint = new SKPaint
		{
			Color = SKColors.White,
			IsAntialias = true,
			Style = SKPaintStyle.Fill
		};

		using var outlinePaint = new SKPaint
		{
			Color = SKColors.Black,
			IsAntialias = true,
			Style = SKPaintStyle.Stroke,
			StrokeWidth = font.Size * 0.16f,
			StrokeJoin = SKStrokeJoin.Round
		};

		var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

		canvas.DrawText("Objets trouvés", PageWidth / 2f, Margin + font.Size, SKTextAlign.Center, font, outlinePaint);

		for (var i = 0; i < items.Count && i < AppConstants.MaxPhotos; i++)
		{
			var (path, id) = items[i];
			var col = i % Columns;
			var row = i / Columns;

			var left = Margin + col * (cellW + Gap);
			var top = Margin + row * (cellH + Gap);
			var dest = new SKRect(left, top, left + cellW, top + cellH);

			using var image = SKImage.FromEncodedData(path);
			if (image is null)
				continue;

			canvas.DrawImage(
				image,
				new SKRect(0, 0, image.Width, image.Height),
				dest,
				sampling
			);

			// ID en haut à gauche : contour noir d'abord, remplissage blanc par-dessus
			var text = id.ToString();
			var x = dest.Left + 8f;
			var y = dest.Top + 8f + font.Size * 0.8f;

			canvas.DrawText(text, x, y, SKTextAlign.Left, font, outlinePaint);
			canvas.DrawText(text, x, y, SKTextAlign.Left, font, fillPaint);
		}
	}

	// Rend la page en JPEG pour la preview (scale 2 = 1190 x 1684 px).
	public static byte[] RenderPreview(IReadOnlyList<(string Path, int Id)> items, float scale = 2f)
	{
		var width = (int)(PageWidth * scale);
		var height = (int)(PageHeight * scale);

		using var bitmap = new SKBitmap(width, height);
		using (var canvas = new SKCanvas(bitmap))
		{
			canvas.Scale(scale);
			DrawPage(canvas, items);
		}

		using var image = SKImage.FromBitmap(bitmap);
		using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
		return data.ToArray();
	}
}
