using System.Collections;
using System.Diagnostics;
using System.IO.Enumeration;
using System.Text;
using System.Xml;

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

interface ISerializer<T>
{
    void Write(Stream stream, T obj);
}

interface ITextSerializer<T>
{
    void Write(TextWriter stream, T obj);
}

class ConsoleSerializer : ITextSerializer<Scan>
{

    const string Directory = "\uD83D\uDCC1";
    const string File = "\uD83D\uDCC4";

    int maxDepth;
    string[] indents;
    Node[] nodes;

    public ConsoleSerializer(int depth)
    {
        this.maxDepth = depth;
        this.indents = new string[depth];
        this.nodes = new Node[depth];
    }

    public void Write(TextWriter w, Scan result)
    {
        w.WriteLine(result.root);
        WriteDirectory(w, result.rootNode, 0);
    }

    const long K = 1024;
    const long M = K * K;
    const long G = M * K;

    static string FormatFileSize(long fileSize)
    {
        if (fileSize > G)
            return (fileSize / (double)G).ToString("0.00' GB'");
        if (fileSize > M)
            return (fileSize / (double)G).ToString("0.00' MB'");
        if (fileSize > K)
            return (fileSize / (double)G).ToString("0.00' KB'");

        return fileSize.ToString("0'B'");
    }


    void WriteDirectory(TextWriter w, Node node, int depth)
    {
        if (depth >= this.maxDepth)
        {
            return;
        }

        nodes[depth] = node;
        // TODO: are these inline/nested functions causing closure allocations?
        void Indent()
        {
            w.Write(GetIndent());
        }

        void WriteIndented(string s)
        {
            Indent();
            w.WriteLine(s);
        }
        const int IndentSize = 3;

        string GetIndent()
        {
            var d = Math.Min(depth, indents.Length - 1);
            return indents[d] ??= new string(' ', d * IndentSize);
        }

        void WriteDirectories(IEnumerable<Node> dirs, int depth)
        {
            if (depth >= this.maxDepth) return;

            var e = dirs.OrderByDescending(d => d.Size).GetEnumerator();
            if (e.MoveNext())
            {
                var first = e.Current;
                WriteDirectory(w, first, depth);

                var limit = first.Size / 8;
                int min = 0;

                foreach (var node in e.OneShot().TakeWhile(n => min-- > 0 || n.Size >= limit))
                {
                    WriteDirectory(w, node, depth);
                }

                long s = 0;
                int dc = 0;
                int fc = 0;
                foreach (var x in e.OneShot())
                {
                    s += x.Size;
                    fc += x.FileCount;
                    dc += x.DirectoryCount;
                }
                WriteRow("...", fc, dc, s);
            }
        }

        void WriteRow(string name, int fc, int dc, long s)
        {
            if (s == 0) return;

            const int MaxNameLen = 32;

            string TruncateName(string name)
            {
                var limit = MaxNameLen - depth * IndentSize;
                if (name.Length > limit)
                {
                    var h = limit / 2;
                    return name[0..(h - 2)] + ".." + name[^h..^1];
                }
                return name;
            }
            name = TruncateName(name);

            var sizeStr = FormatFileSize(s);
            w.WriteLine($"{GetIndent() + Directory + name,-32} {sizeStr,24} {fc,24:#,##0} {dc,24:#,##0}");
        }

        void Write(Node node)
        {
            var name = Path.GetFileName(node.name);
            WriteRow(name, node.FileCount, node.DirectoryCount, node.Size);
        }

        Write(node);

        if (node.Directories == null)
        {
            // do I want to do anything here?
        }
        else
        {
            WriteDirectories(node.Directories, depth + 1);
        }
    }
}

static class Ex
{
    class OneShotEnumerable<T> : IEnumerable<T>
    {
        IEnumerator<T> e;

        public OneShotEnumerable(IEnumerator<T> e)
        {
            this.e = e;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return e;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static IEnumerable<T> OneShot<T>(this IEnumerator<T> e)
    {
        return new OneShotEnumerable<T>(e);
    }
}

class ScanXmlSerializer : ITextSerializer<Scan>
{
    public void Write(TextWriter w, Scan result)
    {
        var s = new XmlWriterSettings() { NewLineChars = "\n", Indent = true, IndentChars = " ", NewLineOnAttributes = true };
        using var x = XmlWriter.Create(w, s);
        var r = result.rootNode;
        x.WriteStartElement("root");
        x.WriteAttributeString("path", result.rootNode.name);
        WriteDirectory(x, r);
    }

    void WriteDirectory(XmlWriter w, Node node)
    {
        w.WriteStartElement("directory");
        w.WriteAttributeString("name", Path.GetFileName(node.name));
        w.WriteStartAttribute("size");
        w.WriteValue(node.Size);
        w.WriteEndAttribute();

        if (node.Directories == null)
        {
            // do I want to do anything here?
        }
        else
        {
            foreach (var c in node.Directories.OrderByDescending(d => d.Size))
            {
                WriteDirectory(w, c);
            }
        }
        w.WriteEndElement();
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

    double MB = 1024d * 1024d;

    public async Task RunAsync()
    {
        var sw = Stopwatch.StartNew();
        rootNode = await Node.BuildTree(root, depth);

        var s = sw.Elapsed;
        var c = rootNode.FileCount;
        var mb = rootNode.Size / MB;

        Console.WriteLine($"{c:#,##0} files with total {mb:#,##0.00}mb in {s}");


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
            foreach (var f in Directory.EnumerateFiles(path, "*", FlatOptions))
            {
                node.size += new FileInfo(f).Length;
                node.immediateFileCount++;
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

