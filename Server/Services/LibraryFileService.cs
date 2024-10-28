using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Esprima.Ast;
using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using FileFlows.Server.Hubs;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;
using Humanizer;
using ILogger = FileFlows.Plugin.ILogger;

namespace FileFlows.Server.Services;

public class LibraryFileService 
{
    private static FairSemaphore NextFileSemaphore = new (1);

    private static Logger NextFileLogger;
    
    /// <summary>
    /// Constructs a next library file result
    /// </summary>
    /// <param name="status">the status of the call</param>
    /// <param name="file">the library file to process</param>
    /// <returns>the next library file result</returns>
    private NextLibraryFileResult NextFileResult(NextLibraryFileStatus? status = null, LibraryFile file = null)
    {
        NextLibraryFileResult result = new();
        if (status != null)
            result.Status = status.Value;
        result.File = file;
        return result;
    }


    /// <summary>
    /// Get all matching files
    /// </summary>
    /// <param name="status">the status to get</param>
    /// <param name="skip">how many files to skip</param>
    /// <param name="rows">the rows to get</param>
    /// <param name="filter">a text filter</param>
    /// <param name="allLibraries">all libraries in the system</param>
    /// <param name="node">[Optional] a specific node to match against</param>
    /// <param name="library">[Optional] library to filter by</param>
    /// <param name="flow">[Optional] flow to filter by</param>
    /// <param name="sortBy">[Optional] sort by method</param>
    /// <returns>the matching files</returns>
    public async Task<List<LibraryFile>> GetAll(FileStatus? status = null, int skip = 0, int rows = 0,
        string filter = null,
        List<Library>? allLibraries = null, Guid? node = null, Guid? library = null, Guid? flow = null,
        FilesSortBy? sortBy = null)
    {
        allLibraries ??= (await ServiceLoader.Load<LibraryService>().GetAllAsync());

        var sysInfo = new LibraryFilterSystemInfo()
        {
            AllLibraries = allLibraries.ToDictionary(x => x.Uid, x => x),
            Executors = FlowRunnerService.Executors.Values.ToList(),
            LicensedForProcessingOrder = LicenseHelper.IsLicensed(LicenseFlags.ProcessingOrder)
        };
        return await new LibraryFileManager().GetAll(new()
        {
            Status = status,
            Skip = skip,
            Rows = rows,
            Filter = filter,
            SysInfo = sysInfo,
            NodeUid = node,
            LibraryUid = library,
            FlowUid = flow,
            SortBy = sortBy
        });
    }
   
    /// <summary>
    /// Gets all files using the given filter
    /// </summary>
    /// <param name="filter">the filter</param>
    /// <returns>all matching files</returns>
    public Task<List<LibraryFile>> GetAll(LibraryFileFilter filter)
        => new LibraryFileManager().GetAll(filter);

    /// <summary>
    /// Gets the total items matching the filter
    /// </summary>
    /// <param name="filter">the filter</param>
    /// <returns>the total number of items matching</returns>
    public async Task<int> GetTotalMatchingItems(LibraryFileFilter filter)
    {
        // var allLibraries = await ServiceLoader.Load<LibraryService>().GetAllAsync();
        return await new LibraryFileManager().GetTotalMatchingItems(filter);
    }

    private readonly ConcurrentDictionary<Guid, DateTime> nodesThatCannotRun = new();

    /// <summary>
    /// Tells the server not to check this node for number of seconds when checking for load balancing as it will
    /// be unavailable for this amount of time
    /// </summary>
    /// <param name="nodeUid">the UID of the node</param>
    /// <param name="forSeconds">the time in seconds</param>
    public void NodeCannotRun(Guid nodeUid, int forSeconds)
    {
        var date = DateTime.Now.AddSeconds(forSeconds);
        NextFileLogger?.ILog($"Node '{nodeUid}' cannot run until {date}");
        nodesThatCannotRun[nodeUid] = date;
    }

