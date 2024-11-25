// using FileFlows.RemoteServices;
// using FileFlows.ServerShared.Helpers;
// using FileFlows.ServerShared.Models;
// using FileFlows.ServerShared.Services;
// using FileFlows.ServerShared.Workers;
// using FileFlows.Shared.Helpers;
// using FileFlows.Shared.Models;
// using Microsoft.Extensions.Logging;
//
// namespace FileFlows.Node.Workers;
//
// /// <summary>
// /// Worker that gathers system statistics 
// /// </summary>
// public class SystemStatisticsWorker():Worker(ScheduleType.Second, 10, quiet: true)
// {
//     private ProcessingNode? processingNode; 
//     
//     /// <inheritdoc />
//     protected override void Execute()
//     {
//         if(processingNode == null)
//         {
//             var nodeService = ServiceLoader.Load<INodeService>();
//             processingNode =
//                 nodeService.GetByAddressAsync(AppSettings.ForcedHostName?.EmptyAsNull() ?? Environment.MachineName).Result;
//             if (processingNode == null)
//                 return;
//         }
//         
//         var tempStorage = GetTempStorageSize();
//         var logStorage = GetLogStorageSize();
//         ServiceLoader.Load<INodeService>().RecordNodeSystemStatistics(new()
//         {
//             Uid = processingNode.Uid,
//             TemporaryDirectorySize = tempStorage,
//             LogDirectorySize = logStorage
//         }).Wait();
//     }
//
//     private DirectorySize GetTempStorageSize()
//     {
//         var tempPath = processingNode?.TempPath;
//         if (string.IsNullOrEmpty(tempPath))
//             return new DirectorySize();
//         return GetDirectorySize(tempPath);
//     }
//
//     private DirectorySize GetLogStorageSize()
//     {
//         string path = DirectoryHelper.LoggingDirectory;
//         return GetDirectorySize(path, pattern: "*.log");
//     }
//     
//     private DirectorySize GetDirectorySize(string path, string pattern = "*.*")
//     {
//         long size = 0;
//         if (string.IsNullOrEmpty(path) == false)
//         {
//             try
//             {
//                 var dir = new DirectoryInfo(path);
//                 if (dir.Exists)
//                 {
//                     size = dir.EnumerateFiles(pattern, SearchOption.AllDirectories).Sum(x => x.Length);
//                     // Logger.Instance.DLog($"Directory '{dir.FullName}' size: " + FileSizeHelper.Humanize(size));
//                 }
//             }
//             catch (Exception)
//             {
//                 // Ignored
//             }
//         }
//
//         return new()
//         {
//             Path = path,
//             Size = size
//         };
//     }
// }