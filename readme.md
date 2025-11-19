# Sylvan.Tools.FileSystemInfo

A .NET global tool that provides functionality similar to WinDirStat. Useful for finding where disk space is being consumed.

## Usage

### Install

```
dotnet tool install -g Sylvan.Tools.FileSystemInfo
```

### Running

```
sds [path]
```

## Performance

This tool was designed with performance in mind. 
On my machine™, scanning the `c:\users\` folder takes **minutes** using Windows Explorer file properties dialog. 
Similarly, WinDirStat report 2:04 to scan the folder.
The same scan can be done by this tool in ~5sec.

[!WARNING]
> This tool will run in WSL, but I found the performance to be terrible in that environment.
> I suspect this is due to some file system abstraction layer between WSL and Windows, but I don't know for sure.
> If anyone tries this in a *native* Linux environment, I'd be interested to hear how it performs there.