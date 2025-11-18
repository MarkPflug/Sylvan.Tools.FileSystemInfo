using System.Diagnostics;
using System.IO.Enumeration;
using System.Text;

public static class Program
{
    const int DefaultDepth = 3;
    const int MaxDepth = 6;

    public static async Task Main(string[] args)
    {
        var depth = DefaultDepth;
        Console.OutputEncoding = Encoding.UTF8;

        var dir = args.Length == 0 ? "." : args[0];
        var path = Path.GetFullPath(dir);
        var scan = new Scan(path, depth);
        Console.WriteLine("Scanning " + path);

        await scan.RunAsync();

        var serializer = new ConsoleSerializer(depth);
        serializer.Write(Console.Out, scan);
    }
}

class Scan
{
    public string root;
    int depth;
    public Node rootNode;

    public Scan(string root, int depth)
    {
        this.root = root;
        this.depth = depth;
    }

    public async Task RunAsync()
    {
        var sw = Stopwatch.StartNew();
        rootNode = await Node.BuildTree(root, depth);
        var s = sw.Elapsed;

        Console.WriteLine($"Scan completed in {s}");
    }
}

class Node
{
    static readonly EnumerationOptions QuickOptions =
    new EnumerationOptions
    {
        IgnoreInaccessible = true,
        RecurseSubdirectories = true,
        AttributesToSkip = FileAttributes.None | FileAttributes.ReparsePoint,
    };

    static readonly EnumerationOptions FlatOptions =
        new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = false,
            AttributesToSkip = FileAttributes.None | FileAttributes.ReparsePoint,
        };

    static readonly EnumerationOptions FlatFileOptions =
    new EnumerationOptions
    {
        IgnoreInaccessible = true,
        RecurseSubdirectories = false,
        AttributesToSkip = FileAttributes.None | FileAttributes.ReparsePoint | FileAttributes.Directory,
    };

    public static async Task<Node> BuildTree(string path, int depth)
    {
        var node = new Node();
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
            List<Task<Node>> tasks = new();
            foreach (var d in Directory.EnumerateDirectories(path, "*", FlatOptions))
            {
                Func<Task<Node>> tf = async () =>
                {
                    var child = await BuildTree(d, depth - 1);
                    node.directoryCount += 1 + child.directoryCount;
                    node.fileCount += child.fileCount;
                    node.size += child.size;
                    return child;
                };
                var t = Task.Run(tf);
                tasks.Add(t);
            }
            node.directories = await Task.WhenAll(tasks);

            var items = new FileSystemEnumerable<long>(path, ScanFiles, FlatFileOptions);

            foreach (var item in items)
            {
                if (item >= 0)
                {
                    node.fileCount++;
                    node.immediateFileCount ++;
                    node.size += item;
                }
            }

            static long ScanFiles(ref FileSystemEntry e)
            {
                return e.IsDirectory
                    ? -1
                    : e.Length;
            }
        }
        return node;
    }

    record ScanResult
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

    internal string name;
    Node[] directories;

    int directoryCount;
    int fileCount;
    int immediateFileCount;
    long size;

    public long Size => size;
    public int DirectoryCount => directoryCount;
    public int FileCount => fileCount;
    public int ImmediateFileCount => immediateFileCount;
}