    /// <summary>
    /// Gets the next file to process
    /// </summary>
    /// <param name="nodeName">the name of the node</param>
    /// <param name="nodeUid">the UID of the node</param>
    /// <param name="nodeVersion">the version of the node</param>
    /// <param name="workerUid">the UID of the worker service making this request</param>
    /// <returns>the result of the next file</returns>
    public async Task<NextLibraryFileResult> GetNext(string nodeName, Guid nodeUid, string nodeVersion, Guid workerUid)
    {
        await NextFileSemaphore.WaitAsync();
        try
        {
            if (NextFileLogger == null)
            {
                NextFileLogger = new();
                NextFileLogger.RegisterWriter(new FileLogger(DirectoryHelper.LoggingDirectory, "FileProcessRequest",
                    false));
            }

            StringLogger logger = new StringLogger();

            var result = await GetNextActual(logger, nodeName, nodeUid, nodeVersion, workerUid);
            var lines = logger.ToString().Split('\n');
            if (lines.Length == 1)
            {
                if(lines[0].Contains("No file found to process", StringComparison.InvariantCultureIgnoreCase))
                    NextFileLogger.ILog($"{nodeName} => {result.Status}");
                else
                    NextFileLogger.ILog($"{nodeName} => {result.Status}: {lines[0]}");
            }
            else
                NextFileLogger.ILog($"{nodeName} => {result.Status}\n{string.Join("\n", lines.Select(x => "                       " + x))}");

            if (result.File != null)
            {
                // record that this has started now, its not the complete start, but the flow runner has request it
                // by recording this now, we add the flow running extremely early into the life cycle and we can 
                // then limit the library runners, and wont have issues with "Unknown executor identifier" when using the file server
                FlowRunnerService.Executors[result.File.Uid] = new()
                {
                    Uid = result.File.Uid,
                    LibraryFile = result.File,
                    NodeName = nodeName,
                    NodeUid = nodeUid,
                    IsRemote = nodeUid != CommonVariables.InternalNodeUid,
                    RelativeFile = result.File.RelativePath,
                    Library = result.File.Library,
                    IsDirectory = result.File.IsDirectory,
                    StartedAt = DateTime.UtcNow,
                    LastUpdate = DateTime.UtcNow
                };
            }

            return result;
        }
        finally
        {
            NextFileSemaphore.Release();
        }
    }
    
    /// <summary>
    /// Actual code that gets the next file
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="nodeName">the name of the node</param>
    /// <param name="nodeUid">the UID of the node</param>
    /// <param name="nodeVersion">the version of the node</param>
    /// <param name="workerUid">the UID of the worker service making this request</param>
    /// <returns>the result of the next file</returns>
    private async Task<NextLibraryFileResult> GetNextActual(Plugin.ILogger logger, string nodeName, Guid nodeUid, string nodeVersion, Guid workerUid)
    {
        var nodeService = ServiceLoader.Load<NodeService>();
        await nodeService.UpdateLastSeen(nodeUid);

        if (UpdaterWorker.UpdatePending)
        {
            logger.ILog("Update pending. No file.");
            return NextFileResult(NextLibraryFileStatus.UpdatePending); // if an update is pending, stop providing new files to process
        }

        var settings = await ServiceLoader.Load<ISettingsService>().Get();
        if (settings.IsPaused)
        {
            logger.ILog("System is paused.  No file.");
            return NextFileResult(NextLibraryFileStatus.SystemPaused);
        }

        var node = await nodeService.GetByUidAsync(nodeUid);
        if (node != null && node.Version != nodeVersion)
        {
            node.Version = nodeVersion;
            await nodeService.UpdateVersion(node.Uid, nodeVersion);
        }
        
        if (nodeUid != CommonVariables.InternalNodeUid) // dont test version number for internal processing node
        {
            if (Version.TryParse(nodeVersion, out var nVersion) == false)
            {
                logger.WLog($"Invalid version '{nodeVersion}'.");
                return NextFileResult(NextLibraryFileStatus.InvalidVersion);
            }

            if (nVersion < Globals.MinimumNodeVersion)
            {
                logger.WLog(
                    $"Node '{nodeName}' version '{nVersion}' is less than minimum supported version '{Globals.MinimumNodeVersion}'");
                return NextFileResult(NextLibraryFileStatus.VersionMismatch);
            }
        }

        if (settings.EulaAccepted == false)
        {
            logger.WLog($"EULA not accepted.  No file.");
            return NextFileResult(NextLibraryFileStatus.SystemPaused);
        }

        if (await NodeEnabled(node) == false)
        {
            logger.ILog($"Node '{node.Name}' not enabled.  No file.");
            return NextFileResult(NextLibraryFileStatus.NodeNotEnabled);
        }

        var file = await GetNextLibraryFile(logger, node, workerUid);
        if (file == null)
        {
            logger.ILog($"No file found to process.");
            return NextFileResult(NextLibraryFileStatus.NoFile, file);
        }

        #region reset the file for processing
        try
        {
            // try to delete a log file for this library file if one already exists (in case the flow was cancelled and now its being re-run)                
            LibraryFileLogHelper.DeleteLogs(file.Uid);
        }
        catch (Exception)
        {
            logger.WLog($"Failed to delete old log file for file[{file.Uid}]: {file.Name}");
        }

        var library = await ServiceLoader.Load<LibraryService>().GetByUidAsync(file.LibraryUid!.Value);

        logger.ILog("Resetting file info for: " + file.Name);
        file.FinalSize = 0;
        file.FailureReason = string.Empty;
        file.OutputPath = string.Empty;
        file.ProcessOnNodeUid = null;
        file.ProcessingEnded = DateTime.MinValue;
        file.ExecutedNodes = new();
        file.FinalMetadata = new();
        file.OriginalMetadata= new();
        await new LibraryFileManager().ResetFileInfoForProcessing(file.Uid, 
            file.FlowUid != null && file.FlowUid != Guid.Empty ? file.FlowUid : library?.Flow?.Uid, 
            file.FlowUid != null && file.FlowUid != Guid.Empty ? file.FlowName : library?.Flow?.Name);
        #endregion
        logger.ILog($"File found to process: {file.Name}");
        
        return NextFileResult(NextLibraryFileStatus.Success, file);
    }

