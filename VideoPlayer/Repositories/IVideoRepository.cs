using System.Collections.Generic;

namespace VideoPlayer.Repositories;

// สร้าง Interface สำหรับ VideoRepository
public interface IVideoRepository
{
    List<Video> GetAllVideos();
    Video GetVideoById(int id);
    void SaveData(Video video);
}

