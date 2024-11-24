using ImageSteganography.Models;
using ImageSteganography.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImageSteganography.Controllers
{
    public class ImageSteganographyController(ImageSteganographyService service) : Controller
    {
        private readonly ImageSteganographyService service = service;

        public IActionResult Encode()
        {
            return View();
        }

        public IActionResult Decode()
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

        [HttpPost]
        public async Task<IActionResult> DecodeMessageFromImage([FromForm] DecodeMessageFromImageModel model)
        {
            try
            {
                var message = await service.DecodeMessageFromImage(model.ImageFile);

                return Ok(message);
            }
            catch (InvalidOperationException e)
            {
                return StatusCode(500);
            }
        }
    }
}
