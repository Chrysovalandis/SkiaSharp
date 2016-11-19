﻿using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SkiaSharp.Tests
{
	[TestFixture]
	public class SKCodecTest : SKTest
	{
		[Test]
		public void MinBufferedBytesNeededHasAValue ()
		{
			Assert.IsTrue (SKCodec.MinBufferedBytesNeeded > 0);
		}

		[Test]
		public void CanCreateStreamCodec ()
		{
			var stream = new SKFileStream (Path.Combine (PathToImages, "color-wheel.png"));
			using (var codec = SKCodec.Create (stream)) {
				Assert.AreEqual (SKEncodedFormat.Png, codec.EncodedFormat);
				Assert.AreEqual (128, codec.Info.Width);
				Assert.AreEqual (128, codec.Info.Height);
				Assert.AreEqual (SKAlphaType.Unpremul, codec.Info.AlphaType);
				if (IsUnix) {
					Assert.AreEqual (SKColorType.Rgba8888, codec.Info.ColorType);
				} else {
					Assert.AreEqual (SKColorType.Bgra8888, codec.Info.ColorType);
				}
			}
		}

		[Test]
		public void CanGetPixels ()
		{
			var stream = new SKFileStream (Path.Combine (PathToImages, "baboon.png"));
			using (var codec = SKCodec.Create (stream)) {
				var pixels = codec.Pixels;
				Assert.AreEqual (codec.Info.BytesSize, pixels.Length);
			}
		}

		[Test]
		public void DecodePartialImage ()
		{
			// read the data here, so we can fake a throttle/download
			var path = Path.Combine (PathToImages, "baboon.png");
			var fileData = File.ReadAllBytes (path);
			SKColor[] correctBytes;
			using (var bitmap = SKBitmap.Decode (path)) {
				correctBytes = bitmap.Pixels;
			}

			int offset = 0;
			int maxCount = 1024 * 4;

			// the "download" stream needs some data
			var downloadStream = new MemoryStream ();
			downloadStream.Write (fileData, offset, maxCount);
			downloadStream.Position -= maxCount;
			offset += maxCount;

			using (var codec = SKCodec.Create (new SKManagedStream (downloadStream))) {
				var info = new SKImageInfo (codec.Info.Width, codec.Info.Height);

				// the bitmap to be decoded
				using (var incremental = new SKBitmap (info)) {

					// start decoding
					IntPtr length;
					var pixels = incremental.GetPixels (out length);
					var result = codec.StartIncrementalDecode (info, pixels, info.RowBytes);

					// make sure the start was successful
					Assert.AreEqual (SKCodecResult.Success, result);
					result = SKCodecResult.IncompleteInput;

					while (result == SKCodecResult.IncompleteInput) {
						// decode the rest
						int rowsDecoded = 0;
						result = codec.IncrementalDecode (out rowsDecoded);

						// write some more data to the stream
						maxCount = Math.Min (maxCount, fileData.Length - offset);
						downloadStream.Write (fileData, offset, maxCount);
						downloadStream.Position -= maxCount;
						offset += maxCount;
					}

					// compare to original
					CollectionAssert.AreEqual (correctBytes, incremental.Pixels);
				}
			}
		}

		[Test]
		public void BitmapDecodesCorrectly ()
		{
			byte[] codecPixels;
			byte[] bitmapPixels;

			using (var codec = SKCodec.Create (new SKFileStream (Path.Combine (PathToImages, "baboon.png")))) {
				codecPixels = codec.Pixels;
			}

			using (var bitmap = SKBitmap.Decode (Path.Combine (PathToImages, "baboon.png"))) {
				bitmapPixels = bitmap.Bytes;
			}

			CollectionAssert.AreEqual (codecPixels, bitmapPixels);
		}

		[Test]
		public void BitmapDecodesCorrectlyWithManagedStream ()
		{
			byte[] codecPixels;
			byte[] bitmapPixels;

			var stream = File.OpenRead (Path.Combine(PathToImages, "baboon.png"));
			using (var codec = SKCodec.Create (new SKManagedStream (stream))) {
				codecPixels = codec.Pixels;
			}

			using (var bitmap = SKBitmap.Decode (Path.Combine (PathToImages, "baboon.png"))) {
				bitmapPixels = bitmap.Bytes;
			}

			CollectionAssert.AreEqual (codecPixels, bitmapPixels);
		}
	}
}
