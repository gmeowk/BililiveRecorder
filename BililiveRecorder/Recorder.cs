using BililiveRecorder.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace BililiveRecorder
{
    public class Recorder : BackgroundService
    {
        private readonly ILogger<Recorder> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _cfg;
        private readonly IConvertMediaTaskQueue _taskQueue;

        public Recorder(ILogger<Recorder> logger, IHttpClientFactory httpClientFactory, IConfiguration cfg, IConvertMediaTaskQueue taskQueue)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cfg = cfg;
            _taskQueue = taskQueue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var _room = _cfg.GetSection("room").Get<int[]>();
            foreach (var item in _room)
            {
                _ = Start(item);
            }
        }

        private async Task Start(int roomId)
        {
            var _isStart = false;
            while(!_isStart)
            {
                _isStart = await IsStartRecorderAsync(roomId);
                if (!_isStart)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            }
            var _url = await GetUrlAsync(roomId);
            await RecorderAsync(_url, roomId);
            await Start(roomId);
        }

        /// <summary>
        /// 判断是否开播
        /// </summary>
        /// <param name="roomId">直播间ID</param>
        /// <returns></returns>
        private async Task<bool> IsStartRecorderAsync(int roomId)
        {
            try
            {
                using (var client = _httpClientFactory.CreateClient("live"))
                {
                    using (var _response = await client.GetAsync($"room/v1/Room/get_info?id={roomId}"))
                    {
                        var _remoteResult = await _response.Content.ReadAsStringAsync();
                        if (_response.IsSuccessStatusCode)
                        {
                            var _result = JsonSerializer.Deserialize<RoomInfo>(_remoteResult);
                            var _isStream = _result.data.live_status == 1 ? true : false;
                            return _isStream;
                        }
                        _logger.LogInformation(_remoteResult);
                    }
                    throw new Exception("获取直播状态失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return await IsStartRecorderAsync(roomId);
            }
        }

        /// <summary>
        /// 获取播放地址
        /// </summary>
        /// <param name="roomId">直播间ID</param>
        /// <returns></returns>
        private async Task<string> GetUrlAsync(int roomId)
        {
            try
            {
                using (var client = _httpClientFactory.CreateClient("live"))
                {
                    using (var _response = await client.GetAsync($"room/v1/Room/playUrl?cid={roomId}&quality=4&platform=web"))
                    {
                        if (_response.IsSuccessStatusCode)
                        {
                            var _remoteResult = await _response.Content.ReadAsStringAsync();
                            _logger.LogInformation(_remoteResult);
                            var _status = int.Parse(JObject.Parse(_remoteResult)["code"].ToString());
                            if (_status == 0)
                            {
                                var _result = JsonSerializer.Deserialize<PlayUrl>(_remoteResult);
                                var urls = _result.data.durl.Select(o => o.url).ToList();
                                return urls[new Random().Next(urls.Count)];
                            }
                        }
                    }
                    throw new Exception("未找到播放地址");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex.StackTrace);
                return await GetUrlAsync(roomId);
            }
        }

        /// <summary>
        /// 开始录制
        /// </summary>
        /// <param name="url">直播流地址</param>
        /// <param name="roomId">直播间ID</param>
        /// <returns></returns>
        private async Task RecorderAsync(string url, int roomId)
        {
            var _basePath = Directory.GetCurrentDirectory();
            if (!string.IsNullOrWhiteSpace(_cfg.GetValue<string>("savepath")))
            {
                _basePath = _cfg.GetValue<string>("savepath");
            }
            var _path = Path.Combine(_basePath, roomId.ToString());
            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
            }
            var _filePath = Path.Combine(_path, $"{DateTime.Now:yyyyMMdd_HHmmss}.flv");
            try
            {
                using (var client = _httpClientFactory.CreateClient("live"))
                {
                    using (var _response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (_response.IsSuccessStatusCode)
                        {
                            var _responseStream = await _response.Content.ReadAsStreamAsync();
                            var _fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                            await _responseStream.CopyToAsync(_fileStream);
                        }
                        else if (_response.StatusCode == HttpStatusCode.Redirect || _response.StatusCode == HttpStatusCode.Moved)
                        {
                            _response.Dispose();
                            client.Dispose();
                            await RecorderAsync(_response.Headers.Location.OriginalString, roomId);
                        }
                        else if (_response.StatusCode != HttpStatusCode.NotFound)
                        {
                            _logger.LogInformation(await _response.Content.ReadAsStringAsync());
                            throw new Exception("未能正确接收直播流");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex.StackTrace);
                await Start(roomId);
            }
            finally
            {
                if (_cfg.GetValue<bool>("converter:isEnable"))
                {
                    ConvertExtension(_filePath);
                }
            }
        }

        /// <summary>
        /// 格式转换
        /// </summary>
        /// <param name="_filePath"></param>
        private void ConvertExtension(string _filePath)
        {
            if (File.Exists(_filePath))
            {
                _taskQueue.ConvertMediaTaskItem(async token =>
                {
                    try
                    {
                        var _fileinfo = new FileInfo(_filePath);
                        string outputFileName = Path.ChangeExtension(_fileinfo.FullName, _cfg.GetValue<string>("converter:extension"));
                        var mediaInfo = await FFmpeg.GetMediaInfo(_fileinfo.FullName);
                        //var videoStream = mediaInfo.VideoStreams.First();
                        //var audioStream = mediaInfo.AudioStreams.First();
                        //videoStream.Rotate(RotateDegrees.CounterClockwise).SetSize(VideoSize.Hd1080).SetCodec(VideoCodec.h264);
                        var conversion = await FFmpeg.Conversions.FromSnippet.Convert(_fileinfo.FullName, outputFileName);
                        conversion.SetOutput(outputFileName).SetOverwriteOutput(true).UseMultiThread(true).SetPreset(ConversionPreset.UltraFast);
                        //conversion.OnProgress += async (sender, args) =>
                        //{
                        //    _logger.LogInformation($"[{args.Duration}/{args.TotalLength}][{args.Percent}%] {_fileinfo.Name}");
                        //};
                        await conversion.Start();
                        _logger.LogInformation($"【{_fileinfo.Name}】转换完成");
                        if (_cfg.GetValue<bool>("converter:isDeleteOriFile"))
                        {
                            File.Delete(_filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                });
            }
        }
    }
}
