using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WrapFTP
{
    public class FtpClient
    {
        public string Host { get; }
        public int Port { get; }
        public string Username { get; }
        private string Password { get; }
        public int Timeout { get; }
        public bool PassiveMode { get; }
        public bool EnableSsl { get; }

        public FtpClient(string host, int port = 21, string username = null, string password = null, int timeout = -1, bool passiveMode = true, bool enableSsl = false)
        {
            Validate.NotNullOrEmptyOrWhiteSpace(host, nameof(host));
            Validate.Positive(port, nameof(port));

            Host = host;
            Port = port;
            Username = username;
            Password = password;
            Timeout = timeout;
            PassiveMode = passiveMode;
            EnableSsl = enableSsl;
        }

        private FtpWebRequest GetFtpWebRequest(string remotePath)
        {
            var request = (FtpWebRequest)WebRequest.Create($"ftp://{Host}:{Port}/{remotePath}");
            request.Credentials = new NetworkCredential(Username ?? "anonymous", Password);
            request.Timeout = Timeout;
            request.ReadWriteTimeout = Timeout;
            request.KeepAlive = false;
            request.ServicePoint.ConnectionLimit = 1000;
            request.UsePassive = PassiveMode;
            request.EnableSsl = EnableSsl;

            return request;
        }

        public async Task Upload(string localFileName, string remoteFileName = null)
        {
            Validate.NotNullOrEmptyOrWhiteSpace(localFileName, nameof(localFileName));

            remoteFileName = remoteFileName ?? Path.GetFileName(localFileName);

            var request = GetFtpWebRequest(remoteFileName);

            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.ContentLength = new FileInfo(localFileName).Length;

            using (var requestStream = await request.GetRequestStreamAsync())
            {
                using (var fs = new FileStream(localFileName, FileMode.Open))
                    await fs.CopyToAsync(requestStream);
            }

            using (await request.GetResponseAsync()) { }
        }

        public async Task Upload(Stream stream, string remoteFileName)
        {
            Validate.NotNull(stream, nameof(stream));
            Validate.NotNullOrEmptyOrWhiteSpace(remoteFileName, nameof(remoteFileName));

            var request = GetFtpWebRequest(remoteFileName);

            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.ContentLength = stream.Length;

            using (var requestStream = await request.GetRequestStreamAsync())
            {
                await stream.CopyToAsync(requestStream);
            }

            using (await request.GetResponseAsync()) { }
        }

        public async Task Upload(byte[] buffer, string remoteFileName)
        {
            Validate.NotNull(buffer, nameof(buffer));
            Validate.NotNullOrEmptyOrWhiteSpace(remoteFileName, nameof(remoteFileName));

            var request = GetFtpWebRequest(remoteFileName);

            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.ContentLength = buffer.Length;

            using (var requestStream = await request.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(buffer, 0, buffer.Length);
            }

            using (await request.GetResponseAsync()) { }
        }



        public async Task Download(string remoteFileName, string localFileName)
        {
            Validate.NotNullOrEmptyOrWhiteSpace(remoteFileName, nameof(remoteFileName));
            Validate.NotNullOrEmptyOrWhiteSpace(localFileName, nameof(localFileName));

            //if (Path.IsPathRooted(remoteFileName))
            //    throw new ArgumentException("Must be a relative file path", nameof(remoteFileName));

            var request = GetFtpWebRequest(remoteFileName);

            request.Method = WebRequestMethods.Ftp.DownloadFile;

            using (var response = ((FtpWebResponse)await request.GetResponseAsync()))
            {
                using (var responseStream = response.GetResponseStream())
                using (var fs = new FileStream(localFileName, FileMode.Create))
                    await responseStream.CopyToAsync(fs);
            }
        }

        public async Task<Stream> Download(string remoteFileName)
        {
            Validate.NotNullOrEmptyOrWhiteSpace(remoteFileName, nameof(remoteFileName));

            //if (Path.IsPathRooted(remoteFileName))
            //    throw new ArgumentException("Must be a relative file path", nameof(remoteFileName));

            var request = GetFtpWebRequest(remoteFileName);

            request.Method = WebRequestMethods.Ftp.DownloadFile;

            using (var response = ((FtpWebResponse)await request.GetResponseAsync()))
            {
                using (var responseStream = response.GetResponseStream())
                {
                    var ms = new MemoryStream();
                    await responseStream.CopyToAsync(ms);

                    ms.Seek(0, SeekOrigin.Begin);

                    return ms;
                }
            }
        }

        public async Task<string[]> ListDirectory(string remoteDirectory = null)
        {
            if (remoteDirectory != null)
            {
                remoteDirectory = remoteDirectory.Replace('\\', '/');
                if (!remoteDirectory.EndsWith("/"))
                    remoteDirectory += "/";
            }

            var request = GetFtpWebRequest(remoteDirectory);

            request.Method = WebRequestMethods.Ftp.ListDirectory;

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
            if (remoteDirectory != null)
            {
                remoteDirectory = remoteDirectory.Replace('\\', '/');
                if (!remoteDirectory.EndsWith("/"))
                    remoteDirectory += "/";
            }

            var request = GetFtpWebRequest(remoteDirectory);

            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

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
            var request = GetFtpWebRequest(oldRemoteFileName);

            request.Method = WebRequestMethods.Ftp.Rename;
            request.RenameTo = newRemoteFileName;

            using (await request.GetResponseAsync()) { }
        }


        public async Task MakeDirectory(string remoteDirectoryName)
        {
            var request = GetFtpWebRequest(remoteDirectoryName);

            request.Method = WebRequestMethods.Ftp.MakeDirectory;

            using (await request.GetResponseAsync()) { }
        }

        public async Task RemoveDirectory(string remoteDirectoryName)
        {
            var request = GetFtpWebRequest(remoteDirectoryName);

            request.Method = WebRequestMethods.Ftp.RemoveDirectory;

            using (await request.GetResponseAsync()) { }
        }

        public async Task Delete(string remoteFileName)
        {
            var request = GetFtpWebRequest(remoteFileName);
            request.Method = WebRequestMethods.Ftp.DeleteFile;

            using (await request.GetResponseAsync()) { }
        }

        public async Task<DateTime> GetLastModifiedTimestamp(string remoteFileName)
        {
            var request = GetFtpWebRequest(remoteFileName);
            request.Method = WebRequestMethods.Ftp.GetDateTimestamp;

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

        public async Task<long> GetFileSize(string remoteFileName)
        {
            var request = GetFtpWebRequest(remoteFileName);
            request.Method = WebRequestMethods.Ftp.GetFileSize;

            using (var response = await request.GetResponseAsync())
            {
                using (var responseStream = response.GetResponseStream())
                using (var streamReader = new StreamReader(responseStream))
                {
                    var responseContent = await streamReader.ReadToEndAsync();
                    if (long.TryParse(responseContent, out var fileSize))
                        return fileSize;

                    throw new FtpReplyException("Unable to understand reply from server");
                }
            }
        }


    }
}
