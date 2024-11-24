using ImageSteganography.Controllers;
using ImageSteganography.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace UnitTesting.Controllers
{
	public class ImageSteganographyController_Tests																																					   
	{
		[Fact]
		public void ImageSteganographyController_EncodesMessageCorrectly()
		{
			var jsonString = "{\"AllowedFileTypes\" : [\".jpg\", \".jpeg\", \".png\"]}";

			var configuration = new ConfigurationBuilder()
				.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
				.Build();

            var service = new Mock<ImageSteganographyService>(configuration);
			var controller = new ImageSteganographyController(service.Object);

			//TODO: act

			Assert.Equal("", "");
		}
	}
}
