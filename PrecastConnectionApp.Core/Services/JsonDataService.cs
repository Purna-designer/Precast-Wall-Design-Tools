using System.IO;
using Newtonsoft.Json;
using PrecastConnectionApp.Models;

namespace PrecastConnectionApp.Services
{
    public class JsonDataService
    {
        public void SaveProjectData(string filePath, ProjectData data)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public ProjectData LoadProjectData(string filePath)
        {
            if (!File.Exists(filePath))
                return new ProjectData();
            
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<ProjectData>(json) ?? new ProjectData();
        }
    }
}