    /// <inheritdoc />
    public Task<LibraryFile?> Get(Guid uid)
        => new LibraryFileManager().Get(uid);

    /// <inheritdoc />
    public Task Delete(params Guid[] uids)
        => new LibraryFileManager().Delete(uids);

    /// <inheritdoc />
    public async Task<LibraryFile> Update(LibraryFile libraryFile)
    {
        // ensure the tags are known
        if (libraryFile.Tags?.Any() == true)
        {
            var knownTags = await new TagManager().GetAll();
            for(int i=libraryFile.Tags.Count - 1;i>=0;i--)
            {
                var tag = libraryFile.Tags[i];
                if (knownTags.Any(x => x.Uid == tag) == false)
                    libraryFile.Tags.RemoveAt(i);
            }
        }

        await new LibraryFileManager().UpdateFile(libraryFile);
        return libraryFile;
    }


    /// <summary>
    /// Gets the library status overview
    /// </summary>
    /// <returns>the library status overview</returns>
    public Task<List<LibraryStatus>> GetStatus()
        => new LibraryFileManager().GetStatus();
    
    /// <summary>
    /// Saves the full log for a library file
    /// Call this after processing has completed for a library file
    /// </summary>
    /// <param name="uid">The uid of the library file</param>
    /// <param name="log">the log</param>
    /// <returns>true if successfully saved log</returns>
    public async Task<bool> SaveFullLog(Guid uid, string log)
    {
        try
        {
            await LibraryFileLogHelper.SaveLog(uid, log, saveHtml: true);
            return true;
        }
        catch (Exception) { }
        return false;
    }

    /// <summary>
    /// Tests if a library file exists on server.
    /// This is used to test if a mapping issue exists on the node, and will be called if a Node cannot find the library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    public async Task<bool> ExistsOnServer(Guid uid)
    {
        var libFile = await Get(uid);
        if (libFile == null)
            return false;
        bool result = false;
        
        if (libFile.IsDirectory)
        {
            Logger.Instance.ILog("Checking Folder exists on server: " + libFile.Name);
            try
            {
                result = System.IO.Directory.Exists(libFile.Name);
            }
            catch (Exception) {  }
        }
        else
        {
            try
            {
                result = System.IO.File.Exists(libFile.Name);
            }
            catch (Exception)
            {
            }
        }

        Logger.Instance.ILog((libFile.IsDirectory ? "Directory" : "File") +
                             (result == false ? " does not exist" : " exists") +
                             " on server: " + libFile.Name);
        
        return result;
    }
    
    
    
    
    /// <summary>
    /// Checks if the node is enabled
    /// </summary>
    /// <param name="node">the processing node</param>
    /// <returns>true if enabled, otherwise false</returns>
    private async Task<bool> NodeEnabled(ProcessingNode node)
    {
#if(DEBUG)
        if (Globals.IsUnitTesting)
            return await Task.FromResult(true);
#endif
        var licensedNodes = LicenseHelper.GetLicensedProcessingNodes();
        var allNodes = await ServiceLoader.Load<NodeService>().GetAllAsync();
        var enabledNodes = allNodes.Where(x => x.Enabled).OrderBy(x => x.Name).Take(licensedNodes).ToArray();
        var enabledNodeUids = enabledNodes.Select(x => x.Uid).ToArray();
        return enabledNodeUids.Contains(node.Uid);
    }


