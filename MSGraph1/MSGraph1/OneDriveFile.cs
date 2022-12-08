using Microsoft.Graph;
using System;
using System.Linq;

namespace MSGraph1
{
    /// <summary>
    /// Represents a OneDrive file
    /// </summary>
    public class OneDriveFile : OneDriveItem
    {
        /// <summary>
        /// Gets the thumbnail, if any, for the file, otherwhile returns null
        /// </summary>
        public Image Thumbnail
        {
            get
            {
                var mediumThunbnail = DriveItem.Thumbnails.FirstOrDefault(t => t.Medium != null);
                if (mediumThunbnail != null)
                {
                    //return new BitmapImage(new Uri(mediumThunbnail.Medium.Url));
                    return null; // I am not sure really neutral or cross platform
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the HTTP uri to use to download the file's content
        /// </summary>
        public Uri DownloadUri => new Uri(DriveItem.AdditionalData["@microsoft.graph.downloadUrl"].ToString());

        public OneDriveFile(DriveItem item) : base(item)
        {

        }
    }
}
