using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Pipelines;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageSteganography.Services
{
    public class ImageSteganographyService(IConfiguration config)
    {
        private readonly List<string> allowedFileTypes = config.GetSection("AllowedFileTypes").Get<List<string>>() ?? throw new InvalidOperationException();

        #region Common methods
        private bool IsAllowedFileType(string filetype)
        {
            return allowedFileTypes.Contains(filetype);
        }
        private int GetLastDigitOf(int number)
        {
            return number % 10;
        }

        public async Task<Image<Rgba32>> LoadImageFrom(IFormFile imageFile)
        {
            if (imageFile == null || !IsAllowedFileType(Path.GetExtension(imageFile.FileName)))
            {
                throw new InvalidOperationException();
            }

            var stream = imageFile.OpenReadStream();
            var image = await Image.LoadAsync<Rgba32>(stream);
            return image;
        }
        #endregion

        #region Methods used for encoding

        public async Task<FileStreamResult> EncodeMessageInImage(IFormFile imageFile, string message)
        {
            var image = await LoadImageFrom(imageFile);

            if (
                !IsAllowedFileType(Path.GetExtension(imageFile.FileName)) ||
                message == null ||
                message.Equals(string.Empty) ||
                !MessageFitsInImage(image, message)
            )
            {
                throw new InvalidOperationException();
            }

            var embeddableMessageLength = GetLengthOfMessageAsEmbeddableValue(SplitUnicodeString(message).Length);
            var embeddableMessage = GetEmbeddableValuesForMessage(message);

            AddLengthOfMessageToEmbeddableMessage(embeddableMessage, embeddableMessageLength);

            var manipulatedImage = Encode(image, embeddableMessage);
            var fileStream = await GetFileStreamFor(manipulatedImage);
            return fileStream;
        }

        private void AddLengthOfMessageToEmbeddableMessage(List<int[]> embeddableMessage, int[] embeddableMessageLength)
        {
            int[] firstPixelData = new int[6];
            Array.Copy(embeddableMessageLength, 0, firstPixelData, 0, 6);

            int[] secondPixelData = new int[6];
            Array.Copy(embeddableMessageLength, 6, secondPixelData, 0, 6);

            embeddableMessage.Insert(0, firstPixelData); // The length of the message is encoded in the first four pixels
            embeddableMessage.Insert(1, secondPixelData);
        }

        private int[] GetLengthOfMessageAsEmbeddableValue(long sizeOfMessage)
        {
            string[] digits = sizeOfMessage.ToString().Select(c => c.ToString()).ToArray();

            // We use two pixels to store the message length. This gives us a maximum message length of (10^12) -1.
            // Fun fact: that's over 320.000 bibles.
            // Encoding that would require an image file of roughly 2TB. As such, two pixels should be enough space.
            int[] embeddableLengthOfMessage = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            for (int i = 0; i < digits.Length; i++)
            {
                string digit = digits[digits.Length - i - 1].ToString();
                embeddableLengthOfMessage[embeddableLengthOfMessage.Length - i - 1] = Int32.Parse(digit);
            }

            return embeddableLengthOfMessage;
        }

        private bool MessageFitsInImage(Image<Rgba32> image, string message)
        {
            int amountOfPixels = image.Width * image.Height;
            return (amountOfPixels / 2) >= message.Length;
        }

        private List<int[]> GetEmbeddableValuesForMessage(string message)
        {
            List<int[]> result = new();
            string[] characters = SplitUnicodeString(message);

            foreach (string ch in characters)
            {
                int[] embeddableValues = CharacterToEmbeddableIntegerArray(ch);
                result.Add(embeddableValues);
                foreach (var item in embeddableValues)
                {
                    Console.WriteLine(item);
                }
            }

            return result;
        }

        private Image<Rgba32> Encode(Image<Rgba32> image, List<int[]> values)
        {
            int currentValueIndex = 0;
            int maxValueIndex = values.Count;
            int[] currentValue = values[currentValueIndex];

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x += 2)
                {
                    if (currentValueIndex >= maxValueIndex)
                    {
                        return image;
                    }

                    currentValue = values[currentValueIndex];
                    Rgba32 firstPixel = image[x, y];
                    Rgba32 secondPixel = image[x + 1, y];

                    // The Unicode characters we encode have up to 6 digits so we use 2 pixels to encode each of the 6 digits
                    firstPixel.R = EncodeDigitInByte(firstPixel.R, currentValue[0]);
                    firstPixel.G = EncodeDigitInByte(firstPixel.G, currentValue[1]);
                    firstPixel.B = EncodeDigitInByte(firstPixel.B, currentValue[2]);

                    secondPixel.R = EncodeDigitInByte(secondPixel.R, currentValue[3]);
                    secondPixel.G = EncodeDigitInByte(secondPixel.G, currentValue[4]);
                    secondPixel.B = EncodeDigitInByte(secondPixel.B, currentValue[5]);

                    image[x, y] = firstPixel;
                    image[x+1, y] = secondPixel;

                    currentValueIndex++;
                }
            }
            return image;
        }

        private byte EncodeDigitInByte(byte input, int digit)
        {
            int roundedToLastDecimal = input - GetLastDigitOf(input);
            int newValue = roundedToLastDecimal + digit;

            if(newValue > 255)
            {
                newValue -= 10;
            }

            return (byte)newValue;
        }

        private string[] SplitUnicodeString(string toSplit)
        {
            StringInfo stringInfo = new(toSplit);
            string[] result = new string[stringInfo.LengthInTextElements];
            for (int i = 0; i < stringInfo.LengthInTextElements; i++)
            {
                result[i] = stringInfo.SubstringByTextElements(i, 1);
            }
            return result;
        }

        private async Task<FileStreamResult> GetFileStreamFor(Image<Rgba32> image)
        {
            var outputStream = new MemoryStream();
            await image.SaveAsPngAsync(outputStream);
            outputStream.Position = 0;
            return new FileStreamResult(outputStream, "image/png")
            {
                FileDownloadName = "modified_image.png"
            };
        }

        private int[] CharacterToEmbeddableIntegerArray(string character)
        {
            int integerValue = character.EnumerateRunes().First().Value;
            string[] num = integerValue.ToString().Select(c => c.ToString()).ToArray();

            int[] embeddable = { 0, 0, 0, 0, 0, 0 };

            for (int i = 0; i < num.Length; i++)
            {
                string digit = num[num.Length-i-1].ToString();
                embeddable[embeddable.Length - i - 1] = Int32.Parse(digit);
            }

            return embeddable;
        }

        #endregion

        #region Methods used for decoding

        public async Task<string> DecodeMessageFromImage(IFormFile imageFile)
        {
            var image = await LoadImageFrom(imageFile);

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x += 2)
                {
                    Rgba32 firstPixel = image[x, y];
                    Rgba32 secondPixel = image[x + 1, y];

                    int[] values = new int[]
                    { 
                        GetLastDigitOf(firstPixel.R), GetLastDigitOf(firstPixel.G), GetLastDigitOf(firstPixel.B),
                        GetLastDigitOf(secondPixel.R), GetLastDigitOf(secondPixel.G), GetLastDigitOf(secondPixel.B),
                    };

                    foreach (var item in values)
                    {
                        Console.WriteLine(item);
                    }

                    return "";
                }
            }

            return "";
        }

        #endregion
    }
}
