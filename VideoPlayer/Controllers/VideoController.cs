
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using VideoPlayer.Models;
using VideoPlayer.Repositories;

namespace AspCore7_Web_Identity.Controllers
{
    public class VideoController : Controller
    {
        private readonly IHostEnvironment _hostingEnvironment;

        private readonly IVideoRepository _videoRepository;
        public VideoController(IHostEnvironment hostingEnvironment, IVideoRepository videoRepository)
        {
            _videoRepository = videoRepository;
            _hostingEnvironment = hostingEnvironment;
        }   
        public IActionResult Index()
        {
            var videos = _videoRepository.GetAllVideos();
            return View(videos);
        }
        public IActionResult Upload()
        {
            return View();
        }

        public IActionResult Play(int id)
        {
            var video = _videoRepository.GetVideoById(id);
            return View(video);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(VideoUploadModel model)
        {
            if (model.File == null || model.File.Length == 0)
                return BadRequest("No file uploaded.");

            var uploadPath = Path.Combine(_hostingEnvironment.ContentRootPath, "wwwroot", "uploads");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, model.File.FileName);

            // Upload file asynchronously
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.File.CopyToAsync(fileStream);
            }

            // Create folder for HLS files
            var hlsOutputPath = Path.Combine(uploadPath, "hls", model.File.FileName);
            if (!Directory.Exists(hlsOutputPath))
                Directory.CreateDirectory(hlsOutputPath);

            // เริ่มการแปลงไฟล์ใน Background Task
            _ = Task.Run(() => ConvertToHls(filePath, Path.Combine(hlsOutputPath, Path.GetFileNameWithoutExtension(model.File.FileName))));

            // เก็บข้อมูลการแปลงไฟล์ลงฐานข้อมูล
            var video = new Video
            {
                Title = model.Title,
                Description = model.Description,
                Url = "/uploads/hls/" + model.File.FileName + "/" + Path.GetFileNameWithoutExtension(model.File.FileName) + ".m3u8",
            };

            _videoRepository.SaveData(video);

            // ตอบกลับไปยังผู้ใช้ได้ทันทีไม่ต้องรอการแปลงไฟล์เสร็จ
            return Ok("Video uploaded successfully. Processing in the background.");
        }

        private async Task ConvertToHls(string inputPath, string outputPath)
        {
            var arguments = $"-i \"{inputPath}\" -c:v libx264 -b:v 1M -hls_time 4 -hls_list_size 0 -hls_segment_filename \"{outputPath}%03d.ts\" \"{outputPath}.m3u8\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();

            // เริ่มอ่านข้อมูล Output และ Error แบบ Asynchronous
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(); // รอให้ Process เสร็จสิ้นแบบ Asynchronous

            if (process.ExitCode != 0)
            {
                // ตรวจสอบ Error ถ้ามีปัญหา
                var errorOutput = errorBuilder.ToString();
                Console.WriteLine($"Error in FFmpeg: {errorOutput}");
            }
        }


    }
}