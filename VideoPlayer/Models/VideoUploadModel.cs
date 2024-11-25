namespace VideoPlayer.Models
{
    public class VideoUploadModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public IFormFile? File { get; set; }
    }
}