    /// <summary>
    /// Gets the next library file queued for processing
    /// </summary>
    /// <param name="logger">the logger to use</param>
    /// <param name="node">The node doing the processing</param>
    /// <param name="workerUid">The UID of the worker on the node</param>
    /// <returns>If found, the next library file to process, otherwise null</returns>
    public async Task<LibraryFile?> GetNextLibraryFile(Plugin.ILogger logger, ProcessingNode node, Guid workerUid)
    {
        var nodeLibraries = node?.Libraries?.Select(x => x.Uid)?.ToList() ?? new List<Guid>();

        var outOfSchedule = TimeHelper.InSchedule(node.Schedule) == false;

        var allLibraries = (await ServiceLoader.Load<LibraryService>().GetAllAsync());
        if (allLibraries.Any(x => x.Uid == CommonVariables.ManualLibraryUid) == false)
        {
            allLibraries.Add(new()
            {
                Uid = CommonVariables.ManualLibraryUid,
                Name = CommonVariables.ManualLibrary,
                Enabled = true,
                Schedule = new string ('1', 672)
            });
        }

        bool processingOrderLicensed = LicenseHelper.IsLicensed(LicenseFlags.ProcessingOrder);
        var sysInfo = new LibraryFilterSystemInfo()
        {
            AllLibraries = allLibraries.ToDictionary(x => x.Uid, x => x),
            Executors = FlowRunnerService.Executors.Values.ToList(),
            LicensedForProcessingOrder = processingOrderLicensed
        };
        var executing = await FlowRunnerService.ExecutingLibraryFiles();
        var executingLibraries = await FlowRunnerService.ExecutingLibraryRunners();
        
        var canProcess = allLibraries.Where(x =>
        {
            if (processingOrderLicensed && x.MaxRunners > 0 && 
                executingLibraries.TryGetValue(x.Uid, out var currentRunners) && 
                currentRunners >= x.MaxRunners)
            {
                logger.ILog($"Library '{x.Name}' at maximum runners '{currentRunners}' out of '{x.MaxRunners}'");
                return false;
            }

            if (node.AllLibraries == ProcessingLibraries.All)
                return true;
            if (node.AllLibraries == ProcessingLibraries.Only)
                return nodeLibraries.Contains(x.Uid);
            return nodeLibraries.Contains(x.Uid) == false;
        }).Select(x => x.Uid).ToList();

        var manager = new LibraryFileManager();
        var waitingForReprocess = outOfSchedule
            ? null
            : (await manager.GetAll(new() { Status = FileStatus.ReprocessByFlow, SysInfo = sysInfo })).FirstOrDefault(
                x =>
                    x.ProcessOnNodeUid == node.Uid);

        if (waitingForReprocess != null)
            logger.ILog($"File waiting for reprocessing [{node.Name}]: " + waitingForReprocess.Name);

        var nextFile = waitingForReprocess ?? (await manager.GetAll(new()
            {
                Status = FileStatus.Unprocessed, Skip = 0, Rows = 1, AllowedLibraries = canProcess,
                MaxSizeMBs = node.MaxFileSizeMb, ExclusionUids = executing, ForcedOnly = outOfSchedule,
                ProcessingNodeUid = node.Uid,
                SysInfo = sysInfo
            }
        )).FirstOrDefault();

        var nodeService = ServiceLoader.Load<NodeService>();
        if (nextFile == null)
        {
            nodeService.UpdateStatus(node.Uid, ProcessingNodeStatus.Idle);
            return nextFile;
        }

        if (waitingForReprocess == null && await HigherPriorityWaiting(logger, node, nextFile, allLibraries))
        {
            logger.ILog("Higher priority node waiting to process file");
            return null; // a higher priority node should process this file
        }

        nextFile.Status = FileStatus.Processing;
        nextFile.WorkerUid = workerUid;
        nextFile.ProcessingStarted = DateTime.UtcNow;
        nextFile.NodeUid = node.Uid;
        nextFile.NodeName = node.Name;

#if(DEBUG)
        if (Globals.IsUnitTesting)
            return nextFile;
#endif

        await manager.StartProcessing(nextFile.Uid, node.Uid, node.Name, workerUid);
        nodeService.UpdateStatus(node.Uid, ProcessingNodeStatus.Processing);
        return nextFile;
    }



