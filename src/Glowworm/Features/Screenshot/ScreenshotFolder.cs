namespace Glowworm.Features.Screenshot;

public class ScreenshotFolder
{

    public string Folder { get; set; }

    public bool Default { get; set; }

    public bool InGame { get; set; }

    public bool IsCloudGame { get; set; }

    public bool CanRemove => !(InGame || Default || IsCloudGame);


    public ScreenshotFolder(string folder)
    {
        Folder = folder;
    }

}


