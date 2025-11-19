using System.Diagnostics;
using System.Text;

public static class Program
{
    const int DefaultDepth = 3;
    const int MaxDepth = 6;

    public static async Task<int> Main(string[] args)
    {
        var depth = DefaultDepth;
        Console.OutputEncoding = Encoding.UTF8;

        var dir = args.Length == 0 ? "." : args[0];
        var path = Path.GetFullPath(dir);

        if (path.EndsWith(Path.DirectorySeparatorChar))
        {
            //path = path[..^1];
        }
        
        Console.WriteLine("Scanning " + path);
        try
        {
            var sw = Stopwatch.StartNew();
            var root = await Node.BuildTree(path, depth);
            sw.Stop();
            Console.WriteLine($"Scan complete in {sw.Elapsed}");
            var serializer = new ConsoleSerializer(depth);
            serializer.Write(Console.Out, root);
        }
        catch (IOException)
        {
            Console.Error.WriteLine("The directory doesn't exist");
            return -1;
        }

        return 0;
    }
}