    /// <summary>
    /// Checks if another enabled processing node is enabled, in-schedule and not all runners are in use.
    /// If so, then will return false, so another higher priority node can processing a file
    /// </summary>
    /// <param name="logger">the logger to use</param>
    /// <param name="node">the node to check</param>
    /// <param name="file">the next file that should be processed</param>
    /// <param name="allLibraries">all the libraries in the system</param>
    /// <returns>true if another higher priority node should be used instead</returns>
    private async Task<bool> HigherPriorityWaiting(ILogger logger, ProcessingNode node, LibraryFile file, List<Library> allLibraries)
    {
        if (file.NodeUid == node.Uid)
            return false; // forced to process on this node
        
        var allNodes = (await ServiceLoader.Load<NodeService>().GetAllAsync()).Where(x => 
            x.Uid != node.Uid && x.Enabled);
        var allLibrariesUids = allLibraries.Select(x => x.Uid).ToList();
        var executors = FlowRunnerService.Executors.Values.GroupBy(x => x.NodeUid)
            .ToDictionary(x => x.Key, x => x.Count());

        int nodePriority = GetCalculatedNodePriority(node);

        var nodeService = ServiceLoader.Load<NodeService>();
        
        foreach (var other in allNodes)
        {
            if (nodesThatCannotRun.TryGetValue(other.Uid, out var cannotRunUntil) &&
                cannotRunUntil > DateTime.Now)
            {
                logger.ILog($"Other node '{other.Name}' cannot run until: {cannotRunUntil}");
                continue;
            }

            // first check if its in schedule
            if (TimeHelper.InSchedule(other.Schedule) == false)
                continue;

            // check if this node is maxed out
            if (executors.TryGetValue(other.Uid, out int value) && value >= other.FlowRunners)
            {
                nodeService.UpdateStatus(other.Uid, ProcessingNodeStatus.MaximumRunnersReached);
                continue; // it's maxed out
            }

            if (Version.TryParse(other.Version, out Version? otherVersion) == false || otherVersion == null ||
                otherVersion < Globals.MinimumNodeVersion)
            {
                nodeService.UpdateStatus(other.Uid, ProcessingNodeStatus.VersionMismatch);
                continue; // version mismatch
            }

            // check if other can process this library
            var nodeLibraries = other.Libraries?.Select(x => x.Uid)?.ToList() ?? new();
            List<Guid> allowedLibraries = other.AllLibraries switch
            {
                ProcessingLibraries.Only => nodeLibraries,
                ProcessingLibraries.AllExcept => allLibrariesUids.Where(x => nodeLibraries.Contains(x) == false).ToList(),
                _ => allLibrariesUids,
            };
            if (allowedLibraries.Contains(file.LibraryUid!.Value) == false)
            {
                logger.DLog($"Node '{other.Name}' cannot process the file due to library restrictions: {file.Name}");
                continue;
            }

            // check the last time this node was seen to make sure its not disconnected
            if (other.LastSeen < DateTime.UtcNow.AddMinutes(-10))
            {
                string lastSeen = DateTime.UtcNow.Subtract(other.LastSeen)+ " ago";
                try
                {
                    lastSeen = other.LastSeen.Humanize();
                }
                catch (Exception)
                {
                    // this can throw
                }

                logger.ILog("Other node is offline: " + other.Name + ", last seen: " + lastSeen);
                if(other.Uid != CommonVariables.InternalNodeUid)
                    nodeService.UpdateStatus(other.Uid, ProcessingNodeStatus.Offline);
                continue; // 10 minute cut off, give it some grace period
            }

            int otherPriority = GetCalculatedNodePriority(other);
            if (otherPriority < nodePriority)
                continue; // other node is lower priority, so does not block this node
                
            if (otherPriority == nodePriority)
            {
                // now check for load balance, the other node can process this, but so can this node.
                var otherRunners = executors.GetValueOrDefault(other.Uid, 0);
                var nodeRunners = executors.GetValueOrDefault(node.Uid, 0);
                if (nodeRunners <= otherRunners)
                {
                    // this node has less than or equal number of runners
                    continue;
                }
                logger.ILog($"Load balancing '{other.Name}' can process file and is processing less '{otherRunners}', skipping node: '{node.Name}': {file.Name}");
                nodeService.UpdateStatus(node.Uid, ProcessingNodeStatus.HigherPriorityNodeAvailable);
                return true;
            }
            
            // the "other" node is higher priority, it's not maxed out, it's in-schedule, so we don't want the "node"
            // processing this file
            logger.ILog($"Higher priority node '{other.Name}' can process file, skipping node: '{node.Name}': {file.Name}");
            nodeService.UpdateStatus(node.Uid, ProcessingNodeStatus.HigherPriorityNodeAvailable);
            
            return true;
        }
        // no other node is higher priority, this node can process this file
        return false;

        int GetCalculatedNodePriority(ProcessingNode node)
        {
            int priority = node.Priority;
            if (executors.TryGetValue(node.Uid, out var count))
                priority -= count;
            return priority;
        }
    }

