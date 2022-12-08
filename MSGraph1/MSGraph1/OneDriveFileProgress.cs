using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace MSGraph1
{
    /// <summary>
    /// Represents a upload or download process
    /// </summary>
    public class OneDriveFileProgress : INotifyPropertyChanged, IProgress<long>
    {
        private double _maximum;
        private double _current;
        private string _text;
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Creates a progress for a file to download
        /// </summary>
        /// <param name="file"></param>
        public OneDriveFileProgress(OneDriveFile file)
        {
            Path = file.Path;
        }

        /// <summary>
        /// Creates a progress for a file to upload
        /// </summary>
        /// <param name="name"></param>
        public OneDriveFileProgress(string name)
        {
            Path = $"New file: {name}";
        }

        /// <summary>
        /// Gets the file's full path
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the maximum value to use as a progress for the current operation
        /// </summary>
        public double Maximum { get => _maximum; private set => Set(ref _maximum, value); }

        /// <summary>
        /// Gets the current value to use as a progress for the current operation
        /// </summary>
        public double Current { get => _current; private set => Set(ref _current, value); }

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Text { get => _text; private set => Set(ref _text, value); }

        /// <summary>
        /// Event raised when any property has changed
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Starts to upload a file using the session provided
        /// </summary>
        /// <param name="authenticationService">The service used to keep the graph session</param>
        /// <param name="sourceFile">The file to upload</param>
        /// <param name="uploadSession">The upload session</param>
        /// <returns></returns>
        public async Task<OneDriveFile> UploadFileAsync(AuthenticationService service, FileStream fileStream, UploadSession uploadSession)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            // Keep a local copy of the token because the source can change while executing this function
            var token = _cancellationTokenSource.Token;

            Text = "Starting...";
            token.ThrowIfCancellationRequested();

            int maxChunkSize = 320 * 1024; // 320 KB - Change this to your chunk size. 5MB is the default
            ChunkedUploadProvider provider = new ChunkedUploadProvider(uploadSession, service.GraphClient, fileStream, maxChunkSize);

            // Set up the chunk request necessities
            IEnumerable<UploadChunkRequest> chunkRequests = provider.GetUploadChunkRequests();

            // Buffer to use for each chunk
            byte[] readBuffer = new byte[maxChunkSize];
            List<Exception> trackedExceptions = new List<Exception>();

            // Set initial progress values
            Maximum = fileStream.Length;
            Current = 0;

            // Upload the chunks
            foreach (var request in chunkRequests)
            {
                // Send chunk request
                UploadChunkResult result = await provider.GetChunkRequestResponseAsync(request, readBuffer, trackedExceptions);
                token.ThrowIfCancellationRequested();

                // Update the progress values
                ((IProgress<long>)this).Report(fileStream.Position);

                // If upload is completed
                if (result.UploadSucceeded)
                {
                    // Update the progress values
                    Current = Maximum;
                    Text = "Upload completed";
                    // Return the uploaded file
                    return new OneDriveFile(result.ItemResponse);
                }
            }

            return null;
        }

        /// <summary>
        /// Cancels any operation in progress
        /// </summary>
        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        private async Task CopyToAsyncWithProgress(Stream source, Stream destination, int bufferSize, IProgress<long> progress = null, CancellationToken cancellationToken = default)
        {
            var buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                totalBytesRead += bytesRead;
                progress?.Report(totalBytesRead);
            }
        }

        /// <summary>
        /// Starts to download a uri to the specified file
        /// </summary>
        /// <param name="destinationFile">File where content should be placed</param>
        /// <param name="downloadUri">Uri used to download the file</param>
        /// <returns></returns>
        public async Task DownloadFileAsync(string path, Uri downloadUri)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            // Keep a local copy of the token because the source can change while executing this function
            var token = _cancellationTokenSource.Token;

            // Update the UI
            Text = "Starting...";
            Current = 0;

            try
            {
                // Create a new file
                using (var destination = new FileStream(path: path, FileMode.CreateNew))
                {
                    token.ThrowIfCancellationRequested();

                    // Create a client in order to download the file
                    using (var httpClient = new HttpClient())
                    {
                        // Get the response headers
                        HttpResponseMessage response = await httpClient.GetAsync(downloadUri, HttpCompletionOption.ResponseHeadersRead);
                        token.ThrowIfCancellationRequested();

                        // Read the response body as stream
                        using (var responseStream = await response.Content.ReadAsStreamAsync())
                        {
                            // Set the progress
                            Maximum = response.Content.Headers.ContentLength ?? 0;

                            // Copy from hTTP stream to file stream and monitor the progress
                            await CopyToAsyncWithProgress(responseStream, destination ,320 * 1024, this, token);
                        }
                    }

                }

                // Update the UI
                Current = Maximum;
                Text = "Download completed";
            }
            catch (Exception)
            {
                Text = "Error occurred";
            }
        }

        private void Set<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName()] string propertyName = null)
        {
            // Raise the event only if the property value has changed
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        void IProgress<long>.Report(long value)
        {
            double percent = value * 100d / Maximum;
            Current = value;
            Text = $"{percent:0}%";
        }
    }
}
