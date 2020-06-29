# 一个粗糙的B站录播姬

## 简单说明

这是一个很粗糙的B站录播姬

建议运行在NAS、路由器、电视盒子等不需要关机的设备中

云服务器好像会被B站屏蔽IP，自行测试

## 配置说明

配置文件：appsetting.json

- savepath：录播文件保存路径，空的话就默认当前程序运行路径
- room：直播房间号，多房间用半角逗号分隔
- converter：flv直播文件转换，启用的话需要依赖[FFmpeg](https://ffmpeg.org/download.html)
- proxy：HTTP代理

## 使用

- [安装.NET Core Runtime](https://dotnet.microsoft.com/download/dotnet-core/3.1)
- 执行命令：`dotnet BililiveRecorder.dll`
- linux下可通过systemctl后台运行

```
[Unit]
Description=Bilibili Service
After=network.target

[Service]
WorkingDirectory=/mnts/bilibili
User=root
Restart=always
RestartSec=5s
ExecStart=/usr/bin/dotnet /mnts/bilibili/BililiveRecorder.dll
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

- 后续增加Docker运行方式


## 参考资料 & 鸣谢

- [Bililive/BililiveRecorder](https://github.com/Bililive/BililiveRecorder)