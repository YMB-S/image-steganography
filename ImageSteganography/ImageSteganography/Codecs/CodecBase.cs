using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using Microsoft.AspNetCore.Mvc;

namespace ImageSteganography.Codecs
{
    public abstract class CodecBase
    {
        private List<string> allowedFileTypes = new();

        public int PixelsPerEncodedCharacter { get; set; }

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

        
        
        
        
        
        public async Task EncodeMessageInImage(IFormFile imageFile, string message)
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

            int[] embeddableMessageLength = { 1, 2, 3, 4, 1, 2, 3, 4, 0, 0, 0, 0 }; //GetLengthOfMessageAsEmbeddableValue(SplitUnicodeString(message).Length);
            List<int[]> embeddableMessage = new();
            embeddableMessage.Add([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);

            AddLengthOfMessageToEmbeddableMessage(embeddableMessage, embeddableMessageLength);

            //var manipulatedImage = Encode(image, embeddableMessage);
            //var fileStream = await GetFileStreamFor(manipulatedImage);
            //return fileStream;
        }

        private bool MessageFitsInImage(Image<Rgba32> image, string message)
        {
            int amountOfPixels = image.Width * image.Height;
            return (amountOfPixels / PixelsPerEncodedCharacter) >= message.Length;
        }

        //protected abstract void GetLengthOfMessage();

        //protected abstract string[] GetMessage();

        protected void AddLengthOfMessageToEmbeddableMessage(List<int[]> embeddableMessage, int[] embeddableMessageLength)
        {
            List<int[]> valuesToAdd = new();
            int wordLength = (PixelsPerEncodedCharacter * 3);

            for (int i = 0; i < PixelsPerEncodedCharacter; i++)
            {
                int[] pixelData = new int[PixelsPerEncodedCharacter * 3];
                Array.Copy(embeddableMessageLength, i * wordLength, pixelData, 0, wordLength);
                valuesToAdd.Add(pixelData);
            }

            for (int i = valuesToAdd.Count - 1; i >= 0; i--)
            {
                int[] toInsert = valuesToAdd[i];
                embeddableMessage.Insert(0, toInsert);
            }

            Console.WriteLine(embeddableMessage);
        }

        //private void AddLengthOfMessageToEmbeddableMessage(List<int[]> embeddableMessage, int[] embeddableMessageLength)
        //{
        //    int[] firstPixelData = new int[6];
        //    Array.Copy(embeddableMessageLength, 0, firstPixelData, 0, 6);

        //    int[] secondPixelData = new int[6];
        //    Array.Copy(embeddableMessageLength, 6, secondPixelData, 0, 6);

        //    embeddableMessage.Insert(0, firstPixelData); // The length of the message is encoded in the first four pixels
        //    embeddableMessage.Insert(1, secondPixelData);
        //}

        private List<int[]> GetEmbeddableValuesForMessage(string message)
        {
            List<int[]> result = new();
            string[] characters = SplitUnicodeString(message); //GetMessage() instead

            foreach (string ch in characters)
            {
                int[] embeddableValues = CharacterToEmbeddableIntegerArray(ch);
                result.Add(embeddableValues);
            }

            return result;
        }

        private int[] GetLengthOfMessageAsEmbeddableValue(long sizeOfMessage)
        {
            string[] digits = sizeOfMessage.ToString().Select(c => c.ToString()).ToArray();

            // We use four pixels to store the message length. This gives us a maximum message length of (10^12) -1.
            // Fun fact: that's over 320.000 bibles.
            int[] embeddableLengthOfMessage = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            for (int i = 0; i < digits.Length; i++)
            {
                string digit = digits[digits.Length - i - 1].ToString();
                embeddableLengthOfMessage[embeddableLengthOfMessage.Length - i - 1] = Int32.Parse(digit);
            }

            return embeddableLengthOfMessage;
        }
    }
}
