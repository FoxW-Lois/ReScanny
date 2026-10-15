using SkiaSharp;

namespace ReScanny.Helpers;

public static class ImageHelper
{
	private const int MaxHeight = 1500;

	// Décode l'image, applique l'orientation EXIF, la recadre au centre au ratio demandé (largeur / hauteur) et la réencode en JPEG.
	public static byte[] CropToRatio(Stream source, double ratio, int quality = 85)
	{
		// SKCodec a besoin d'un flux en mémoire et repositionnable.
		using var memory = new MemoryStream();
		source.CopyTo(memory);
		memory.Position = 0;

		using var codec = SKCodec.Create(memory) ?? throw new InvalidOperationException("Image illisible.");
		var origin = codec.EncodedOrigin;

		var swap = origin is SKEncodedOrigin.RightTop or SKEncodedOrigin.LeftBottom
								or SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightBottom;

		// Dimensions orientées de l'image complète
		var fullW = swap ? codec.Info.Height : codec.Info.Width;
		var fullH = swap ? codec.Info.Width : codec.Info.Height;

		// Hauteur du recadrage 3:4 en pleine résolution
		var fullCropH = (double)fullW / fullH > ratio ? fullH : fullW / ratio;

		// Échelle de décodage : on ne descend pas sous MaxHeight après recadrage
		var desiredScale = (float)Math.Min(1.0, MaxHeight / fullCropH);
		var scaled = codec.GetScaledDimensions(desiredScale);

		var info = new SKImageInfo(scaled.Width, scaled.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
		using var decoded = new SKBitmap(info);

		var result = codec.GetPixels(info, decoded.GetPixels());
		if (result is not (SKCodecResult.Success or SKCodecResult.IncompleteInput))
			throw new InvalidOperationException($"Décodage impossible : {result}");

		// Dimensions orientées de l'image décodée
		var ow = swap ? decoded.Height : decoded.Width;
		var oh = swap ? decoded.Width : decoded.Height;

		// Zone de recadrage centrée (espace orienté)
		float cropW, cropH;
		if ((double)ow / oh > ratio)
		{
			cropH = oh;
			cropW = (float)(cropH * ratio);
		}
		else
		{
			cropW = ow;
			cropH = (float)(cropW / ratio);
		}

		var left = (ow - cropW) / 2f;
		var top = (oh - cropH) / 2f;

		var k = Math.Min(1f, MaxHeight / cropH);
		var outW = (int)Math.Round(cropW * k);
		var outH = (int)Math.Round(cropH * k);

		using var output = new SKBitmap(outW, outH);
		using (var canvas = new SKCanvas(output))
		using (var image = SKImage.FromBitmap(decoded))
		{
			// Espace orienté -> pixels de sortie (recadrage + réduction)
			canvas.Scale(outW / cropW, outH / cropH);
			canvas.Translate(-left, -top);

			// Rotation EXIF appliquée au dessin de l'image source
			switch (origin)
			{
				case SKEncodedOrigin.BottomRight:
					canvas.Translate(ow, oh);
					canvas.RotateDegrees(180);
					break;

				case SKEncodedOrigin.RightTop:
					canvas.Translate(ow, 0);
					canvas.RotateDegrees(90);
					break;

				case SKEncodedOrigin.LeftBottom:
					canvas.Translate(0, oh);
					canvas.RotateDegrees(270);
					break;
			}

			canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
		}

		using var outImage = SKImage.FromBitmap(output);
		using var data = outImage.Encode(SKEncodedImageFormat.Jpeg, quality);
		return data.ToArray();
	}

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
