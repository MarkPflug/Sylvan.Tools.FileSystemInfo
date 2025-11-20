using System.Diagnostics;
using System.IO.Enumeration;
using System.Runtime.CompilerServices;

sealed class Node
{
    static readonly EnumerationOptions QuickOptions =
        new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
        };

    static readonly EnumerationOptions FlatOptions =
        new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = false,
            //AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.,
        };
    static readonly EnumerationOptions FlatFileOptions =
        new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = false,
            AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.Directory,
        };

    public static async Task<Node> BuildTree(string path, int depth)
    {
        var node = new Node();
        node.directoryCount = 1; // self
        node.name = path;

        if (depth == 0)
        {
            var a = QuickScan(path);
            node.directoryCount = a.directoryCount;
            node.fileCount = a.fileCount;
            node.size = a.size;
        }
        else
        {

            static long ScanFiles(ref FileSystemEntry e)
            {
                return e.IsDirectory
                    ? -1
                    : e.Length;
            }

            var items = new FileSystemEnumerable<long>(path, ScanFiles, FlatFileOptions);

            foreach (var item in items)
            {
                if (item >= 0)
                {
                    node.fileCount++;
                    node.immediateFileCount++;
                    node.size += item;
                }
            }

            Task<Node> ScanDirs(ref FileSystemEntry e)
            {
                if (e.IsDirectory)
                {
                    string dir = new string(e.Directory);
                    string name = new string(e.FileName);
                    var path = Path.Combine(dir, name);
                    // this task run is needed to get the recursive calls
                    // of the current thread.
                    return Task.Run<Node>(async () =>
                    {
                        return await BuildTree(path, depth - 1).ConfigureAwait(false);
                    });
                }
                else
                {
                    return null!;
                }
            }

            var tasks = new FileSystemEnumerable<Task<Node>>(path, ScanDirs, FlatOptions);

            node.directories = await Task.WhenAll(tasks.Where(t => t is not null).ToList()).ConfigureAwait(false);
            foreach (var d in node.directories)
            {
                node.fileCount += d.fileCount;
                node.size += d.size;
                node.directoryCount += d.directoryCount;
            }
        }
        return node;
    }

    class ScanResult
    {
        public int directoryCount = 0;
        public int fileCount = 0;
        public long size = 0;
    }

    static ScanResult QuickScan(string path)
    {
        var items = new FileSystemEnumerable<long>(path, Scan, QuickOptions);

        var r = new ScanResult();

        // pump the directory enumeration the delegate does all the work.
        foreach (var item in items)
        {
            if (item >= 0)
            {
                r.fileCount++;
                r.size += item;
            }
            else
            {
                r.directoryCount++;
            }
        }

        static long Scan(ref FileSystemEntry e)
        {
            return e.IsDirectory
                ? -1
                : e.Length;
        }

        return r;
    }

    public string Name => name;

    public IEnumerable<Node> Directories => directories;

    public Node()
    {
        this.name = string.Empty;
        //this.dir = string.Empty;
        this.directories = Array.Empty<Node>();
    }

    internal string name;
    //internal string dir;

    Node[] directories;

    int directoryCount;
    int fileCount;
    int immediateFileCount;
    long size;

    public long Size => size;
    public int DirectoryCount => directoryCount;
    public int FileCount => fileCount;
    public int ImmediateFileCount => immediateFileCount;

    public override string ToString()
    {
        return name;
    }
}