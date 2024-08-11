using System.Text;
using FileFlows.Plugin;
using FileFlows.Server.Controllers;
using FileFlows.Server.Services;

namespace FileFlows.Server.Middleware;

/// <summary>
/// A middleware used to log all requests
/// </summary>
public class LoggingMiddleware
{
    /// <summary>
    /// Next request delegate
    /// </summary>
    private readonly RequestDelegate _next;

    private SettingsService _settingsService;
    /// <summary>
    /// Settings service
    /// </summary>
    private SettingsService SettingsService
    {
        get
        {
            if (_settingsService == null)
                _settingsService = (SettingsService)ServiceLoader.Load<ISettingsService>();
            return _settingsService;
        }
    }
    
    
    /// <summary>
    /// Gets the logger for the request logger
    /// </summary>
    public static FileLogger RequestLogger { get; private set; }

    /// <summary>
    /// Constructs a instance of the exception middleware
    /// </summary>
    /// <param name="next">the next middleware to call</param>
    public LoggingMiddleware(RequestDelegate next)
    {
        _next = next;
        RequestLogger = new FileLogger(DirectoryHelper.LoggingDirectory, "FileFlowsHTTP", register: false);
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    /// <param name="context">the HttpContext executing this middleware</param>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        finally
        {
            try
            {
                if (WebServer.FullyStarted)
                {   
                    LogType logType = LogType.Info;
                    int statusCode = context.Response?.StatusCode ?? 0;

                    if (context.Request.Path.Value?.Contains("remote/library-file/manually-add") == true)
                        logType = LogType.Debug;
                    if (statusCode is >= 300 and < 400)
                        logType = LogType.Warning;
                    else if (statusCode is >= 400 and < 500)
                        logType = LogType.Warning;
                    else if (statusCode >= 500)
                        logType = LogType.Error;
                    
                    if (logType != LogType.Info || SettingsService.Get().Result.LogEveryRequest)
                    {
                        if (logType == LogType.Debug)
                        {
                            // Enable buffering so the request can be read multiple times
                            context.Request.EnableBuffering();

                            // Read the request body
                            string requestBody = await ReadRequestBodyAsync(context.Request);
                            _ = RequestLogger.Log(logType,
                                $"REQUEST [{context.Request?.Method}] [{context.Response?.StatusCode}]: {context.Request?.Path.Value}\n" + 
                                requestBody);

                            // Reset the request body stream position so the next middleware can read it
                            context.Request.Body.Position = 0;
                            
                        }
                        else
                        {
                            _ = RequestLogger.Log(logType,
                                $"REQUEST [{context.Request?.Method}] [{context.Response?.StatusCode}]: {context.Request?.Path.Value}");
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }
        
    /// <summary>
    /// Reads the entire request body from the <see cref="HttpRequest"/> stream asynchronously.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/> from which to read the body.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the request body as a string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="request"/> is <c>null</c>.</exception>
    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        // Read the request body stream
        using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
        {
            string body = await reader.ReadToEndAsync();
            return body;
        }
    }
}
