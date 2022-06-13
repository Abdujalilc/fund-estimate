using System;
using System.Configuration;
using Topshelf;

namespace FundEstimateWebClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceName = ConfigurationManager.AppSettings["ServiceName"];
            var serviceDisplayName = ConfigurationManager.AppSettings["ServiceDisplayName"];
            var serviceDescription = ConfigurationManager.AppSettings["ServiceDescription"];

            var serviceRunner = HostFactory.Run(config =>
            {
                config.Service<WebClient>(service =>
                {
                    service.ConstructUsing(name => new WebClient());
                    service.WhenStarted(serviceConfig => serviceConfig.Start());
                    service.WhenStopped(serviceConfig => serviceConfig.Stop());
                });
                config.RunAsLocalSystem();

                config.SetServiceName(serviceName);
                config.SetDisplayName(serviceDisplayName);
                config.SetDescription(serviceDescription);
            });

            var exitCode = (int)Convert.ChangeType(serviceRunner, serviceRunner.GetTypeCode());
            Environment.ExitCode = exitCode;
        }
    }
}
