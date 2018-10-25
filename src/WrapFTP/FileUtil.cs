using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WrapFTP
{
    internal static class FileUtil
    {
        public static async Task<byte[]> ReadAllFileAsync(string filename)
        {
            Validate.NotNullOrEmptyOrWhiteSpace(filename, nameof(filename));
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                var buff = new byte[file.Length];
                await file.ReadAsync(buff, 0, (int)file.Length);
                return buff;
            }
        }

        
        public static async Task<string> ReadAllTextFileAsync(string filename)
        {
            Validate.NotNullOrEmptyOrWhiteSpace(filename, nameof(filename));
            using (var reader = File.OpenText(filename))
            {
                return await reader.ReadToEndAsync();
            }
        }

        public static async Task SaveAllFileAsync(string filename,  byte[] data)
        {
            Validate.NotNullOrEmptyOrWhiteSpace(filename, nameof(filename));

            Validate.NotNull(data, nameof(data));
            using (var file = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Write, 4096, true))
            {
                await file.WriteAsync(data, 0, data.Length);
            }
        }

        public static async Task SaveAllTextAsync(string filename, string content)
        {
            Validate.NotNullOrEmptyOrWhiteSpace(filename, nameof(filename));

            Validate.NotNull(content, nameof(content));
            using (var file = new StreamWriter(filename))
            {
                await file.WriteAsync(content);
            }
        }

        public static void Move(string source, string destination, bool overwrite)
        {
            if (File.Exists(destination))
            {
                if (overwrite)
                {
                    File.Copy(source, destination, true);
                    File.Delete(source);
                }
            }
            else
                File.Move(source, destination);
        }


        public static void MakeOld(string resourceFile)
        {
            Validate.NotNullOrEmptyOrWhiteSpace(resourceFile, nameof(resourceFile));

            var oldFile = resourceFile + "_OLD";
            var oldIndex = 1;
            while (File.Exists(oldFile))
            {
                oldFile = resourceFile + "_OLD" + oldIndex++;
            }

            File.Move(resourceFile, oldFile);
        }

        public static string MakeValidFileName(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
