﻿#define UseOptions // or NoOptions
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EchoApp
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);
            loggerFactory.AddDebug(LogLevel.Debug);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

#if NoOptions
            #region UseWebSockets
            app.UseWebSockets();
            #endregion
#endif
#if UseOptions
            #region UseWebSocketsOptions
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024
            };
            app.UseWebSockets(webSocketOptions);
            #endregion
#endif
            #region AcceptWebSocket
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await Echo(context, webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }

            });
#endregion
            app.UseFileServer();
        }

        static ArrayList sockets = new ArrayList();
        public static void gotChanged(String str){
            Console.WriteLine("gotChanged : " + str);

            Task.Run(async ()=>{
                var idx = 0;
                foreach(WebSocket soc in sockets){
                    try{
                        // EchoApp.Program.logDebug("send["+(idx++)+"] : " + str);
                        Console.WriteLine("send["+(idx++)+"] : " + str);
                        await soc.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(str)), System.Net.WebSockets.WebSocketMessageType.Text, true, new CancellationToken());
                    }
                    catch(Exception e){

                    }
                }
            });
        }
#region Echo
        private async Task Echo(HttpContext context, WebSocket webSocket)
        {
            sockets.Add(webSocket);
            var t = Task.Delay(100).ContinueWith(async (t1)=>{
	            await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("connected..." + EchoApp.Program.region)), System.Net.WebSockets.WebSocketMessageType.Text, true, new CancellationToken());
	            // await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(EchoApp.Program.debuglog)), System.Net.WebSockets.WebSocketMessageType.Text, true, new CancellationToken());
            });

            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                // var msg = new ArraySegment<byte>(buffer, 0, result.Count).ToString();
                var msg = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);

                EchoApp.Program.app.newRecord(msg);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            sockets.Remove(webSocket);
        }

        private async Task Echo2(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
#endregion
    }
}
