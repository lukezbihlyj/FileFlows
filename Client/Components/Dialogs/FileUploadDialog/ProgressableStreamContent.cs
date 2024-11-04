using System.Net;
using System.Net.Http;
using System.IO;
using System.Threading;

namespace FileFlows.Client.Components.Dialogs;

public class ProgressableStreamContent : HttpContent
{
    private const int DefaultBufferSize = 81920;
    private readonly Stream content;
    private readonly IProgress<long> progress;
    private readonly int bufferSize;

    public ProgressableStreamContent(Stream content, IProgress<long> progress, int bufferSize = DefaultBufferSize)
    {
        this.content = content ?? throw new ArgumentNullException(nameof(content));
        this.progress = progress;
        this.bufferSize = bufferSize;
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        throw new NotImplementedException();
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        var buffer = new byte[bufferSize];
        long uploaded = 0;
        int bytesRead;
        while ((bytesRead = await content.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await stream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            uploaded += bytesRead;
            progress?.Report(uploaded);
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = content.Length;
        return true;
    }
}