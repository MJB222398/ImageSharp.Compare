﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;

namespace Codeuctivity.ImageSharpCompare
{
    /// <summary>
    /// ImageSharpCompare, compares images.
    /// Use this class to compare images using a third image as mask of regions where your two compared images may differ.
    /// An alpha channel is ignored.
    /// </summary>
#pragma warning disable CA1724 // Type names should not match namespaces - this is accepted for now to prevent a breaking change

    public static class ImageSharpCompare
#pragma warning restore CA1724 // Type names should not match namespaces - this is accepted for now to prevent a breaking change
    {
        private const string sizeDiffersExceptionMessage = "Size of images differ.";

        private static bool ImagesHaveSameDimension(Image actual, Image expected)
        {
            ArgumentNullException.ThrowIfNull(actual);
            ArgumentNullException.ThrowIfNull(expected);
            return actual.Height == expected.Height && actual.Width == expected.Width;
        }

        private static (Image<Rgb24>, Image<Rgb24>) GrowToSameDimension(Image<Rgb24> actual, Image<Rgb24> expected)
        {
            var biggestWidth = actual.Width > expected.Width ? actual.Width : expected.Width;
            var biggestHeight = actual.Height > expected.Height ? actual.Height : expected.Height;

            var grownExpected = expected.Clone();
            var grownActual = actual.Clone();
            grownActual.Mutate(x => x.Resize(biggestWidth, biggestHeight));
            grownExpected.Mutate(x => x.Resize(biggestWidth, biggestHeight));

            return (grownActual, grownExpected);
        }

        private static (Image<Rgb24>, Image<Rgb24>, Image<Rgb24>) GrowToSameDimension(Image<Rgb24> actual, Image<Rgb24> expected, Image<Rgb24> mask)
        {
            var biggestWidth = actual.Width > expected.Width ? actual.Width : expected.Width;
            biggestWidth = biggestWidth > mask.Width ? biggestWidth : mask.Width;
            var biggestHeight = actual.Height > expected.Height ? actual.Height : expected.Height;
            biggestHeight = biggestHeight > mask.Height ? biggestHeight : mask.Height;

            var grownExpected = expected.Clone();
            var grownActual = actual.Clone();
            var grownMask = mask.Clone();
            grownActual.Mutate(x => x.Resize(biggestWidth, biggestHeight));
            grownExpected.Mutate(x => x.Resize(biggestWidth, biggestHeight));
            grownMask.Mutate(x => x.Resize(biggestWidth, biggestHeight));

            return (grownActual, grownExpected, grownMask);
        }

        /// <summary>
        /// Is true if width and height of both images are equal
        /// </summary>
        /// <param name="pathImageActual"></param>
        /// <param name="pathImageExpected"></param>
        /// <returns></returns>
        public static bool ImagesHaveEqualSize(string pathImageActual, string pathImageExpected)
        {
            using var actualImage = Image.Load(pathImageActual);
            using var expectedImage = Image.Load(pathImageExpected);
            return ImagesHaveEqualSize(actualImage, expectedImage);
        }

        /// <summary>
        /// Is true if width and height of both images are equal
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <returns></returns>
        public static bool ImagesHaveEqualSize(Stream actual, Stream expected)
        {
            using var actualImage = Image.Load(actual);
            using var expectedImage = Image.Load(expected);
            return ImagesHaveEqualSize(actualImage, expectedImage);
        }

        /// <summary>
        /// Is true if width and height of both images are equal
        /// </summary>
        /// <param name="actualImage"></param>
        /// <param name="expectedImage"></param>
        /// <returns></returns>
        public static bool ImagesHaveEqualSize(Image actualImage, Image expectedImage)
        {
            return ImagesHaveSameDimension(actualImage, expectedImage);
        }

        /// <summary>
        /// Compares two images for equivalence
        /// </summary>
        /// <param name="pathImageActual"></param>
        /// <param name="pathImageExpected"></param>
        /// <param name="resizeOption"></param>
        /// <returns>True if every pixel of actual is equal to expected</returns>
        public static bool ImagesAreEqual(string pathImageActual, string pathImageExpected, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            using var actualImage = Image.Load(pathImageActual);
            using var expectedImage = Image.Load(pathImageExpected);
            return ImagesAreEqual(actualImage, expectedImage, resizeOption);
        }

        /// <summary>
        /// Compares two images for equivalence
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <param name="resizeOption"></param>
        /// <returns>True if every pixel of actual is equal to expected</returns>
        public static bool ImagesAreEqual(Stream actual, Stream expected, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            using var actualImage = Image.Load(actual);
            using var expectedImage = Image.Load(expected);
            return ImagesAreEqual(actualImage, expectedImage, resizeOption);
        }

        /// <summary>
        /// Compares two images for equivalence
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <param name="resizeOption"></param>
        /// <returns>True if every pixel of actual is equal to expected</returns>
        public static bool ImagesAreEqual(Image actual, Image expected, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            ArgumentNullException.ThrowIfNull(actual);

            ArgumentNullException.ThrowIfNull(expected);

            var ownsActual = false;
            var ownsExpected = false;
            Image<Rgb24>? actualPixelAccessibleImage = null;
            Image<Rgb24>? expectedPixelAccusableImage = null;
            try
            {
                actualPixelAccessibleImage = ToRgb24Image(actual, out ownsActual);
                expectedPixelAccusableImage = ToRgb24Image(expected, out ownsExpected);

                return ImagesAreEqual(actualPixelAccessibleImage, expectedPixelAccusableImage, resizeOption);
            }
            finally
            {
                if (ownsActual)
                {
                    actualPixelAccessibleImage?.Dispose();
                }
                if (ownsExpected)
                {
                    expectedPixelAccusableImage?.Dispose();
                }
            }
        }

        /// <summary>
        /// Compares two images for equivalence
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <param name="resizeOption"></param>
        /// <returns>True if every pixel of actual is equal to expected</returns>
        public static bool ImagesAreEqual(Image<Rgb24> actual, Image<Rgb24> expected, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            ArgumentNullException.ThrowIfNull(actual);

            ArgumentNullException.ThrowIfNull(expected);

            if (resizeOption == ResizeOption.DontResize && !ImagesHaveSameDimension(actual, expected))
            {
                return false;
            }

            if (resizeOption == ResizeOption.DontResize || ImagesHaveSameDimension(actual, expected))
            {
                for (var x = 0; x < actual.Width; x++)
                {
                    for (var y = 0; y < actual.Height; y++)
                    {
                        if (!actual[x, y].Equals(expected[x, y]))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            var grown = GrowToSameDimension(actual, expected);
            try
            {
                return ImagesAreEqual(grown.Item1, grown.Item2, ResizeOption.DontResize);
            }
            finally
            {
                grown.Item1?.Dispose();
                grown.Item2?.Dispose();
            }
        }

        /// <summary>
        /// Calculates ICompareResult expressing the amount of difference of both images
        /// </summary>
        /// <param name="pathActualImage"></param>
        /// <param name="pathExpectedImage"></param>
        /// <param name="resizeOption"></param>
        /// <returns>Mean and absolute pixel error</returns>
        public static ICompareResult CalcDiff(string pathActualImage, string pathExpectedImage, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            using var actual = Image.Load(pathActualImage);
            using var expected = Image.Load(pathExpectedImage);
            return CalcDiff(actual, expected, resizeOption);
        }

        /// <summary>
        /// Calculates ICompareResult expressing the amount of difference of both images using a mask image for tolerated difference between the two images
        /// </summary>
        /// <param name="pathActualImage"></param>
        /// <param name="pathExpectedImage"></param>
        /// <param name="pathMaskImage"></param>
        /// <param name="resizeOption"></param>
        /// <returns>Mean and absolute pixel error</returns>
        public static ICompareResult CalcDiff(string pathActualImage, string pathExpectedImage, string pathMaskImage, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            using var actual = Image.Load(pathActualImage);
            using var expected = Image.Load(pathExpectedImage);
            using var mask = Image.Load(pathMaskImage);
            return CalcDiff(actual, expected, mask, resizeOption);
        }

        /// <summary>
        /// Calculates ICompareResult expressing the amount of difference of both images
        /// </summary>
        /// <param name="actualImage"></param>
        /// <param name="expectedImage"></param>
        /// <param name="resizeOption"></param>
        /// <returns>Mean and absolute pixel error</returns>
        public static ICompareResult CalcDiff(Stream actualImage, Stream expectedImage, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            using var actual = Image.Load(actualImage);
            using var expected = Image.Load(expectedImage);
            return CalcDiff(actual, expected, resizeOption);
        }

        /// <summary>
        /// Calculates ICompareResult expressing the amount of difference of both images
        /// </summary>
        /// <param name="actualImage"></param>
        /// <param name="expectedImage"></param>
        /// <param name="maskImage"></param>
        /// <param name="resizeOption"></param>
        /// <returns></returns>
        public static ICompareResult CalcDiff(Stream actualImage, Stream expectedImage, Image maskImage, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            using var actual = Image.Load(actualImage);
            using var expected = Image.Load(expectedImage);
            return CalcDiff(actual, expected, maskImage, resizeOption);
        }

        /// <summary>
        /// Calculates ICompareResult expressing the amount of difference of both images
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <param name="resizeOption"></param>
        /// <returns>Mean and absolute pixel error</returns>
        public static ICompareResult CalcDiff(Image actual, Image expected, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            ArgumentNullException.ThrowIfNull(actual);

            ArgumentNullException.ThrowIfNull(expected);

            var ownsActual = false;
            var ownsExpected = false;
            Image<Rgb24>? actualRgb24 = null;
            Image<Rgb24>? expectedRgb24 = null;

            try
            {
                actualRgb24 = ToRgb24Image(actual, out ownsActual);
                expectedRgb24 = ToRgb24Image(expected, out ownsExpected);

                return CalcDiff(actualRgb24, expectedRgb24, resizeOption);
            }
            finally
            {
                if (ownsActual)
                {
                    actualRgb24?.Dispose();
                }
                if (ownsExpected)
                {
                    expectedRgb24?.Dispose();
                }
            }
        }

        /// <summary>
        /// Calculates ICompareResult expressing the amount of difference of both images
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <param name="resizeOption"></param>
        /// <returns>Mean and absolute pixel error</returns>
        public static ICompareResult CalcDiff(Image<Rgb24> actual, Image<Rgb24> expected, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            var imagesHaveSameDimension = ImagesHaveSameDimension(actual, expected);

            if (resizeOption == ResizeOption.Resize && !imagesHaveSameDimension)
            {
                var grown = GrowToSameDimension(actual, expected);
                try
                {
                    return CalcDiff(grown.Item1, grown.Item2, ResizeOption.DontResize);
                }
                finally
                {
                    grown.Item1?.Dispose();
                    grown.Item2?.Dispose();
                }
            }

            if (!imagesHaveSameDimension)
            {
                throw new ImageSharpCompareException(sizeDiffersExceptionMessage);
            }

            var quantity = actual.Width * actual.Height;
            var absoluteError = 0;
            var pixelErrorCount = 0;

            for (var x = 0; x < actual.Width; x++)
            {
                for (var y = 0; y < actual.Height; y++)
                {
                    var actualPixel = actual[x, y];
                    var expectedPixel = expected[x, y];

                    var r = Math.Abs(expectedPixel.R - actualPixel.R);
                    var g = Math.Abs(expectedPixel.G - actualPixel.G);
                    var b = Math.Abs(expectedPixel.B - actualPixel.B);
                    absoluteError = absoluteError + r + g + b;

                    pixelErrorCount += r + g + b > 0 ? 1 : 0;
                }
            }

            var meanError = (double)absoluteError / quantity;
            var pixelErrorPercentage = (double)pixelErrorCount / quantity * 100;
            return new CompareResult(absoluteError, meanError, pixelErrorCount, pixelErrorPercentage);
        }

        /// <summary>
        /// Calculates ICompareResult expressing the amount of difference of both images using a image mask for tolerated difference between the two images
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <param name="maskImage"></param>
        /// <param name="resizeOption"></param>
        /// <returns>Mean and absolute pixel error</returns>
        public static ICompareResult CalcDiff(Image actual, Image expected, Image maskImage, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            ArgumentNullException.ThrowIfNull(actual);

            ArgumentNullException.ThrowIfNull(expected);

            ArgumentNullException.ThrowIfNull(maskImage);

            var ownsActual = false;
            var ownsExpected = false;
            var ownsMask = false;
            Image<Rgb24>? actualRgb24 = null;
            Image<Rgb24>? expectedRgb24 = null;
            Image<Rgb24>? maskImageRgb24 = null;

            try
            {
                actualRgb24 = ToRgb24Image(actual, out ownsActual);
                expectedRgb24 = ToRgb24Image(expected, out ownsExpected);
                maskImageRgb24 = ToRgb24Image(maskImage, out ownsMask);

                return CalcDiff(actualRgb24, expectedRgb24, maskImageRgb24, resizeOption);
            }
            finally
            {
                if (ownsActual)
                {
                    actualRgb24?.Dispose();
                }
                if (ownsExpected)
                {
                    expectedRgb24?.Dispose();
                }
                if (ownsMask)
                {
                    maskImageRgb24?.Dispose();
                }
            }
        }

        /// <summary>
        /// Calculates ICompareResult expressing the amount of difference of both images using a image mask for tolerated difference between the two images
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <param name="maskImage"></param>
        /// <param name="resizeOption"></param>
        /// <returns>Mean and absolute pixel error</returns>
        public static ICompareResult CalcDiff(Image<Rgb24> actual, Image<Rgb24> expected, Image<Rgb24> maskImage, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            ArgumentNullException.ThrowIfNull(maskImage);

            var imagesHaveSameDimension = ImagesHaveSameDimension(actual, expected) && ImagesHaveSameDimension(actual, maskImage);

            if (resizeOption == ResizeOption.Resize && !imagesHaveSameDimension)
            {
                var grown = GrowToSameDimension(actual, expected, maskImage);
                try
                {
                    return CalcDiff(grown.Item1, grown.Item2, grown.Item3, ResizeOption.DontResize);
                }
                finally
                {
                    grown.Item1?.Dispose();
                    grown.Item2?.Dispose();
                    grown.Item3?.Dispose();
                }
            }

            if (!imagesHaveSameDimension)
            {
                throw new ImageSharpCompareException(sizeDiffersExceptionMessage);
            }

            var quantity = actual.Width * actual.Height;
            var absoluteError = 0;
            var pixelErrorCount = 0;

            for (var x = 0; x < actual.Width; x++)
            {
                for (var y = 0; y < actual.Height; y++)
                {
                    var maskImagePixel = maskImage[x, y];
                    var actualPixel = actual[x, y];
                    var expectedPixel = expected[x, y];

                    var r = Math.Abs(expectedPixel.R - actualPixel.R);
                    var g = Math.Abs(expectedPixel.G - actualPixel.G);
                    var b = Math.Abs(expectedPixel.B - actualPixel.B);

                    var error = 0;

                    if (r > maskImagePixel.R)
                    {
                        error += r;
                    }

                    if (g > maskImagePixel.G)
                    {
                        error += g;
                    }

                    if (b > maskImagePixel.B)
                    {
                        error += b;
                    }

                    absoluteError += error;
                    pixelErrorCount += error > 0 ? 1 : 0;
                }
            }
            var meanError = (double)absoluteError / quantity;
            var pixelErrorPercentage = (double)pixelErrorCount / quantity * 100;
            return new CompareResult(absoluteError, meanError, pixelErrorCount, pixelErrorPercentage);
        }

        /// <summary>
        /// Creates a diff mask image of two images
        /// </summary>
        /// <param name="pathActualImage"></param>
        /// <param name="pathExpectedImage"></param>
        /// <param name="resizeOption"></param>
        /// <returns>Image representing diff, black means no diff between actual image and expected image, white means max diff</returns>
        public static Image CalcDiffMaskImage(string pathActualImage, string pathExpectedImage, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            using var actual = Image.Load(pathActualImage);
            using var expected = Image.Load(pathExpectedImage);
            return CalcDiffMaskImage(actual, expected, resizeOption);
        }

        /// <summary>
        /// Creates a diff mask image of two images
        /// </summary>
        /// <param name="pathActualImage"></param>
        /// <param name="pathExpectedImage"></param>
        /// <param name="pathMaskImage"></param>
        /// <param name="resizeOption"></param>
        /// <returns>Image representing diff, black means no diff between actual image and expected image, white means max diff</returns>
        public static Image CalcDiffMaskImage(string pathActualImage, string pathExpectedImage, string pathMaskImage, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            using var actual = Image.Load(pathActualImage);
            using var expected = Image.Load(pathExpectedImage);
            using var mask = Image.Load(pathMaskImage);
            return CalcDiffMaskImage(actual, expected, mask, resizeOption);
        }

        /// <summary>
        /// Creates a diff mask image of two images
        /// </summary>
        /// <param name="actualImage"></param>
        /// <param name="expectedImage"></param>
        /// <param name="resizeOption"></param>
        /// <returns>Image representing diff, black means no diff between actual image and expected image, white means max diff</returns>
        public static Image CalcDiffMaskImage(Stream actualImage, Stream expectedImage, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            using var actual = Image.Load(actualImage);
            using var expected = Image.Load(expectedImage);
            return CalcDiffMaskImage(actual, expected, resizeOption);
        }

        /// <summary>
        /// Creates a diff mask image of two images
        /// </summary>
        /// <param name="actualImage"></param>
        /// <param name="expectedImage"></param>
        /// <param name="maskImage"></param>
        /// <param name="resizeOption"></param>
        /// <returns>Image representing diff, black means no diff between actual image and expected image, white means max diff</returns>
        public static Image CalcDiffMaskImage(Stream actualImage, Stream expectedImage, Stream maskImage, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            using var actual = Image.Load(actualImage);
            using var expected = Image.Load(expectedImage);
            using var mask = Image.Load(maskImage);
            return CalcDiffMaskImage(actual, expected, mask, resizeOption);
        }

        /// <summary>
        /// Creates a diff mask image of two images
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <param name="resizeOption"></param>
        /// <returns>Image representing diff, black means no diff between actual image and expected image, white means max diff</returns>
        public static Image CalcDiffMaskImage(Image actual, Image expected, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            ArgumentNullException.ThrowIfNull(actual);

            ArgumentNullException.ThrowIfNull(expected);

            var ownsActual = false;
            var ownsExpected = false;
            Image<Rgb24>? actualRgb24 = null;
            Image<Rgb24>? expectedRgb24 = null;

            try
            {
                actualRgb24 = ToRgb24Image(actual, out ownsActual);
                expectedRgb24 = ToRgb24Image(expected, out ownsExpected);

                return CalcDiffMaskImage(actualRgb24, expectedRgb24, resizeOption);
            }
            finally
            {
                if (ownsActual)
                {
                    actualRgb24?.Dispose();
                }
                if (ownsExpected)
                {
                    expectedRgb24?.Dispose();
                }
            }
        }

        /// <summary>
        /// Creates a diff mask image of two images
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <param name="mask"></param>
        /// <param name="resizeOption"></param>
        /// <returns>Image representing diff, black means no diff between actual image and expected image, white means max diff</returns>
        public static Image CalcDiffMaskImage(Image actual, Image expected, Image mask, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            ArgumentNullException.ThrowIfNull(actual);
            ArgumentNullException.ThrowIfNull(expected);
            ArgumentNullException.ThrowIfNull(mask);
            var ownsActual = false;
            var ownsExpected = false;
            var ownsMask = false;
            Image<Rgb24>? actualRgb24 = null;
            Image<Rgb24>? expectedRgb24 = null;
            Image<Rgb24>? maskRgb24 = null;

            try
            {
                actualRgb24 = ToRgb24Image(actual, out ownsActual);
                expectedRgb24 = ToRgb24Image(expected, out ownsExpected);
                maskRgb24 = ToRgb24Image(mask, out ownsMask);

                return CalcDiffMaskImage(actualRgb24, expectedRgb24, maskRgb24, resizeOption);
            }
            finally
            {
                if (ownsActual)
                {
                    actualRgb24?.Dispose();
                }
                if (ownsExpected)
                {
                    expectedRgb24?.Dispose();
                }
                if (ownsMask)
                {
                    maskRgb24?.Dispose();
                }
            }
        }

        /// <summary>
        /// Creates a diff mask image of two images
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <param name="resizeOption"></param>
        /// <returns>Image representing diff, black means no diff between actual image and expected image, white means max diff</returns>
        public static Image CalcDiffMaskImage(Image<Rgb24> actual, Image<Rgb24> expected, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            var imagesHAveSameDimension = ImagesHaveSameDimension(actual, expected);

            if (resizeOption == ResizeOption.DontResize && !imagesHAveSameDimension)
            {
                throw new ImageSharpCompareException(sizeDiffersExceptionMessage);
            }

            if (imagesHAveSameDimension)
            {
                var maskImage = new Image<Rgb24>(actual.Width, actual.Height);

                for (var x = 0; x < actual.Width; x++)
                {
                    for (var y = 0; y < actual.Height; y++)
                    {
                        var actualPixel = actual[x, y];
                        var expectedPixel = expected[x, y];

                        var pixel = new Rgb24
                        {
                            R = (byte)Math.Abs(actualPixel.R - expectedPixel.R),
                            G = (byte)Math.Abs(actualPixel.G - expectedPixel.G),
                            B = (byte)Math.Abs(actualPixel.B - expectedPixel.B)
                        };

                        maskImage[x, y] = pixel;
                    }
                }
                return maskImage;
            }

            var grown = GrowToSameDimension(actual, expected);
            try
            {
                return CalcDiffMaskImage(grown.Item1, grown.Item2, ResizeOption.DontResize);
            }
            finally
            {
                grown.Item1?.Dispose();
                grown.Item2?.Dispose();
            }
        }

        /// <summary>
        /// Creates a diff mask image of two images using a image mask for tolerated difference between the two images.
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <param name="mask"></param>
        /// <param name="resizeOption"></param>
        /// <returns>Image representing diff, black means no diff between actual image and expected image, white means max diff</returns>
        public static Image CalcDiffMaskImage(Image<Rgb24> actual, Image<Rgb24> expected, Image<Rgb24> mask, ResizeOption resizeOption = ResizeOption.DontResize)
        {
            ArgumentNullException.ThrowIfNull(mask);
            var imagesHaveSameDimensions = ImagesHaveSameDimension(actual, expected) && ImagesHaveSameDimension(actual, mask);

            if (!imagesHaveSameDimensions && resizeOption == ResizeOption.DontResize)
            {
                throw new ImageSharpCompareException(sizeDiffersExceptionMessage);
            }

            if (imagesHaveSameDimensions)
            {
                var maskImageResult = new Image<Rgb24>(actual.Width, actual.Height);

                for (var x = 0; x < actual.Width; x++)
                {
                    for (var y = 0; y < actual.Height; y++)
                    {
                        var maskPixel = mask[x, y];
                        var actualPixel = actual[x, y];
                        var expectedPixel = expected[x, y];

                        maskImageResult[x, y] = new Rgb24
                        {
                            R = (byte)Math.Max(byte.MinValue, Math.Abs(expectedPixel.R - actualPixel.R) - maskPixel.R),
                            G = (byte)Math.Max(byte.MinValue, Math.Abs(expectedPixel.G - actualPixel.G) - maskPixel.G),
                            B = (byte)Math.Max(byte.MinValue, Math.Abs(expectedPixel.B - actualPixel.B) - maskPixel.B)
                        };
                    }
                }

                return maskImageResult;
            }

            var grown = GrowToSameDimension(actual, expected, mask);
            try
            {
                return CalcDiffMaskImage(grown.Item1, grown.Item2, grown.Item3, ResizeOption.DontResize);
            }
            finally
            {
                grown.Item1?.Dispose();
                grown.Item2?.Dispose();
                grown.Item3?.Dispose();
            }
        }

        private static Image<Rgb24> ToRgb24Image(Image actual, out bool ownsImage)
        {
            if (actual is Image<Rgb24> actualPixelAccessibleImage)
            {
                ownsImage = false;
                return actualPixelAccessibleImage;
            }

            if (actual is Image<Rgba32> imageRgba32)
            {
                ownsImage = true;
                return ConvertRgba32ToRgb24(imageRgba32);
            }

            throw new NotImplementedException($"Pixel type {actual.PixelType} is not supported to be compared.");
        }

        /// <summary>
        /// Converts a Rgba32 Image to Rgb24 one
        /// </summary>
        /// <param name="imageRgba32"></param>
        public static Image<Rgb24> ConvertRgba32ToRgb24(Image<Rgba32> imageRgba32)
        {
            ArgumentNullException.ThrowIfNull(imageRgba32);

            var maskRgb24 = new Image<Rgb24>(imageRgba32.Width, imageRgba32.Height);

            for (var x = 0; x < imageRgba32.Width; x++)
            {
                for (var y = 0; y < imageRgba32.Height; y++)
                {
                    var pixel = new Rgb24();
                    pixel.FromRgba32(imageRgba32[x, y]);

                    maskRgb24[x, y] = pixel;
                }
            }
            return maskRgb24;
        }
    }
}