    /// <summary>
    /// Unholds any held files
    /// </summary>
    /// <param name="uids">the UIDs of the files to unhold</param>
    /// <returns>a task to await</returns>
    public Task Unhold(Guid[] uids)
        => new LibraryFileManager().Unhold(uids);

    /// <summary>
    /// Updates all files with the new flow name if they used this flow
    /// </summary>
    /// <param name="uid">the UID of the flow</param>
    /// <param name="name">the new name of the flow</param>
    /// <returns>a task to await</returns>
    public Task UpdateFlowName(Guid uid, string name)
        => new LibraryFileManager().UpdateFlowName(uid, name);

    /// <summary>
    /// Deletes the files from the given libraries
    /// </summary>
    /// <param name="uids">the UIDs of the libraries</param>
    /// <returns>a task to await</returns>
    public async Task DeleteByLibrary(Guid[] uids)
    {
        if (uids?.Any() != true)
            return;
        await new LibraryFileManager().DeleteByLibrary(uids);
        await ClientServiceManager.Instance.UpdateFileStatus();
    }

    /// <summary>
    /// Reprocess all files based on library UIDs
    /// </summary>
    /// <param name="uids">an array of UID of the libraries to reprocess</param>
    /// <returns>true if any rows were updated, otherwise false</returns>
    public async Task ReprocessByLibraryUid(Guid[] uids)
    {
        await new LibraryFileManager().ReprocessByLibraryUid(uids);
        await ClientServiceManager.Instance.UpdateFileStatus();
    }

    /// <summary>
    /// Gets all the UIDs for library files in the system
    /// </summary>
    /// <returns>the UIDs of known library files</returns>
    public Task<List<Guid>> GetUids()
        => new LibraryFileManager().GetUids();

    /// <summary>
    /// Gets the processing time for each library file 
    /// </summary>
    /// <returns>the processing time for each library file</returns>
    public Task<List<LibraryFileProcessingTime>> GetLibraryProcessingTimes()
        => new LibraryFileManager().GetLibraryProcessingTimes();

    /// <summary>
    /// Clears the executed nodes, metadata, final size etc for a file
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    /// <param name="flowUid">the UID of the flow that will be executed</param>
    /// <param name="flowName">the name of the flow that will be executed</param>
    /// <returns>true if a row was updated, otherwise false</returns>
    public Task ResetFileInfoForProcessing(Guid uid, Guid? flowUid, string flowName)
        => new LibraryFileManager().ResetFileInfoForProcessing(uid, flowUid, flowName);

    /// <summary>
    /// Updates the original size of a file
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    /// <param name="size">the size of the file in bytes</param>
    /// <returns>true if a row was updated, otherwise false</returns>
    public Task<bool> UpdateOriginalSize(Guid uid, long size)
        => new LibraryFileManager().UpdateOriginalSize(uid, size);

    /// <summary>
    /// Resets any currently processing library files 
    /// This will happen if a server or node is reset
    /// </summary>
    /// <param name="nodeUid">[Optional] the UID of the node</param>
    /// <returns>true if successfully reset, otherwise false</returns>
    public async Task<bool> ResetProcessingStatus(Guid? nodeUid)
    {
        try
        {
            await new LibraryFileManager().ResetProcessingStatus(nodeUid);
            await ClientServiceManager.Instance?.UpdateFileStatus();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed to reset processing status: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return false;
        }
    }

