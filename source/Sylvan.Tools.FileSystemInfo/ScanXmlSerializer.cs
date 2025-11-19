using System.Xml;

sealed class ScanXmlSerializer 
{
    public void Write(TextWriter w, Node root)
    {
        var s = new XmlWriterSettings() { NewLineChars = "\n", Indent = true, IndentChars = " ", NewLineOnAttributes = true };
        using var x = XmlWriter.Create(w, s);
        x.WriteStartElement("root");
        x.WriteAttributeString("path", root.Name);
        WriteDirectory(x, root);
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
