using Microsoft.Graph;

namespace MSGraph1
{

    /// <summary>
    /// Represents a OneDrive folder
    /// </summary>
    public class OneDriveFolder : OneDriveItem
    {
        public OneDriveFolder(DriveItem item) : base(item)
        {
        }
    }
}