    /// <summary>
    /// Gets the current status of a file
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    /// <returns>the current status of the file</returns>
    public Task<FileStatus?> GetFileStatus(Guid uid)
        => new LibraryFileManager().GetFileStatus(uid);

    // /// <summary>
    // /// Special case used by the flow runner to update a processing library file
    // /// </summary>
    // /// <param name="file">the processing library file</param>
    // public Task UpdateWork(LibraryFile file)
    //     => new LibraryFileManager().UpdateWork(file);

    /// <summary>
    /// Moves the passed in UIDs to the top of the processing order
    /// </summary>
    /// <param name="uids">the UIDs to move</param>
    public Task MoveToTop(params Guid[] uids)
        => new LibraryFileManager().MoveToTop(uids);

    /// <summary>
    /// Reset processing for the files
    /// </summary>
    /// <param name="uids">a list of UIDs to reprocess</param>
    public async Task Reprocess(params Guid[] uids)
    { 
        await new LibraryFileManager().SetStatus(FileStatus.Unprocessed, uids);
        await ClientServiceManager.Instance.UpdateFileStatus();
    }

    /// <summary>
    /// Reset processing for the files
    /// </summary>
    /// <param name="model">the reprocess model</param>
    /// <param name="onlySetProcessInfo">if only the process information should be set, ie these are unprocessed files</param>
    /// <returns>true if successful, otherwise a failure reason</returns>
    public async Task<Result<bool>> Reprocess(ReprocessModel model, bool onlySetProcessInfo)
    {
        if (model.Flow != null)
        {
            var flow = await new FlowService().GetByUidAsync(model.Flow.Uid);
            if (flow == null)
                return Result<bool>.Fail("Flow not found");
            model.Flow.Name = flow.Name;
        }
        if (model.Node != null)
        {
            var node = await new NodeService().GetByUidAsync(model.Node.Uid);
            if (node == null)
                return Result<bool>.Fail("Node not found");
            model.Node.Name = node.Name;
        }
        await new LibraryFileManager().Reprocess(model, onlySetProcessInfo);
        await ClientServiceManager.Instance.UpdateFileStatus();
        return true;
    }

    /// <summary>
    /// Toggles a flag on files
    /// </summary>
    /// <param name="uids">the UIDs of the files</param>
    /// <returns>true if any rows were updated, otherwise false</returns>
    public Task<bool> ToggleForce(params Guid[] uids)
        => new LibraryFileManager().ToggleFlag(LibraryFileFlags.ForceProcessing, uids);

    /// <summary>
    /// Force processing a set of files
    /// </summary>
    /// <param name="uids">the UIDs of the files</param>
    /// <returns>true if any rows were updated, otherwise false</returns>
    public Task<bool> ForceProcessing(Guid[] uids)
        => new LibraryFileManager().ForceProcessing(uids);

    /// <summary>
    /// Sets a status on a file
    /// </summary>
    /// <param name="status">The status to set</param>
    /// <param name="uids">the UIDs of the files</param>
    /// <returns>true if any rows were updated, otherwise false</returns>
    public Task<bool> SetStatus(FileStatus status, params Guid[] uids)
        => new LibraryFileManager().SetStatus(status, uids);

    /// <summary>
    /// Adds a files 
    /// </summary>
    /// <param name="files">the files being added</param>
    public Task Insert(params LibraryFile[] files)
        => new LibraryFileManager().Insert(files);

    /// <summary>
    /// Gets a library file if it is known
    /// </summary>
    /// <param name="path">the path of the library file</param>
    /// <param name="libraryUid">[Optional] the UID of the library the file is in, if not passed in then the first file with the name will be used</param>
    /// <returns>the library file if it is known</returns>
    public Task<LibraryFile?>GetFileIfKnown(string path, Guid? libraryUid)
        => new LibraryFileManager().GetFileIfKnown(path, libraryUid);

    /// <summary>
    /// Gets a library file if it is known by its fingerprint
    /// </summary>
    /// <param name="libraryUid">The UID of the library</param>
    /// <param name="fingerprint">the fingerprint of the library file</param>
    /// <returns>the library file if it is known</returns>
    public Task<LibraryFile?>  GetFileByFingerprint(Guid libraryUid, string fingerprint)
        => new LibraryFileManager().GetFileByFingerprint(libraryUid, fingerprint);

