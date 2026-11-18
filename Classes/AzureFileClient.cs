using Azure;
using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Http;
using System.Configuration;
using System.IO;
using System.Net;

namespace AzureFileStorageClient
{
    public class AzureFileClient
    {
        const int uploadLimit = 4194304;
        public static void UploadFile(string folderName, string localFilePath, string fileShareName = "main-share")
        {
            //main-share
            //MainPhotos
            string fileName = System.IO.Path.GetFileName(localFilePath);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string connectionString = "DefaultEndpointsProtocol=https;AccountName=sqlvazxycxhw5bmcqe;AccountKey=dGpc2E7s9+wA0LDcEInFsrGPeuQCm9ADbFEZv/+P0sltg/nmhvfZlrBdFaVA+TFMdS9C3y78bUglRDGwHFYbnQ==;EndpointSuffix=core.windows.net";

            ShareClient share = new ShareClient(connectionString, fileShareName);

            var directory = share.GetDirectoryClient(folderName);

            var file = directory.GetFileClient(fileName);
            using (FileStream stream = File.OpenRead(localFilePath))
            {
                file.Create(stream.Length);
                if (stream.Length <= uploadLimit)
                {
                    file.UploadRange(new HttpRange(0, stream.Length), stream);
                }
                else
                {
                    int bytesRead;
                    long index = 0;
                    byte[] buffer = new byte[uploadLimit];
                    // Stream is larger than the limit so we need to upload in chunks
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        // Create a memory stream for the buffer to upload
                        MemoryStream ms = new MemoryStream(buffer, 0, bytesRead);
                        file.UploadRange(new HttpRange(index, ms.Length), ms);
                        index += ms.Length; // increment the index to the account for bytes already written
                    }
                }
            }
        }

        public static void UploadFile(string folderName, string fileName, IFormFile formFile, string fileShareName = "main-share")
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string connectionString = "DefaultEndpointsProtocol=https;AccountName=sqlvazxycxhw5bmcqe;AccountKey=dGpc2E7s9+wA0LDcEInFsrGPeuQCm9ADbFEZv/+P0sltg/nmhvfZlrBdFaVA+TFMdS9C3y78bUglRDGwHFYbnQ==;EndpointSuffix=core.windows.net";

            ShareClient share = new ShareClient(connectionString, fileShareName);

            var directory = share.GetDirectoryClient(folderName);

            var file = directory.GetFileClient(fileName);
            using (Stream stream = formFile.OpenReadStream())
            {
                file.Create(stream.Length);
                if (stream.Length <= uploadLimit)
                {
                    file.UploadRange(new HttpRange(0, formFile.OpenReadStream().Length), stream);
                }
                else
                {
                    int bytesRead;
                    long index = 0;
                    byte[] buffer = new byte[uploadLimit];
                    // Stream is larger than the limit so we need to upload in chunks
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        // Create a memory stream for the buffer to upload
                        MemoryStream ms = new MemoryStream(buffer, 0, bytesRead);
                        file.UploadRange(new HttpRange(index, ms.Length), ms);
                        index += ms.Length; // increment the index to the account for bytes already written
                    }
                }
            }
        }

    }
}
