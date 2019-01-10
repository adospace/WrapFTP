using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WrapFTP.Tests
{
    [TestClass]
    public class IntegrationTests
    {
        [TestMethod]
        public async Task Complete()
        {
            var ftpFolder = Path.Combine(Path.GetTempPath(), "WrapFTP_test");
            var ftpUploadsFolder = Path.Combine(ftpFolder, "uploads");
            
            #region Setup of test ftp server
            foreach (var process in Process.GetProcessesByName("ftpdmin"))
            {
                process.Kill();
                System.Threading.Thread.Sleep(1000);
            }


            Directory.CreateDirectory(ftpFolder);
            if (Directory.Exists(ftpUploadsFolder))
                Directory.Delete(ftpUploadsFolder, true);
            Directory.CreateDirectory(ftpUploadsFolder);

            using (var fs = new FileStream(Path.Combine(ftpFolder, "ftpdmin.exe"), FileMode.Create))
                await Assembly.GetExecutingAssembly().GetManifestResourceStream("WrapFTP.Tests.TestServer.ftpdmin.exe")
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

            //ftpadmin doesn't support newest ftp commands
            //var lastModifiedTimestamp = await ftpClient.GetLastModifiedTimestamp("remoteFileRenamed.txt");

            //Assert.IsTrue(lastModifiedTimestamp != default(DateTime));

            //nor SIZE command...
            //var fileSize = await ftpClient.GetFileSize("remoteFileRenamed.txt");

            //Assert.AreEqual(new FileInfo(tempFileToUpload).Length, fileSize);

            #region Shutdown test ftp server
            foreach (var process in Process.GetProcessesByName("ftpdmin"))
            {
                process.Kill();
                System.Threading.Thread.Sleep(1000);
            }
            #endregion
        }

        [TestMethod]
        public async Task UploadBinary()
        {
            var ftpFolder = Path.Combine(Path.GetTempPath(), "WrapFTP_test");
            var ftpUploadsFolder = Path.Combine(ftpFolder, "uploads");

            #region Setup of test ftp server
            foreach (var process in Process.GetProcessesByName("ftpdmin"))
            {
                process.Kill();
                System.Threading.Thread.Sleep(1000);
            }


            Directory.CreateDirectory(ftpFolder);
            if (Directory.Exists(ftpUploadsFolder))
                Directory.Delete(ftpUploadsFolder, true);
            Directory.CreateDirectory(ftpUploadsFolder);

            using (var fs = new FileStream(Path.Combine(ftpFolder, "ftpdmin.exe"), FileMode.Create))
                await Assembly.GetExecutingAssembly().GetManifestResourceStream("WrapFTP.Tests.TestServer.ftpdmin.exe")
                    .CopyToAsync(fs);


            var ftpServerProcess = Process.Start(new ProcessStartInfo()
            {
                FileName = Path.Combine(ftpFolder, "ftpdmin.exe"),
                Arguments = $"-ha 127.0.0.1 \"{ftpUploadsFolder}\"",
                WorkingDirectory = ftpFolder
            });
            #endregion

            var ftpClient = new FtpClient("localhost");


            await ftpClient.Upload(Encoding.UTF8.GetBytes("sample content"), "test_file.txt");

            var filesInUploadsFolder = Directory.GetFiles(ftpUploadsFolder);

            Assert.AreEqual(1, filesInUploadsFolder.Length);

            var uploadFileContent = await File.ReadAllTextAsync(filesInUploadsFolder[0]);

            Assert.AreEqual("sample content", uploadFileContent);

            #region Shutdown test ftp server
            foreach (var process in Process.GetProcessesByName("ftpdmin"))
            {
                process.Kill();
                System.Threading.Thread.Sleep(1000);
            }
            #endregion
        }

        [TestMethod]
        public async Task UploadStream()
        {
            var ftpFolder = Path.Combine(Path.GetTempPath(), "WrapFTP_test");
            var ftpUploadsFolder = Path.Combine(ftpFolder, "uploads");

            #region Setup of test ftp server
            foreach (var process in Process.GetProcessesByName("ftpdmin"))
            {
                process.Kill();
                System.Threading.Thread.Sleep(1000);
            }


            Directory.CreateDirectory(ftpFolder);
            if (Directory.Exists(ftpUploadsFolder))
                Directory.Delete(ftpUploadsFolder, true);
            Directory.CreateDirectory(ftpUploadsFolder);

            using (var fs = new FileStream(Path.Combine(ftpFolder, "ftpdmin.exe"), FileMode.Create))
                await Assembly.GetExecutingAssembly().GetManifestResourceStream("WrapFTP.Tests.TestServer.ftpdmin.exe")
                    .CopyToAsync(fs);


            var ftpServerProcess = Process.Start(new ProcessStartInfo()
            {
                FileName = Path.Combine(ftpFolder, "ftpdmin.exe"),
                Arguments = $"-ha 127.0.0.1 \"{ftpUploadsFolder}\"",
                WorkingDirectory = ftpFolder
            });
            #endregion

            var ftpClient = new FtpClient("localhost");

            await ftpClient.Upload(new MemoryStream(Encoding.UTF8.GetBytes("sample content")), "test_file.txt");
            await ftpClient.Upload(new MemoryStream(Encoding.UTF8.GetBytes("sample content edited")), "test_file.txt");

            var filesInUploadsFolder = Directory.GetFiles(ftpUploadsFolder);

            Assert.AreEqual(1, filesInUploadsFolder.Length);

            var uploadFileContent = await File.ReadAllTextAsync(filesInUploadsFolder[0]);

            Assert.AreEqual("sample content edited", uploadFileContent);

            #region Shutdown test ftp server
            foreach (var process in Process.GetProcessesByName("ftpdmin"))
            {
                process.Kill();
                System.Threading.Thread.Sleep(1000);
            }
            #endregion
        }

        [TestMethod]
        public async Task DownloadStream()
        {
            var ftpFolder = Path.Combine(Path.GetTempPath(), "WrapFTP_test");
            var ftpUploadsFolder = Path.Combine(ftpFolder, "uploads");

            #region Setup of test ftp server
            foreach (var process in Process.GetProcessesByName("ftpdmin"))
            {
                process.Kill();
                System.Threading.Thread.Sleep(1000);
            }


            Directory.CreateDirectory(ftpFolder);
            if (Directory.Exists(ftpUploadsFolder))
                Directory.Delete(ftpUploadsFolder, true);
            Directory.CreateDirectory(ftpUploadsFolder);

            using (var fs = new FileStream(Path.Combine(ftpFolder, "ftpdmin.exe"), FileMode.Create))
                await Assembly.GetExecutingAssembly().GetManifestResourceStream("WrapFTP.Tests.TestServer.ftpdmin.exe")
                    .CopyToAsync(fs);


            var ftpServerProcess = Process.Start(new ProcessStartInfo()
            {
                FileName = Path.Combine(ftpFolder, "ftpdmin.exe"),
                Arguments = $"-ha 127.0.0.1 \"{ftpUploadsFolder}\"",
                WorkingDirectory = ftpFolder
            });
            #endregion

            var ftpClient = new FtpClient("localhost");

            await ftpClient.Upload(new MemoryStream(Encoding.UTF8.GetBytes("sample content")), "test_file.txt");

            var stream = await ftpClient.Download("test_file.txt");
            string fileContent = null;
            using (var sr = new StreamReader(stream, Encoding.UTF8, false))
            {
                fileContent = await sr.ReadToEndAsync();
            }

            Assert.AreEqual("sample content", fileContent);
        }

    }
}
