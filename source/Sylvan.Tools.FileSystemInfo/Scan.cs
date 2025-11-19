sealed class Scan
{
    string root;
    int depth;

    public Scan(string root, int depth)
    {
        this.root = root;
        this.depth = depth;
    }

    public async Task<Node> RunAsync()
    {        
        return await Node.BuildTree(root, depth);        
    }
}
