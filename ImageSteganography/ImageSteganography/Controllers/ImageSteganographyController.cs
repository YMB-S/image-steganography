using ImageSteganography.Models;
using ImageSteganography.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImageSteganography.Controllers
{
    public class ImageSteganographyController(ImageSteganographyService service) : Controller
    {
        private readonly ImageSteganographyService service = service;

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EncodeMessageInImage([FromForm] EncodeMessageInImageModel model)
        {
            try
            {
                FileStreamResult stream = await service.EncodeMessageInImage(model.ImageFile, model.Message);
                stream.FileStream.Position = 0;
                var file = File(stream.FileStream, "image/png");

                return file;
            }
            catch (InvalidOperationException e)
            {
                return StatusCode(500);
            }
        }
    }
}
