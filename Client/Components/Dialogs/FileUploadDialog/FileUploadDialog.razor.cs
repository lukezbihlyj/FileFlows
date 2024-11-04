using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Partial class for handling the file upload dialog logic.
/// </summary>
public partial class FileUploadDialog : IModal
{
    private string lblTitle, lblCancel, lblEta, lblSpeed, lblUploaded, lblComplete;
    private long uploadedBytes;
    private long totalBytes;
    private bool isUploading;
    private CancellationTokenSource cts;
    private IBrowserFile FileUploading;
    private DateTime startTime;
    private double uploadSpeed;
    private TimeSpan eta;
    private string formattedSpeed;
    private string formattedEta;
    private string formattedUploaded;
    
    /// <inheritdoc />
    [Parameter]
    public IModalOptions Options { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TaskCompletionSource<object> TaskCompletionSource { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblCancel = Translater.Instant("Labels.Cancel");
        lblTitle = Translater.Instant("Labels.Upload");
        lblEta = Translater.Instant("Dialogs.FileUpload.ETA");
        lblSpeed = Translater.Instant("Dialogs.FileUpload.Speed");
        lblUploaded = Translater.Instant("Dialogs.FileUpload.UploadedAmount");
        lblComplete = Translater.Instant("Dialogs.FileUpload.Complete");
        if (Options is FileUploadOptions fileUploadOptions == false || fileUploadOptions.File == null)
        {
            Cancel();
            return;
        }

        FileUploading = fileUploadOptions.File;
        Logger.Instance.ILog("File: " + fileUploadOptions.File.Name);
        
        // Begin upload if file is valid
        cts = new CancellationTokenSource();
        await UploadFile(FileUploading, cts.Token);
    }

    /// <summary>
    /// Closes the file upload dialog.
    /// </summary>
    public void Close()
    {
        TaskCompletionSource.TrySetCanceled(); // Set result when closing
    }

    public void Cancel()
    {
        TaskCompletionSource.TrySetCanceled(); // Indicate cancellation
    }
    /// <summary>
    /// Uploads the file with progress and cancellation support.
    /// </summary>
    private async Task UploadFile(IBrowserFile file, CancellationToken cancellationToken)
    {
        string url = "/api/library-file/upload";
#if (DEBUG)
        url = "http://localhost:6868" + url;
#endif
        totalBytes = file.Size;
        startTime = DateTime.Now;
        
        var progress = new Progress<long>(bytes =>
        {
            uploadedBytes = bytes;
            UpdateUploadMetrics();
            InvokeAsync(StateHasChanged);
        });

        using var stream = file.OpenReadStream(maxAllowedSize: long.MaxValue);
        var content = new ProgressableStreamContent(stream, progress);

        using var form = new MultipartFormDataContent();
        form.Add(content, "file", file.Name);

        isUploading = true;
        var response = await HttpHelper.Client.PostAsync(url, form, cancellationToken);

        isUploading = false;
        if (response.IsSuccessStatusCode)
        {
            var uploadedFile = await response.Content.ReadAsStringAsync(cancellationToken);
            TaskCompletionSource.TrySetResult(uploadedFile);
        }
        else
            TaskCompletionSource.TrySetResult("Upload Failed");
    }

    private void UpdateUploadMetrics()
    {
        var elapsedTime = DateTime.Now - startTime;
        uploadSpeed = uploadedBytes / elapsedTime.TotalSeconds;
        eta = TimeSpan.FromSeconds((totalBytes - uploadedBytes) / uploadSpeed);

        formattedSpeed = FormatSpeed(uploadSpeed);
        formattedEta = eta.ToString(@"hh\:mm\:ss");
        formattedUploaded = $"{FormatBytes(uploadedBytes)} / {FormatBytes(totalBytes)}";
    }

    private string FormatSpeed(double speed)
    {
        if (speed > 1_000_000)
            return $"{(speed / 1_000_000):F2} MB/s";
        if (speed > 1000)
            return $"{(speed / 1000):F2} KB/s";
        return $"{speed:F2} Bytes/s";
    }

    private string FormatBytes(double bytes)
    {
        if (bytes >= 1_000_000_000) // GB
            return $"{bytes / 1_000_000_000:F2} GB";
        if (bytes >= 1_000_000) // MB
            return $"{bytes / 1_000_000:F2} MB";
        if (bytes >= 1000) // KB
            return $"{bytes / 1000:F2} KB";
        return $"{bytes:F2} Bytes";
    }
}

/// <summary>
/// The File Upload Options
/// </summary>
public class FileUploadOptions : IModalOptions
{
    /// <summary>
    /// Gets or sets the file being uploaded
    /// </summary>
    public IBrowserFile File { get; set; }
}