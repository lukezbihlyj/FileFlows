using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.Plugin;
using FileFlows.Shared.Models;
using DatabaseType = FileFlows.Shared.Models.DatabaseType;
using ILogger = FileFlows.Common.ILogger;

namespace FileFlows.DataLayer.Upgrades;

/// <summary>
/// Upgrades for 24.05.1
/// </summary>
public class Upgrade_24_05_1
{
    /// <summary>
    /// Run the upgrade
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the database type</param>
    /// <param name="connectionString">the database connection string</param>
    /// <returns>the upgrade result</returns>
    public Result<bool> Run(ILogger logger, DatabaseType dbType, string connectionString)
    {
        var connector = DatabaseConnectorLoader.LoadConnector(logger, dbType, connectionString);
        using var db = connector.GetDb(true).Result;

        foreach (var dm in new DockerMod[] {dmFFmpeg(), dmRar()})
        {
            var dbo = FileFlowsObjectManager.ConvertToDbObject(dm);
            dbo.DateModified = DateTime.UtcNow;
            dbo.DateCreated = DateTime.UtcNow;
            dbo.Uid = Guid.NewGuid();
            db.Db.Insert(dbo);
        }

        return true;
    }
    
    private DockerMod dmFFmpeg()
        => new()
        {
            Name = "FFmpeg6",
            Description =
                "FFmpeg is a free and open-source software project consisting of a suite of libraries and programs for handling video, audio, and other multimedia files and streams. At its core is the command-line ffmpeg tool itself, designed for processing of video and audio files.",
            Author = "John Andrews",
            Revision = 1,
            Repository = true,
            Enabled = true,
            Icon =
            "data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz4NCjwhLS0gR2VuZXJhdG9yOiBBZG9iZSBJbGx1c3RyYXRvciAyNi4wLjEsIFNWRyBFeHBvcnQgUGx1Zy1JbiAuIFNWRyBWZXJzaW9uOiA2LjAwIEJ1aWxkIDApICAtLT4NCjxzdmcgdmVyc2lvbj0iMS4xIiBpZD0iTGF5ZXJfMSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIiB4bWxuczp4bGluaz0iaHR0cDovL3d3dy53My5vcmcvMTk5OS94bGluayIgeD0iMHB4IiB5PSIwcHgiDQoJIHZpZXdCb3g9IjAgMCAyMDAwIDIwMDAiIHN0eWxlPSJlbmFibGUtYmFja2dyb3VuZDpuZXcgMCAwIDIwMDAgMjAwMDsiIHhtbDpzcGFjZT0icHJlc2VydmUiPg0KPHN0eWxlIHR5cGU9InRleHQvY3NzIj4NCgkuc3Qwe2ZpbGw6bm9uZTtzdHJva2U6IzM3OEU0MztzdHJva2Utd2lkdGg6MzAwO3N0cm9rZS1saW5lY2FwOnJvdW5kO3N0cm9rZS1saW5lam9pbjpyb3VuZDtzdHJva2UtbWl0ZXJsaW1pdDo4O30NCjwvc3R5bGU+DQo8ZyB0cmFuc2Zvcm09InRyYW5zbGF0ZSg1LDUpIj4NCgk8cGF0aCBjbGFzcz0ic3QwIiBkPSJNMTY2LjcsMTY2LjdoNTUyLjJMMTY2LjcsNzE4Ljl2NTUyLjJMMTI3MS4xLDE2Ni43aDU1Mi4yTDE2Ni43LDE4MjMuM2g1NTIuMkwxODIzLjMsNzE4Ljl2NTUyLjJsLTU1Mi4yLDU1Mi4yDQoJCWg1NTIuMiIvPg0KPC9nPg0KPC9zdmc+DQo=",
            Code = @"#!/bin/bash

if command -v ffmpeg &>/dev/null; then
    echo ""FFmpeg is already installed.""
    exit
fi

architecture=$(uname -m)

echo ""Architecture: $architecture""

if [ ""$architecture"" == ""x86_64"" ]; then

  echo ""The architecture is AMD (x86_64).""

  apt-get update
  wget -O - https://repo.jellyfin.org/jellyfin_team.gpg.key | apt-key add -
  echo ""deb [arch=$( dpkg --print-architecture )] https://repo.jellyfin.org/$( awk -F'=' '/^ID=/{ print $NF }' /etc/os-release ) $( awk -F'=' '/^VERSION_CODENAME=/{ print $NF }' /etc/os-release ) main"" | tee /etc/apt/sources.list.d/jellyfin.list
  apt-get update
  apt-get install --no-install-recommends --no-install-suggests -y jellyfin-ffmpeg6 
  ln -s /usr/lib/jellyfin-ffmpeg/ffmpeg /usr/local/bin/ffmpeg
  ln -s /usr/lib/jellyfin-ffmpeg/ffprobe /usr/local/bin/ffprobe

elif [ ""$architecture"" == ""armv7l"" ] || [ ""$architecture"" == ""aarch64"" ] || [[ ""$architecture"" =~ [Aa][Rr][Mm] ]]; then

  echo ""The architecture is ARM.""

  apt-get update
  wget -O - https://repo.jellyfin.org/jellyfin_team.gpg.key | apt-key add -
  echo ""deb [arch=$( dpkg --print-architecture )] https://repo.jellyfin.org/$( awk -F'=' '/^ID=/{ print $NF }' /etc/os-release ) $( awk -F'=' '/^VERSION_CODENAME=/{ print $NF }' /etc/os-release ) main"" | tee /etc/apt/sources.list.d/jellyfin.list
  apt-get update
  apt-get install --no-install-recommends --no-install-suggests -y jellyfin-ffmpeg6
  ln -s /usr/lib/jellyfin-ffmpeg/ffmpeg /usr/local/bin/ffmpeg
  ln -s /usr/lib/jellyfin-ffmpeg/ffprobe /usr/local/bin/ffprobe

else

  echo ""The architecture is not recognized as AMD or ARM: $architecture.""

fi"
        };
    
    
    private DockerMod dmRar()
        => new()
        {
            Name = "rar",
            Description = "RAR is an archive file format used for data compression, while UNRAR is a utility to extract content from RAR archives.",
            Author = "John Andrews",
            Revision = 1,
            Repository = true,
            Enabled = true,
            Icon = "data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0iaXNvLTg4NTktMSI/Pg0KPCEtLSBVcGxvYWRlZCB0bzogU1ZHIFJlcG8sIHd3dy5zdmdyZXBvLmNvbSwgR2VuZXJhdG9yOiBTVkcgUmVwbyBNaXhlciBUb29scyAtLT4NCjxzdmcgdmVyc2lvbj0iMS4xIiBpZD0iTGF5ZXJfMSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIiB4bWxuczp4bGluaz0iaHR0cDovL3d3dy53My5vcmcvMTk5OS94bGluayIgDQoJIHZpZXdCb3g9IjAgMCA1MTIgNTEyIiB4bWw6c3BhY2U9InByZXNlcnZlIj4NCjxwYXRoIHN0eWxlPSJmaWxsOiNCQzVFQjA7IiBkPSJNNTExLjM0NCwyNzQuMjY2QzUxMS43NywyNjguMjMxLDUxMiwyNjIuMTQzLDUxMiwyNTZDNTEyLDExNC42MTUsMzk3LjM4NSwwLDI1NiwwUzAsMTE0LjYxNSwwLDI1Ng0KCWMwLDExNy43NjksNzkuNTMsMjE2Ljk0OSwxODcuODA5LDI0Ni44MDFMNTExLjM0NCwyNzQuMjY2eiIvPg0KPHBhdGggc3R5bGU9ImZpbGw6I0FBMzM5OTsiIGQ9Ik01MTEuMzQ0LDI3NC4yNjZMMzE0Ljk5MSw3Ny45MTNMMTE5LjA5Niw0MzQuMDg3bDY4LjcxNCw2OC43MTRDMjA5LjUyMiw1MDguNzg3LDIzMi4zODUsNTEyLDI1Niw1MTINCglDMzkxLjI0Myw1MTIsNTAxLjk3Niw0MDcuMTI1LDUxMS4zNDQsMjc0LjI2NnoiLz4NCjxwb2x5Z29uIHN0eWxlPSJmaWxsOiNGRkZGRkY7IiBwb2ludHM9IjI3OC4zMjgsMzMzLjkxMyAyNTUuNzExLDc3LjkxMyAxMTkuMDk2LDc3LjkxMyAxMTkuMDk2LDMxMS42NTIgIi8+DQo8cG9seWdvbiBzdHlsZT0iZmlsbDojRThFNkU2OyIgcG9pbnRzPSIzOTIuOTA0LDMxMS42NTIgMzkyLjkwNCwxNTUuODI2IDMzNy4yNTIsMTMzLjU2NSAzMTQuOTkxLDc3LjkxMyAyNTUuNzExLDc3LjkxMyANCgkyNTYuMDY3LDMzMy45MTMgIi8+DQo8cG9seWdvbiBzdHlsZT0iZmlsbDojRkZGRkZGOyIgcG9pbnRzPSIzMTQuOTkxLDE1NS44MjYgMzE0Ljk5MSw3Ny45MTMgMzkyLjkwNCwxNTUuODI2ICIvPg0KPHJlY3QgeD0iMTE5LjA5NiIgeT0iMzExLjY1MiIgc3R5bGU9ImZpbGw6IzYxMDM1MzsiIHdpZHRoPSIyNzMuODA5IiBoZWlnaHQ9IjEyMi40MzUiLz4NCjxnPg0KCTxwYXRoIHN0eWxlPSJmaWxsOiNGRkZGRkY7IiBkPSJNMTk5LjUzNSwzODQuNDUzaC0wLjM3OEgxODguOTR2MTQuOTA5aC0xMy40NzF2LTUyLjk3NWgyMy42ODdjMTQuMDAxLDAsMjIuMDIzLDYuNjU5LDIyLjAyMywxOC40NjUNCgkJYzAsOC4wOTctMy40MDYsMTMuOTI1LTkuNjExLDE3LjAyN2wxMS4xMjUsMTcuNDgzaC0xNS4yODdMMTk5LjUzNSwzODQuNDUzeiBNMTk5LjE1NywzNzMuODU4YzUuODI4LDAsOS4yMzMtMi45NTIsOS4yMzMtOC41NTINCgkJYzAtNS41MjUtMy40MDUtOC4zMjQtOS4yMzMtOC4zMjRIMTg4Ljk0djE2Ljg3N2gxMC4yMTdWMzczLjg1OHoiLz4NCgk8cGF0aCBzdHlsZT0iZmlsbDojRkZGRkZGOyIgZD0iTTI0NC4xOTIsMzg5LjZsLTMuODU5LDkuNzYzaC0xMy44NTFsMjIuODU1LTUyLjk3NWgxMy44NDhsMjIuMzI3LDUyLjk3NWgtMTQuMzc5bC0zLjc4My05Ljc2Mw0KCQlIMjQ0LjE5MnogTTI1NS44NDYsMzU5Ljc4MWwtNy43MiwxOS42MDFoMTUuMjg4TDI1NS44NDYsMzU5Ljc4MXoiLz4NCgk8cGF0aCBzdHlsZT0iZmlsbDojRkZGRkZGOyIgZD0iTTMxNi4zOTYsMzg0LjQ1M2gtMC4zNzhIMzA1Ljh2MTQuOTA5aC0xMy40N3YtNTIuOTc1aDIzLjY4OGMxNCwwLDIyLjAyMiw2LjY1OSwyMi4wMjIsMTguNDY1DQoJCWMwLDguMDk3LTMuNDA2LDEzLjkyNS05LjYxMSwxNy4wMjdsMTEuMTI1LDE3LjQ4M2gtMTUuMjg4TDMxNi4zOTYsMzg0LjQ1M3ogTTMxNi4wMTgsMzczLjg1OGM1LjgyNiwwLDkuMjMzLTIuOTUyLDkuMjMzLTguNTUyDQoJCWMwLTUuNTI1LTMuNDA3LTguMzI0LTkuMjMzLTguMzI0SDMwNS44djE2Ljg3N2gxMC4yMThWMzczLjg1OHoiLz4NCjwvZz4NCjwvc3ZnPg==",
            Code = @"#!/bin/bash

# Check if rar is installed
if ! command -v unrar &>/dev/null; then
    echo ""rar is not installed. Installing...""

    # Update package lists
    apt update

    # Install MKVToolNix
    apt install -y rar unrar

    echo ""Installation complete.""
else
    echo ""rar is already installed.""
fi"
        };
}