using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Services;
using System;
using Models;
using System.Linq;
using Model;

namespace FundEstimateWebClient
{
    internal class WebClient
    {
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly CancellationToken _cancellationToken;
        readonly Task _task;

        public string FileLocation = string.Empty;
        public bool LogToConsole = false;
        public bool LogToFile = false;

        private HelperService helperService;
        private DatabaseService databaseService;
        private WebClientService webClientService;
        private Settings settings = new Settings();


        public WebClient()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            _task = new Task(DoWork, _cancellationToken);
        }

        public void Start()
        {
            _task.Start();
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _task.Wait();
        }

        private void DoWork()
        {            
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                settings = LoadJson(GetPath(""));
                FileLocation = settings.FileSettings.FilePath;
                LogToFile = settings.ThreadSettings.LogToFile;
                LogToConsole = settings.ThreadSettings.LogToConsole;
                string logText = "";

                try
                {
                    databaseService = new DatabaseService(settings.ConnectionStrings.DefaultConnection);
                    webClientService = new WebClientService();
                    helperService = new HelperService();

                    logText = $"{DateTime.Now:u}\t\t:STARTING";

                    helperService.Consoler(settings.ThreadSettings.LogToConsole, settings.ThreadSettings.LogToFile, logText, FileLocation).Wait(_cancellationToken);

                    this.SendFundEstimatesToSR().Wait(_cancellationToken);

                    logText = $"{DateTime.Now:u}\t\t:SendFundEstimatesToSR";
                    helperService.Consoler(settings.ThreadSettings.LogToConsole, settings.ThreadSettings.LogToFile, logText, FileLocation).Wait(_cancellationToken);

                    var sleep = int.Parse(settings.ThreadSettings.SleepMinutes) * 3000;
                    Thread.Sleep(sleep);
                    logText = $"{DateTime.Now:u}\t\t:SLEPT FOR {sleep}";
                    helperService.Consoler(true, settings.ThreadSettings.LogToFile, logText, FileLocation).Wait(_cancellationToken);
                }
                catch (Exception e)
                {
                    helperService.Consoler(true, settings.ThreadSettings.LogToFile, e.ToString(), FileLocation).Wait(_cancellationToken);
                }
                if (_cancellationToken.IsCancellationRequested)
                    helperService.Consoler(true, settings.ThreadSettings.LogToFile, _cancellationToken.ToString(), FileLocation).Wait(_cancellationToken);

            }
        }
        public async Task SendFundEstimatesToSR()
        {
            var pendingFundEstimates = await databaseService.GetFundEstimates().ConfigureAwait(false);
            string url = settings.UrlAddress.Url;
            if (pendingFundEstimates.Response.Count != 0)
            {
                PostResult postResult = await webClientService.FundEstimateOut(pendingFundEstimates, url);
                if (postResult.StatusCode == "SUCCESS" && pendingFundEstimates.Response.Any())
                {
                    await databaseService.SetSentStatus(pendingFundEstimates);
                }
            }
        }
        public Settings LoadJson(string path)
        {
            Settings conStr;
            using (var r = new StreamReader(path + "configuration.json"))
            {
                var json = r.ReadToEnd();
                conStr = JsonConvert.DeserializeObject<Settings>(json);
            }
            return conStr;
        }
        public static string GetPath(string _DirectoryName)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), _DirectoryName);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path + "\\";
        }
    }
}