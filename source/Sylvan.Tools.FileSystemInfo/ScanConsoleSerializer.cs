sealed class ConsoleSerializer
{
    const string Directory = "\uD83D\uDCC1";
    const string File = "\uD83D\uDCC4";

    bool useColor;
    int maxDepth;
    string[] indents;
    Node[] nodes;

    const int PathWidth = 48;
    const int SizeWidth = 12;
    const int PctWidth = 12;
    const int FilesWidth = 12;
    const int DirsWidth = 12;

    static readonly Color Red = new Color(255, 0, 0);
    static readonly Color White = new Color(224, 224, 224);

    public ConsoleSerializer(int depth)
    {
        this.maxDepth = depth;
        this.indents = new string[depth];
        this.nodes = new Node[depth];
        this.useColor = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));
    }

    public void Write(TextWriter w, Node root)
    {

        w.WriteLine($"{"PATH",-PathWidth} {"SIZE",SizeWidth} {"PCT",PctWidth} {"FILES",FilesWidth:#,##0} {"DIRECTORIES",DirsWidth:#,##0}");
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

    const string ResetColor = "\e[0m";

    string GetColor(Color c)
    {
        return
            this.useColor
            ? $"\e[38;2;{c.R};{c.G};{c.B}m"
            : string.Empty;
    }

    string GetColor(long v1, long v2)
    {
        var f = v1 == 0 ? 1f : (float)v2 / (float)v1;
        var c = Color.Interpolate(Red, White, f);
        return GetColor(c);        
    }

    readonly struct Color
    {
        readonly byte r, g, b;

        public byte R => r;
        public byte G => g;
        public byte B => b;

        public Color(byte r, byte g, byte b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public static Color Interpolate(Color c1, Color c2, float f)
        {
            var fi = 1f - f;

            return
                new Color(
                    (byte)(c1.r * f + c2.r * fi),
                    (byte)(c1.g * f + c2.g * fi),
                    (byte)(c1.b * f + c2.b * fi)
                );
        }
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
                var node = e.Current;
                // why always write the first?
                WriteDirectory(w, node, depth);

                var limit = node.Size / 6;

                while (e.MoveNext())
                {
                    node = e.Current;
                    if (node.Size <= limit)
                    {
                        // accumulate the rest.
                        long s = node.Size;
                        int dc = node.DirectoryCount;
                        int fc = node.FileCount;

                        while (e.MoveNext())
                        {
                            node = e.Current;
                            s += node.Size;
                            fc += node.FileCount;
                            dc += node.DirectoryCount;
                        }
                        WriteRow(w, "...", fc, dc, s, depth);

                        return;
                    }
                    WriteDirectory(w, node, depth);
                }
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

            string sizeColorCmd = GetColor(nodes[0].Size, s);

            var pct = depth == 0 ? 1f : (float)s / nodes[depth - 1].Size;

            w.WriteLine($"{GetIndent(depth) + Directory + name,-PathWidth} {sizeColorCmd}{sizeStr,SizeWidth}{ResetColor} {pct,PctWidth:P} {fc,FilesWidth:#,##0} {dc,DirsWidth:#,##0}");
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