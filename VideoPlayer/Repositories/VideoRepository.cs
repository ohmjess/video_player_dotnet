using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace VideoPlayer.Repositories
{
    public class VideoRepository : IVideoRepository
    {
        private readonly string _connectionString;

        // Constructor ที่รับ Connection String ผ่าน Dependency Injection หรือกำหนดเอง
        public VideoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ฟังก์ชันสำหรับดึงข้อมูลวิดีโอทั้งหมดจากฐานข้อมูล
        public List<Video> GetAllVideos()
        {
            var videos = new List<Video>();

            // ใช้ MySqlConnection เพื่อเชื่อมต่อกับฐานข้อมูล
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                // SQL Query สำหรับดึงข้อมูล
                string query = "SELECT id, title, description, video_url FROM video";

                // ใช้ MySqlCommand เพื่อรัน Query
                MySqlCommand command = new MySqlCommand(query, connection);

                try
                {
                    // เปิดการเชื่อมต่อฐานข้อมูล
                    connection.Open();
                    MySqlDataReader reader = command.ExecuteReader();

                    // อ่านข้อมูลจากฐานข้อมูล
                    while (reader.Read())
                    {
                        var video = new Video
                        {
                            Id = reader.GetInt32(0), // คอลัมน์ที่ 1: Id
                            Title = reader.GetString(1), // คอลัมน์ที่ 2: Title
                            Description = reader.GetString(2), // คอลัมน์ที่ 3: Description
                            Url = reader.GetString(3) // คอลัมน์ที่ 4: Url
                        };
                        videos.Add(video);
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    // จัดการข้อผิดพลาดที่เกิดขึ้น
                    Console.WriteLine("Error while fetching videos: " + ex.Message);
                }
            }

            return videos;
        }

        public Video GetVideoById(int id)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                string query = "SELECT * FROM video WHERE Id = @Id";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                try
                {
                    connection.Open();
                    MySqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        return new Video
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(2),
                            Description = reader.GetString(3),
                            Url = reader.GetString(4)
                        };
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while fetching video: " + ex.Message);
                }
                finally
                {
                    connection.Close();
                }

                return null; // Return null if no video is found
            }

        }

        public void SaveData(Video video)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                string query = "INSERT INTO video (title, description, video_url) VALUES (@title, @description, @url)";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@title", video.Title);
                    command.Parameters.AddWithValue("@description", video.Description);
                    command.Parameters.AddWithValue("@url", video.Url);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error while saving video: " + ex.Message);
                    }
                }
            }
        }
    }

    // ตัวอย่าง Entity สำหรับ Video
    public class Video
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
    }
}
