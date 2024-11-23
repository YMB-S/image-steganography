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
            if (model.ImageFile == null)
            {
                return Redirect("/Home/Error");
            }

            if (!service.IsAllowedFileType(Path.GetExtension(model.ImageFile.FileName)))
            {
                return Redirect("/Home/Error");
            }

            var stream = await service.EncodeMessageInImage(model.ImageFile, model.Message);
            var file = File(stream.FileStream, "image/png");

            return file;
        }
    }
}
