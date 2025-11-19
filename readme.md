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