using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace BililiveRecorder
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    IConfigurationRoot Configuration = builder.Build();
                    services.AddHttpClient("live", client =>
                    {
                        client.BaseAddress = new Uri(Configuration.GetValue<string>("api:live"));
                        client.Timeout = TimeSpan.FromMilliseconds(30000);
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                        client.DefaultRequestHeaders.UserAgent.Clear();
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.113 Safari/537.36");
                        client.DefaultRequestHeaders.Referrer = new Uri("https://live.bilibili.com");
                        client.DefaultRequestHeaders.Add("Origin", "https://live.bilibili.com");
                    }).ConfigurePrimaryHttpMessageHandler(()=>
                    {
                        var _handler = new HttpClientHandler();
                        if (Configuration.GetValue<bool>("proxy:isEnable"))
                        {
                            _handler.Proxy = new WebProxy(new Uri("http://" + Configuration.GetValue<string>("proxy:address")));
                        }
                        return _handler;
                    });
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Information);
                        builder.AddNLog("nlog.config");
                    });
                    services.AddHostedService<QueuedHostedService>();
                    services.AddHostedService<Recorder>();
                    services.AddSingleton<IConvertMediaTaskQueue, ConvertMediaTaskQueue>();
                });
    }
}
