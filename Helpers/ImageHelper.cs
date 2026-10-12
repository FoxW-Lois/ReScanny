using SkiaSharp;

namespace ReScanny.Helpers;

public static class ImageHelper
{
	private const int MaxHeight = 2000;

	// Décode l'image, applique l'orientation EXIF, la recadre au centre au ratio demandé (largeur / hauteur) et la réencode en JPEG.
	public static byte[] CropToRatio(Stream source, double ratio, int quality = 85)
	{
		// SKCodec a besoin d'un flux en mémoire et repositionnable.
		using var memory = new MemoryStream();
		source.CopyTo(memory);
		memory.Position = 0;

		using var codec = SKCodec.Create(memory) ?? throw new InvalidOperationException("Image illisible.");
		var origin = codec.EncodedOrigin;

		using var decoded = SKBitmap.Decode(codec) ?? throw new InvalidOperationException("Décodage de l'image impossible.");
		using var oriented = ApplyOrigin(decoded, origin);

		// Zone de recadrage centrée
		float cropW, cropH;
		if ((double)oriented.Width / oriented.Height > ratio)
		{
			cropH = oriented.Height;
			cropW = (float)(cropH * ratio);
		}
		else
		{
			cropW = oriented.Width;
			cropH = (float)(cropW / ratio);
		}

		var left = (oriented.Width - cropW) / 2f;
		var top = (oriented.Height - cropH) / 2f;
		var srcRect = new SKRect(left, top, left + cropW, top + cropH);

		// Réduction éventuelle
		var scale = Math.Min(1f, MaxHeight / cropH);
		var outW = (int)Math.Round(cropW * scale);
		var outH = (int)Math.Round(cropH * scale);

		using var output = new SKBitmap(outW, outH);
		using (var canvas = new SKCanvas(output))
		using (var image = SKImage.FromBitmap(oriented))
		{
			canvas.DrawImage(
				image,
				srcRect,
				new SKRect(0, 0, outW, outH),
				new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear)
			);
		}

		using var outImage = SKImage.FromBitmap(output);
		using var data = outImage.Encode(SKEncodedImageFormat.Jpeg, quality);
		return data.ToArray();
	}

	private static SKBitmap ApplyOrigin(SKBitmap src, SKEncodedOrigin origin) => origin switch
	{
		SKEncodedOrigin.BottomRight => Rotate(src, 180),
		SKEncodedOrigin.RightTop => Rotate(src, 90),
		SKEncodedOrigin.LeftBottom => Rotate(src, 270),
		_ => src.Copy() // TopLeft ou cas non géré : copie pour simplifier la libération
	};

	private static SKBitmap Rotate(SKBitmap src, int degrees)
	{
		var swap = degrees is 90 or 270;
		var dst = new SKBitmap(swap ? src.Height : src.Width, swap ? src.Width : src.Height);

		using var canvas = new SKCanvas(dst);
		canvas.Translate(dst.Width / 2f, dst.Height / 2f);
		canvas.RotateDegrees(degrees);
		canvas.Translate(-src.Width / 2f, -src.Height / 2f);
		SKSamplingOptions sampling = SKSamplingOptions.Default;
		canvas.DrawBitmap(src, 0, 0, sampling);

		return dst;
	}
}
