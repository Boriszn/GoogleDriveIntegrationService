using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Download;


namespace GoogleDriveIntegrationService
{
    class Program
    {
        static string[] Scopes = { DriveService.Scope.Drive };
        static string ApplicationName = "GoogleDrive Integration Service";

        static void Main(string[] args)
        {
            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Define parameters of request.
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 10;
            listRequest.Fields = "nextPageToken, files(id, name)";

            // List files.
            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;

            Console.WriteLine("Files:");
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    Console.WriteLine("{0} ({1})", file.Name, file.Id);
                }
            }
            else
            {
                Console.WriteLine("No files found.");
            }

            // CreateDirectory(service);
            UploadFile(service);
            // DownloadFile(service);

            Console.Read();
        }

        public static void CreateDirectory(DriveService service)
        {
            Google.Apis.Drive.v3.Data.File newDirectory = null;

            // Create metaData for a new Directory
            Google.Apis.Drive.v3.Data.File body = new Google.Apis.Drive.v3.Data.File
            {
                Name = "test",
                Description = "description",
                MimeType = "application/vnd.google-apps.folder"
            };

            try
            {
                FilesResource.CreateRequest request = service.Files.Create(body);
                request.Execute();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
            }
        }

        public static void UploadFile(DriveService driveService)
        {
            string fileName = "input-data.json";

            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName
            };

            FilesResource.CreateMediaUpload request;
            using (var stream = new FileStream(fileName, FileMode.Open))
            {
                request = driveService.Files.Create(
                    fileMetadata, stream, "application/json");
                request.Fields = "id";
                request.Upload();
            }

            var file = request.ResponseBody;
            Console.WriteLine("File ID: " + file.Id);
        }

        public static void DownloadFile(DriveService driveService)
        {
            var fileId = "1OASA38JFCgMud7L4p4cHno6N6gUZ0vhE";
            var request = driveService.Files.Get(fileId);
            var stream = new System.IO.MemoryStream();

            request.MediaDownloader.ProgressChanged +=
                (IDownloadProgress progress) =>
                {
                    switch (progress.Status)
                    {
                        case DownloadStatus.Downloading:
                        {
                            Console.WriteLine(progress.BytesDownloaded);
                            break;
                        }
                        case DownloadStatus.Completed:
                        {
                            Console.WriteLine("Download complete.");
                            break;
                        }
                        case DownloadStatus.Failed:
                        {
                            Console.WriteLine("Download failed.");
                            break;
                        }
                    }
                };

            request.Download(stream);
        }
    }
}
