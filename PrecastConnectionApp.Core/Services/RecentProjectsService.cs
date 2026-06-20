using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PrecastConnectionApp.Models;

namespace PrecastConnectionApp.Services
{
    public class RecentProjectsService
    {
        private readonly string _settingsFilePath;

        public RecentProjectsService()
        {
            // Save settings in local AppData folder or beside executable
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "PrecastConnectionApp");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            _settingsFilePath = Path.Combine(appFolder, "recent_projects.json");
        }

        public List<RecentProject> LoadRecentProjects()
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new List<RecentProject>();
            }

            try
            {
                var json = File.ReadAllText(_settingsFilePath);
                var projects = JsonConvert.DeserializeObject<List<RecentProject>>(json);
                return projects?.OrderByDescending(p => p.LastUpdated).ToList() ?? new List<RecentProject>();
            }
            catch
            {
                return new List<RecentProject>();
            }
        }

        public void AddOrUpdateProject(string filePath, ProjectData projectData = null)
        {
            var projects = LoadRecentProjects();
            var projectName = Path.GetFileNameWithoutExtension(filePath);
            Guid projectId = Guid.Empty;

            int total = 0, safe = 0, unsafeCount = 0, review = 0;
            if (projectData != null)
            {
                if (!string.IsNullOrWhiteSpace(projectData.ProjectName))
                {
                    projectName = projectData.ProjectName;
                }
                projectId = projectData.Id;

                total = projectData.Walls.Count;
                if (projectData.Walls != null)
                {
                    safe = projectData.Walls.Count(r => r.Status == "SAFE");
                    unsafeCount = projectData.Walls.Count(r => r.Status == "UNSAFE");
                }
                review = projectData.Walls.Count(r => r.Status == "REVIEW_REQUIRED" || r.Status == "PENDING");
            }

            var existing = projects.FirstOrDefault(p => p.Id == projectId && projectId != Guid.Empty);
            if (existing == null)
            {
                existing = projects.FirstOrDefault(p => string.Equals(p.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            }

            if (existing != null)
            {
                existing.LastUpdated = DateTime.Now;
                existing.ProjectName = projectName;
                existing.FilePath = filePath;
                if (projectId != Guid.Empty)
                {
                    existing.Id = projectId;
                }
                if (projectData != null)
                {
                    existing.Location = projectData.Location;
                    existing.TotalColumns = total;
                    existing.SafeColumns = safe;
                    existing.UnsafeColumns = unsafeCount;
                    existing.ReviewColumns = review;
                }
            }
            else
            {
                projects.Add(new RecentProject
                {
                    Id = projectId,
                    ProjectName = projectName,
                    Location = projectData?.Location,
                    FilePath = filePath,
                    LastUpdated = DateTime.Now,
                    TotalColumns = total,
                    SafeColumns = safe,
                    UnsafeColumns = unsafeCount,
                    ReviewColumns = review
                });
            }

            // Keep top 50 recent projects
            projects = projects.OrderByDescending(p => p.LastUpdated).Take(50).ToList();
            SaveRecentProjects(projects);
        }

        public void RemoveProject(string filePath)
        {
            var projects = LoadRecentProjects();
            var item = projects.FirstOrDefault(p => string.Equals(p.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                projects.Remove(item);
                SaveRecentProjects(projects);
            }
        }

        private void SaveRecentProjects(List<RecentProject> projects)
        {
            try
            {
                var json = JsonConvert.SerializeObject(projects, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // Ignore save errors for settings
            }
        }
    }
}
