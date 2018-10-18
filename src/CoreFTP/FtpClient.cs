using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreFTP
{
    public class FtpClient
    {
        public string Host { get; }
        public int Port { get; }
        public string Username { get; }
        private string Password { get; }

        public FtpClient(string host, int port = 21, string username = null, string password = null)
        {
            Validate.NotNullOrEmptyOrWhiteSpace(host, nameof(host));
            Validate.Positive(port, nameof(port));

            Host = host;
            Port = port;
            Username = username;
            Password = password;
        }

        public async Task Upload(string localFileName, string remoteFileName = null)
        {
            Validate.NotNullOrEmptyOrWhiteSpace(localFileName, nameof(localFileName));

            remoteFileName = remoteFileName ?? Path.GetFileName(localFileName);

            var request = (FtpWebRequest)WebRequest.Create($"ftp://{Host}:{Port}/{remoteFileName}");
            request.Method = WebRequestMethods.Ftp.UploadFile;

            request.Credentials = new NetworkCredential(Username ?? "anonymous", Password);

            request.ContentLength = new FileInfo(localFileName).Length;

            using (var requestStream = await request.GetRequestStreamAsync())
            {
                using (var fs = new FileStream(localFileName, FileMode.Open))
                    await fs.CopyToAsync(requestStream);
            }

            using (await request.GetResponseAsync()) { }
        }

        public async Task Download(string remoteFileName, string localFileName)
        {
            Validate.NotNullOrEmptyOrWhiteSpace(remoteFileName, nameof(remoteFileName));
            Validate.NotNullOrEmptyOrWhiteSpace(localFileName, nameof(localFileName));

            if (Path.IsPathRooted(remoteFileName))
                throw new ArgumentException("Must be a relative file path", nameof(remoteFileName));

            var request = (FtpWebRequest)WebRequest.Create($"ftp://{Host}:{Port}/{remoteFileName}");
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            request.Credentials = new NetworkCredential(Username ?? "anonymous", Password);

            using (var response = ((FtpWebResponse)await request.GetResponseAsync()))
            {
                using (var responseStream = response.GetResponseStream())
                using (var fs = new FileStream(localFileName, FileMode.Create))
                    await responseStream.CopyToAsync(fs);
            }
        }

        public async Task<string[]> ListDirectory(string remoteFolder = null)
        {
            var request = (FtpWebRequest)WebRequest.Create($"ftp://{Host}:{Port}/{remoteFolder}");
            request.Method = WebRequestMethods.Ftp.ListDirectory;

            request.Credentials = new NetworkCredential(Username ?? "anonymous", Password);

            using (var response = ((FtpWebResponse)await request.GetResponseAsync()))
            {
                using (var responseStream = response.GetResponseStream())
                using (var streamReader = new StreamReader(responseStream))
                {
                    var responseContent = await streamReader.ReadToEndAsync();
                    return responseContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
        }

        public async Task<string[]> ListDirectoryDetails(string remoteDirectory = null)
        {
            var request = (FtpWebRequest)WebRequest.Create($"ftp://{Host}:{Port}/{remoteDirectory}");
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            request.Credentials = new NetworkCredential(Username ?? "anonymous", Password);

            using (var response = ((FtpWebResponse)await request.GetResponseAsync()))
            {
                using (var responseStream = response.GetResponseStream())
                using (var streamReader = new StreamReader(responseStream))
                {
                    var responseContent = await streamReader.ReadToEndAsync();
                    return responseContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
        }

        public async Task Rename(string oldRemoteFileName, string newRemoteFileName)
        {
            var request = (FtpWebRequest)WebRequest.Create($"ftp://{Host}:{Port}/{oldRemoteFileName}");
            request.Method = WebRequestMethods.Ftp.Rename;
            request.RenameTo = newRemoteFileName;
            request.Credentials = new NetworkCredential(Username ?? "anonymous", Password);

            using (await request.GetResponseAsync()) { }
        }


        public async Task MakeDirectory(string remoteDirectoryName)
        {
            var request = (FtpWebRequest)WebRequest.Create($"ftp://{Host}:{Port}/{remoteDirectoryName}");
            request.Method = WebRequestMethods.Ftp.MakeDirectory;
            request.Credentials = new NetworkCredential(Username ?? "anonymous", Password);

            using (await request.GetResponseAsync()) { }
        }

        public async Task Delete(string remoteFileName)
        {
            var request = (FtpWebRequest)WebRequest.Create($"ftp://{Host}:{Port}/{remoteFileName}");
            request.Method = WebRequestMethods.Ftp.Rename;
            request.Credentials = new NetworkCredential(Username ?? "anonymous", Password);

            using (await request.GetResponseAsync()) { }
        }

        public async Task<DateTime> GetLastModifiedTimestamp(string remoteFileName)
        {
            var request = (FtpWebRequest)WebRequest.Create($"ftp://{Host}:{Port}/{remoteFileName}");
            request.Method = WebRequestMethods.Ftp.GetDateTimestamp;
            request.Credentials = new NetworkCredential(Username ?? "anonymous", Password);

            using (var response = await request.GetResponseAsync())
            {
                using (var responseStream = response.GetResponseStream())
                using (var streamReader = new StreamReader(responseStream))
                {
                    var responseContent = await streamReader.ReadToEndAsync();
                    if (long.TryParse(responseContent, out var fileTime))
                        return DateTime.FromFileTimeUtc(fileTime);

                    throw new FtpReplyException("Unable to understand reply from server");
                }
            }
        }


    }
}
