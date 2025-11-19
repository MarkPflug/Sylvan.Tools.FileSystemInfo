sealed class ConsoleSerializer
{
    const string Directory = "\uD83D\uDCC1";
    const string File = "\uD83D\uDCC4";

    int maxDepth;
    string[] indents;
    Node[] nodes;

    const int PathWidth = 48;
    const int SizeWidth = 12;
    const int FilesWidth = 12;
    const int DirsWidth = 12;

    public ConsoleSerializer(int depth)
    {
        this.maxDepth = depth;
        this.indents = new string[depth];
        this.nodes = new Node[depth];
    }

    public void Write(TextWriter w, Node root)
    {

        w.WriteLine($"{"PATH",-PathWidth} {"SIZE",SizeWidth} {"FILES",FilesWidth:#,##0} {"DIRECTORIES",DirsWidth:#,##0}");
        WriteDirectory(w, root, 0);
    }

    const long K = 1024;
    const long M = K * K;
    const long G = M * K;

    static string FormatFileSize(long fileSize)
    {
        if (fileSize > G)
            return (fileSize / (double)G).ToString("0.00' GB'");
        if (fileSize > M)
            return (fileSize / (double)M).ToString("0.00' MB'");
        if (fileSize > K)
            return (fileSize / (double)K).ToString("0.00' KB'");

        return fileSize.ToString("0'B'");
    }


    void WriteDirectory(TextWriter w, Node node, int depth)
    {
        if (depth >= this.maxDepth)
        {
            return;
        }

        nodes[depth] = node;

        const int IndentSize = 2;

        string GetIndent(int depth)
        {
            var d = Math.Min(depth, indents.Length - 1);
            return indents[d] ??= new string(' ', d * IndentSize);
        }

        void WriteDirectories(TextWriter w, IEnumerable<Node> dirs, int depth)
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
                WriteRow(w, "...", fc, dc, s, depth);
            }
        }

        void WriteRow(TextWriter w, string name, int fc, int dc, long s, int depth)
        {
            if (s == 0) return;

            const int MaxNameLen = PathWidth - 2; // 2 for space for the directory emoji

            string TruncateName(string name)
            {
                var limit = MaxNameLen - depth * IndentSize;
                if (limit < 4)
                {
                    // we don't have space to show anything meaningful
                    return "";
                }
                if (name.Length > limit)
                {
                    var h = (limit / 2) - 1;
                    return name[0..h] + ".." + name[^h..^0];
                }
                return name;
            }
            name = TruncateName(name);

            var sizeStr = FormatFileSize(s);
            w.WriteLine($"{GetIndent(depth) + Directory + name,-PathWidth} {sizeStr,SizeWidth} {fc,FilesWidth:#,##0} {dc,DirsWidth:#,##0}");
        }

        void Write(TextWriter w, Node node, int depth)
        {
            var name = depth == 0
                ? node.Name
                : Path.GetFileName(node.name);
            WriteRow(w, name, node.FileCount, node.DirectoryCount, node.Size, depth);
        }

        Write(w, node, depth);

        var dirs = node.Directories;
        if (dirs != null)
        {
            WriteDirectories(w, dirs, depth + 1);
        }
    }
}