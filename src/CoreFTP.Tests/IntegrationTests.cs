using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CoreFTP.Tests
{
    [TestClass]
    public class IntegrationTests
    {
        [TestMethod]
        public async Task UploadFile()
        {
            var ftpFolder = Path.Combine(Path.GetTempPath(), "coreftp_test");
            var ftpUploadsFolder = Path.Combine(ftpFolder, "uploads");
            
            #region Setup of test ftp server
            foreach (var process in Process.GetProcessesByName("ftpdmin"))
            {
                System.Diagnostics.Debug.WriteLine("Deleted process");
                process.Kill();
                System.Threading.Thread.Sleep(1000);
            }


            Directory.CreateDirectory(ftpFolder);
            if (Directory.Exists(ftpUploadsFolder))
                Directory.Delete(ftpUploadsFolder, true);
            Directory.CreateDirectory(ftpUploadsFolder);

            using (var fs = new FileStream(Path.Combine(ftpFolder, "ftpdmin.exe"), FileMode.Create))
                await Assembly.GetExecutingAssembly().GetManifestResourceStream("CoreFTP.Tests.TestServer.ftpdmin.exe")
                    .CopyToAsync(fs);


            var ftpServerProcess = Process.Start(new ProcessStartInfo()
            {
                FileName = Path.Combine(ftpFolder, "ftpdmin.exe"),
                Arguments = $"-ha 127.0.0.1 \"{ftpUploadsFolder}\"",
                WorkingDirectory = ftpFolder
            });
            #endregion

            var ftpClient = new FtpClient("localhost");

            var tempFileToUpload = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFileToUpload, "sample content");

            await ftpClient.Upload(tempFileToUpload);

            var filesInUploadsFolder = Directory.GetFiles(ftpUploadsFolder);

            Assert.AreEqual(1, filesInUploadsFolder.Length);

            var uploadFileContent = await File.ReadAllTextAsync(filesInUploadsFolder[0]);

            Assert.AreEqual("sample content", uploadFileContent);

            var tempFileDownloaded = Path.GetTempFileName();
            await ftpClient.Download(Path.GetFileName(tempFileToUpload), tempFileDownloaded);

            var downloadFileContent = await File.ReadAllTextAsync(tempFileDownloaded);
            Assert.AreEqual("sample content", downloadFileContent);

            var remoteFiles = await ftpClient.ListDirectory();
            Assert.AreEqual(1, remoteFiles.Length);
            Assert.AreEqual(Path.GetFileName(tempFileToUpload), remoteFiles[0]);

            await ftpClient.Rename(Path.GetFileName(tempFileToUpload), "remoteFileRenamed.txt");

            var remoteFilesAfterRename = await ftpClient.ListDirectory();
            Assert.AreEqual(1, remoteFilesAfterRename.Length);
            Assert.AreEqual("remoteFileRenamed.txt", remoteFilesAfterRename[0]);

            await ftpClient.MakeDirectory("remoteDir");

            var remoteFilesAfterDirectoryMake = await ftpClient.ListDirectory();
            Assert.AreEqual(2, remoteFilesAfterDirectoryMake.Length);
            Assert.IsNotNull(remoteFilesAfterDirectoryMake.FirstOrDefault(_ => _ == "remoteDir"));

            //var lastModifiedTimestamp = await ftpClient.GetLastModifiedTimestamp("remoteFileRenamed.txt");

            //Assert.IsTrue(lastModifiedTimestamp != default(DateTime));
        }
    }
}