    /// <summary>
    /// Updates a moved file in the database
    /// </summary>
    /// <param name="file">the file to update</param>
    /// <returns>true if any files were updated</returns>
    public Task<bool> UpdateMovedFile(LibraryFile file)
        => new LibraryFileManager().UpdateMovedFile(file);

    /// <summary>
    /// Gets a list of all filenames and the file creation times
    /// </summary>
    /// <param name="libraryUid">the UID of the library</param>
    /// <returns>a dictionary of all files by their lowercase filename</returns>
    public async Task<Dictionary<string, KnownFileInfo>> GetKnownLibraryFilesWithCreationTimes(Guid libraryUid)
    {
        var data = await new LibraryFileManager().GetKnownLibraryFilesWithCreationTimes(libraryUid);
        return data.DistinctBy(x => x.Name.ToLowerInvariant()).ToDictionary(x => x.Name.ToLowerInvariant());
    }

    /// <summary>
    /// Updates all files with the new library name if they used this library
    /// </summary>
    /// <param name="uid">the UID of the library</param>
    /// <param name="name">the new name of the library</param>
    /// <returns>a task to await</returns>
    public Task UpdateLibraryName(Guid uid, string name)
        => new LibraryFileManager().UpdateLibraryName(uid, name);

    /// <summary>
    /// Gets the total storage saved
    /// </summary>
    /// <returns>the total storage saved</returns>
    public Task<long> GetTotalStorageSaved()
        => new LibraryFileManager().GetTotalStorageSaved();

    /// <summary>
    /// Performs a search for files
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>the matching files</returns>
    public Task<List<LibraryFile>> Search(LibraryFileSearchModel filter)
        => new LibraryFileManager().Search(filter);

    /// <summary>
    /// Gets if a file exists
    /// </summary>
    /// <param name="name">the name of the file</param>
    /// <returns>true if exists, otherwise false</returns>
    public Task<bool> FileExists(string name)
        => new LibraryFileManager().FileExists(name);

    /// <summary>
    /// Manually adds items for processing
    /// </summary>
    /// <param name="model">the model</param>
    /// <returns>the files that were added</returns>
    public async Task<Result<string[]>> ManuallyAdd(AddFileModel model)
    {
        Logger.Instance.ILog("Manually Adding: \n" + JsonSerializer.Serialize(model));
        
        if (model?.Files?.Any() != true)
            return Result<string[]>.Fail("No items");

        var flow = await ServiceLoader.Load<FlowService>().GetByUidAsync(model.FlowUid);
        if (flow == null)
            return Result<string[]>.Fail("Unknown flow");

        if (model.NodeUid != null && model.NodeUid != Guid.Empty)
        {
            var node = await ServiceLoader.Load<NodeService>().GetByUidAsync(model.NodeUid.Value);
            if (node == null)
                return Result<string[]>.Fail("Unknown node");
        }

        var newFiles = await Task.WhenAll(model.Files.Distinct().Select(async x =>
        {
            if (string.IsNullOrWhiteSpace(x))
                return null;

            if (await FileExists(x))
                return null; // it already exists

            var lf = new LibraryFile()
            {
                Status = FileStatus.Unprocessed,
                Name = x,
                LibraryUid = CommonVariables.ManualLibraryUid,
                FlowUid = flow.Uid,
                CustomVariables = model.CustomVariables,
                Flow = new()
                {
                    Name = flow.Name,
                    Type = flow.GetType().FullName,
                    Uid = flow.Uid
                },
                Library = new()
                {
                    Name = CommonVariables.ManualLibrary,
                    Uid = CommonVariables.ManualLibraryUid,
                    Type = typeof(Library).FullName
                },
                ProcessOnNodeUid = model.NodeUid == Guid.Empty ? null : model.NodeUid
            };
            if (Regex.IsMatch(x, "^http(s)?://", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
            {
                // get rest of url without domain,
                // get the path, replace query strings with / 
                // ie i want to fake a path for a url so http://mysite.com/a/b/c?ts=123 would be a/b/c/ts-123
            }

            try
            {
                var fileInfo = new FileInfo(x);
                if (fileInfo.Exists)
                {
                    lf.CreationTime = fileInfo.CreationTimeUtc;
                    lf.LastWriteTime = fileInfo.LastWriteTimeUtc;
                    lf.OriginalSize = fileInfo.Length;
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return lf;
        }));
        var files = newFiles.Where(x => x != null).Select(x => x!).ToArray();
        await Insert(files);
        return files.Select(x => x.Name).ToArray();
    }
}