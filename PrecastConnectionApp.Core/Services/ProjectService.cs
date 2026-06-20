using System.IO;
using Newtonsoft.Json;
using PrecastConnectionApp.Models;

namespace PrecastConnectionApp.Services
{
    public class ProjectService
    {
        public void SaveProject(string path, ProjectData data)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public ProjectData LoadProject(string path)
        {
            if (!File.Exists(path))
            {
                return new ProjectData();
            }

            var json = File.ReadAllText(path);
            var data = JsonConvert.DeserializeObject<ProjectData>(json);
            return data ?? new ProjectData();
        }
    }
}
