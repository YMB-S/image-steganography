namespace ImageSteganography.Models
{
    public class EncodeMessageInImageModel
    {
        public string Message { get; set; }
        public IFormFile ImageFile { get; set; }
    }
}
