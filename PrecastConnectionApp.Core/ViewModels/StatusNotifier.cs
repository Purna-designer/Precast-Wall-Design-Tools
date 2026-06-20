using CommunityToolkit.Mvvm.ComponentModel;

namespace PrecastConnectionApp.ViewModels
{
    public partial class StatusNotifier : ObservableObject
    {
        private static readonly StatusNotifier _instance = new StatusNotifier();
        public static StatusNotifier Instance => _instance;

        private string _message = "Ready";
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private string _commandLog = "Ready\n";
        public string CommandLog
        {
            get => _commandLog;
            set => SetProperty(ref _commandLog, value);
        }

        public void SetStatus(string status)
        {
            Message = status;
            CommandLog += $"[{System.DateTime.Now:HH:mm:ss}] {status}\n";
        }
    }
}
