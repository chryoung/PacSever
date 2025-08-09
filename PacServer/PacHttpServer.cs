using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PacServer;

public class PacHttpServer
{
    private HttpListener? listener;
    private string pacContent = "";
    private int port = 12345;
    private CancellationTokenSource? cts;

    public string ServerUrl => $"http://localhost:{port}/proxy.pac";

    public void SetPacContent(string content) => pacContent = content;

    public void Start()
    {
        if (listener != null && listener.IsListening) return;
        listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        cts = new CancellationTokenSource();
        Task.Run(() => ListenLoop(cts.Token));
    }

    public void Stop()
    {
        cts?.Cancel();
        listener?.Stop();
    }

    private async Task ListenLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var ctx = await listener!.GetContextAsync();
            if (ctx.Request.Url?.AbsolutePath == "/proxy.pac")
            {
                ctx.Response.ContentType = "application/x-ns-proxy-autoconfig";
                var buffer = System.Text.Encoding.UTF8.GetBytes(pacContent ?? "");
                ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
                ctx.Response.Close();
            }
            else
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.Close();
            }
        }
    }
}