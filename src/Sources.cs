using System;
namespace HearthstoneAccessPatcher;
public struct Source
{
    public readonly string name;
    public readonly string url;
    public Source(string name, string url)
    {
        this.name = name;
        this.url = url;
    }
}

public static class SourceManager
{
    static public Source[] Sources = new Source[]{
        new Source("Default.", "https://hearthstoneaccess.com/files/pre_patch.zip"),
        new Source("Underground Arena Support (Experimental).", "https://hearthstoneaccess.com/api/v1/release-channels/underground/download-latest"),
    };
}
