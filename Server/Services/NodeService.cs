﻿using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.Server.Controllers;
using FileFlows.Server.Hubs;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// An Service for communicating with the server for all Processing Node related actions
/// </summary>
public class NodeService //: INodeService
{
    private ConcurrentDictionary<Guid, ProcessingNodeStatus> _currentStatus = new();
    
    /// <summary>
    /// Initializes the node service
    /// </summary>
    static NodeService()
    {
        if (Globals.IsUnitTesting)
            return;

        var manager = new NodeManager();
        var internalNode = manager.GetByUid(CommonVariables.InternalNodeUid).Result;
        if (internalNode != null)
        {
            bool update = false;
            if (internalNode.Version != Globals.Version)
            {
                internalNode.Version = Globals.Version;
                update = true;
            }

            if (internalNode.OperatingSystem == OperatingSystemType.Unknown)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    internalNode.OperatingSystem = OperatingSystemType.Windows;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    internalNode.OperatingSystem = OperatingSystemType.Mac;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    internalNode.OperatingSystem = OperatingSystemType.Linux;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                    internalNode.OperatingSystem = OperatingSystemType.Linux;

                if (internalNode.OperatingSystem != OperatingSystemType.Unknown)
                    update = true;
            }

            if (update)
                manager.Update(internalNode, auditDetails: AuditDetails.ForServer()).Wait();
        }
    }

    /// <inheritdoc />
    public Task<ProcessingNode> Register(string serverUrl, string address, string tempPath, List<RegisterModelMapping> mappings)
    {
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// Updates the status of a processing node
    /// </summary>
    /// <param name="uid">The UID of the node</param>
    /// <param name="status">The current status</param>
    public void UpdateStatus(Guid uid, ProcessingNodeStatus? status)
    {
        if (status == null)
            _currentStatus.TryRemove(uid, out _);
        else
            _currentStatus.AddOrUpdate(uid, status.Value, (key, existingVal) => status.Value);
        _ = ClientServiceManager.Instance.UpdateNodeStatusSummaries();
    }

    /// <summary>
    /// Gets the current status of a processing node
    /// </summary>
    /// <param name="uid">The UID of the node</param>
    /// <returns>the current status</returns>
    public ProcessingNodeStatus GetStatus(Guid uid)
        => _currentStatus.GetValueOrDefault(uid, new ProcessingNodeStatus());

    /// <summary>
    /// Gets an instance of the internal processing node
    /// </summary>
    /// <returns>an instance of the internal processing node</returns>
    public async Task<ProcessingNode?> GetServerNodeAsync()
    {
        var manager = new NodeManager();
        var node = await manager.GetByUid(CommonVariables.InternalNodeUid);
        if (node != null)
            return node;
        
        Logger.Instance.ILog("Adding Internal Processing Node");
        bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);                
        var result = await manager.Update(new ProcessingNode
        {
            Uid = CommonVariables.InternalNodeUid,
            Name = CommonVariables.InternalNodeName,
            Address = CommonVariables.InternalNodeName,
            Schedule = new string('1', 672),
            Enabled = true,
            FlowRunners = 1,
            Version = Globals.Version.ToString(),
            AllLibraries = ProcessingLibraries.All,
#if (DEBUG)
            TempPath = windows ? @"d:\videos\temp" : Path.Combine(DirectoryHelper.BaseDirectory, "Temp"),
#else
            TempPath = Globals.IsDocker ? "/temp" : Path.Combine(DirectoryHelper.BaseDirectory, "Temp"),
#endif
        }, auditDetails: AuditDetails.ForServer());
        if (result.Failed(out var error))
        {
            Logger.Instance.ELog("Failed creating internal node: " + error);
            return null;
        }
        node = result.Value;
        node.SignalrUrl = "flow";
        return node;
    } 

    /// <summary>
    /// Gets a tool path by name
    /// </summary>
    /// <param name="name">The name of the tool</param>
    /// <returns>a tool path</returns>
    public async Task<string> GetVariableAsync(string name)
    {
        var result = await new VariableController().GetByName(name);
        return result?.Value ?? string.Empty;
    }

    /// <inheritdoc />
    public Task<List<ProcessingNode>> GetAllAsync()
        => new NodeManager().GetAll();

    /// <inheritdoc />
    public Task<ProcessingNode?> GetByUidAsync(Guid uid)
        => new NodeManager().GetByUid(uid);

    /// <summary>
    /// Gets a processing node by its physical address
    /// </summary>
    /// <param name="address">The address (hostname or IP address) of the node</param>
    /// <returns>An instance of the processing node</returns>
    public async Task<ProcessingNode> GetByAddressAsync(string address)
    {
        if (address == "INTERNAL_NODE")
            return await GetServerNodeAsync();
        address = address.Trim().ToLowerInvariant();
        var manager = new NodeManager();
        return await manager.GetByAddress(address);
    }


    /// <summary>
    /// Updates the last seen date for a node
    /// </summary>
    /// <param name="nodeUid">the UID of the node being updated</param>
    /// <returns>a task to await</returns>
    public Task UpdateLastSeen(Guid nodeUid)
        => new NodeManager().UpdateLastSeen(nodeUid);

    /// <summary>
    /// Updates the node version
    /// </summary>
    /// <param name="nodeUid">the UID of the node being updated</param>
    /// <param name="nodeVersion">the new version number</param>
    /// <returns>a task to await</returns>
    public Task UpdateVersion(Guid nodeUid, string nodeVersion)
        => new NodeManager().UpdateVersion(nodeUid, nodeVersion);

    /// <summary>
    /// Updates a processing node
    /// </summary>
    /// <param name="node">the node to update</param>
    /// <param name="auditDetails">The audit details</param>
    /// <returns>the update result</returns>
    public Task<Result<ProcessingNode>> Update(ProcessingNode node, AuditDetails? auditDetails)
        => new NodeManager().Update(node, auditDetails);

    /// <summary>
    /// Deletes the given nodes
    /// </summary>
    /// <param name="uids">the UID of the nodes to delete</param>
    /// <param name="auditDetails">the audit details</param>
    /// <returns>a task to await</returns>
    public Task Delete(Guid[] uids, AuditDetails auditDetails)
        => new NodeManager().Delete(uids, auditDetails);

    /// <summary>
    /// Gets the total files each node has processed
    /// </summary>
    /// <returns>A dictionary of the total files indexed by the node UID</returns>
    public Task<Dictionary<Guid, int>> GetTotalFiles()
        => new NodeManager().GetTotalFiles();

    public ProcessingNodeStatus GetStatus(ProcessingNode node)
    {
        if (node.Enabled == false)
            return ProcessingNodeStatus.Disabled;
        if (TimeHelper.InSchedule(node.Schedule) == false)
            return ProcessingNodeStatus.OutOfSchedule;
        if (node.Version != Globals.Version && node.Uid != CommonVariables.InternalNodeUid)
            return ProcessingNodeStatus.VersionMismatch;
        if (node.LastSeen < DateTime.UtcNow.AddMinutes(-5) && node.Uid != CommonVariables.InternalNodeUid)
            return ProcessingNodeStatus.Offline;
        if (FlowRunnerService.Executors.Any(x => x.Value.NodeUid == node.Uid))
            return ProcessingNodeStatus.Processing;

        var status = GetStatus(node.Uid);
        if(status == ProcessingNodeStatus.Offline && node.Uid == CommonVariables.InternalNodeUid)
            return ProcessingNodeStatus.Idle;
        return status;
    }

    /// <summary>
    /// Gets the status summaries of all nodes
    /// </summary>
    /// <returns>the status summaries</returns>
    public async Task<List<NodeStatusSummary>> GetStatusSummaries()
    {
        var nodes = await GetAllAsync();
        var server = ServiceLoader.Load<HardwareInfoService>().GetHardwareInfo();
        return nodes.Select(x => new NodeStatusSummary()
        {
            OperatingSystem = x.OperatingSystem,
            Architecture = x.Architecture,
            Version = x.Version,
            Enabled = x.Enabled,
            Uid = x.Uid,
            Address = x.Address,
            FlowRunners = x.FlowRunners,
            Name = x.Name,
            OutOfSchedule = TimeHelper.InSchedule(x.Schedule) == false,
            ScheduleResumesAtUtc = TimeHelper.UtcDateUntilInSchedule(x.Schedule),
            Priority = x.Priority,
            Status = GetStatus(x),
            HardwareInfo = x.Uid == CommonVariables.InternalNodeUid ? server : x.HardwareInfo
        }).ToList();
    }
}