namespace AptCare.Repository.Cloudinary
{
    public class CloudinarySettings
    {
        public string CloudName { get; set; } = null!;
        public string ApiKey { get; set; } = null!;
        public string ApiSecret { get; set; } = null!;
        public string UploadPreset { get; set; } = null!;
    }
}

