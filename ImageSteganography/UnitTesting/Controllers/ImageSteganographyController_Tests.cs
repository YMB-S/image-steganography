using ImageSteganography.Controllers;
using ImageSteganography.Models;
using ImageSteganography.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Net.Mime;
using System.Reflection.PortableExecutable;
using System.Text;
using SixLabors.ImageSharp.Formats;
using Microsoft.AspNetCore.Http.HttpResults;
using SixLabors.ImageSharp.Processing;

namespace UnitTesting.Controllers
{
	public class ImageSteganographyController_Tests
	{
		[Fact]
		public async Task ImageSteganographyController_EncodesMessageCorrectly()
		{
			var jsonString = "{\"AllowedFileTypes\" : [\".jpg\", \".jpeg\", \".png\"]}";

			var configuration = new ConfigurationBuilder()
				.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
				.Build();

			var service = new Mock<ImageSteganographyService>(configuration).Object;
			var controller = new ImageSteganographyController(service);

			string testMessage = "ab🍌";
            IFormFile testFile = CreateImageFile();

			var testModel = new EncodeMessageInImageModel()
			{
				ImageFile = testFile,
				Message = testMessage
			};

            var response = await controller.EncodeMessageInImage(testModel);

            var fileStreamResult = Assert.IsType<FileStreamResult>(response);

            byte[] responseBytes;
            using (var memoryStream = new MemoryStream())
            {
                await fileStreamResult.FileStream.CopyToAsync(memoryStream);
                responseBytes = memoryStream.ToArray();
            }

            using (var resultImage = Image.Load<Rgba32>(responseBytes))
            {
                // Assert correct size of message is encoded in the first pixel
                Assert.Equal(0, resultImage[0, 0].R);
                Assert.Equal(0, resultImage[0, 0].G);
                Assert.Equal(0, resultImage[0, 0].B);
                Assert.Equal(0, resultImage[1, 0].R);
                Assert.Equal(0, resultImage[1, 0].G);
                Assert.Equal(3, resultImage[1, 0].B);

                // Assert message is encoded correctly

                // Assert A is encoded by int 97
                Assert.Equal(0, resultImage[2, 0].R);
                Assert.Equal(0, resultImage[2, 0].G);
                Assert.Equal(0, resultImage[2, 0].B);
                Assert.Equal(0, resultImage[3, 0].R);
                Assert.Equal(9, resultImage[3, 0].G);
                Assert.Equal(7, resultImage[3, 0].B);

                // Assert B is encoded by int 98
                Assert.Equal(0, resultImage[4, 0].R);
                Assert.Equal(0, resultImage[4, 0].G);
                Assert.Equal(0, resultImage[4, 0].B);
                Assert.Equal(0, resultImage[5, 0].R);
                Assert.Equal(9, resultImage[5, 0].G);
                Assert.Equal(8, resultImage[5, 0].B);

                // Assert 🍌 is encoded by int 127820
                Assert.Equal(1, resultImage[6, 0].R);
                Assert.Equal(2, resultImage[6, 0].G);
                Assert.Equal(7, resultImage[6, 0].B);
                Assert.Equal(8, resultImage[7, 0].R);
                Assert.Equal(2, resultImage[7, 0].G);
                Assert.Equal(0, resultImage[7, 0].B);
            }
		}

        private byte[] CreateWhiteImage(int width, int height)
        {
            using var image = new Image<Rgba32>(width, height);
            using var ms = new MemoryStream();

            image.Mutate(ctx => ctx.BackgroundColor(Color.Black));

            image.Save(ms, new PngEncoder());
            return ms.ToArray();
        }

        private IFormFile CreateImageFile()
        {
            byte[] imageBytes = CreateWhiteImage(10, 10);

            IFormFile testFile = new FormFile(new MemoryStream(imageBytes), 0, imageBytes.Length, "Data", "test-image.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            return testFile;
        }
    }
}