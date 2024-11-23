using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Pipelines;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
//using static System.Net.Mime.MediaTypeNames;

namespace ImageSteganography.Services
{
    public class ImageSteganographyService(IConfiguration config)
    {
        private readonly List<string> allowedFileTypes = config.GetSection("AllowedFileTypes").Get<List<string>>() ?? throw new InvalidOperationException();

        public bool IsAllowedFileType(string filetype)
        {
            return allowedFileTypes.Contains(filetype);
        }

        public async Task<FileStreamResult> EncodeMessageInImage(IFormFile imageFile, string message)
        {
            var stream = imageFile.OpenReadStream();
            var image = await Image.LoadAsync<Rgba32>(stream);

            var embeddableMessage = GetEmbeddableValuesForMessage(message);
            var manipulatedImage = Encode(image, embeddableMessage);
            var fileStream = await SaveImage(manipulatedImage);
            return fileStream;
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

                    //Console.WriteLine(firstPixel.R);
                    //Console.WriteLine(firstPixel.G);
                    //Console.WriteLine(firstPixel.B);

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

        private int GetLastDigitOf(int number)
        {
            return number % 10;
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

        private async Task<FileStreamResult> SaveImage(Image<Rgba32> image)
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

        private Image<Rgba32> ReverseImageColors(Image<Rgba32> image)
        {
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    // Get the pixel color
                    Rgba32 pixel = image[x, y];

                    // Example manipulation: invert colors
                    pixel.R = (byte)(255 - pixel.R);
                    pixel.G = (byte)(255 - pixel.G);
                    pixel.B = (byte)(255 - pixel.B);

                    // Write the pixel back to the image
                    image[x, y] = pixel;
                }
            }
            return image;
        }
    }
}
