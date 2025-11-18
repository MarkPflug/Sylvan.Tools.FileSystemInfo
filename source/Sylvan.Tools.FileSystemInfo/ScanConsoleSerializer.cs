class ConsoleSerializer 
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

        void WriteIndented(TextWriter w, string s, int depth)
        {
            w.Write(GetIndent(depth));
            w.WriteLine(s);
        }
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
            w.WriteLine($"{GetIndent(depth) + Directory + name,-32} {sizeStr,24} {fc,24:#,##0} {dc,24:#,##0}");
        }

        void Write(TextWriter w, Node node, int depth)
        {
            var name = Path.GetFileName(node.name);
            WriteRow(w, name, node.FileCount, node.DirectoryCount, node.Size, depth);
        }

        Write(w, node, depth);

        if (node.Directories == null)
        {
            // do I want to do anything here?
        }
        else
        {
            WriteDirectories(w, node.Directories, depth + 1);
        }
    }
}