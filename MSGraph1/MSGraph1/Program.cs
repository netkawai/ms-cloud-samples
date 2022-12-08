using Microsoft.Graph;
using System.Text.Json;
using System.Diagnostics;

namespace MSGraph1
{
    internal class Program
    {
        static void Main(string[] args)
        {

            AuthenticationService AuthenticationService = new AuthenticationService();

            string fullPath = args[0];

            if(!System.IO.File.Exists(fullPath))
                throw new FileNotFoundException(fullPath);

            using (var fileStream = System.IO.File.OpenRead(fullPath))
            {
                // Create IBaseClient for MicrosoftGraph

                // Create session for Upload
                // Create the graph request builder for the drive
                IDriveRequestBuilder driveRequest = AuthenticationService.GraphClient.Me.Drive;
                string folderId = null;
                // If folder id is null, the request refers to the root folder
                IDriveItemRequestBuilder driveItemsRequest;
                if (folderId == null)
                {
                    driveItemsRequest = driveRequest.Root;
                }
                else
                {
                    driveItemsRequest = driveRequest.Items[folderId];
                }

                try
                {
                    var fileName = Path.GetFileName(fullPath);
                    // Create an upload session for a file with the same name of the user selected file
                    UploadSession session = driveItemsRequest
                         .ItemWithPath(fileName)
                         .CreateUploadSession()
                         .Request()
                         .PostAsync().Result;

                    // Add a new upload item at the beginning
                    var item = new OneDriveFileProgress(fileName);
                    //_progressItems.Insert(0, item);

                    // Start the upload process
                    // await item.UploadFileAsync(AuthenticationService, storageFile, session);

                    item.UploadFileAsync(AuthenticationService, fileStream, session).Wait();

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }

            }
            AuthenticationService.SignOutAsync().Wait();
        }
    }
}