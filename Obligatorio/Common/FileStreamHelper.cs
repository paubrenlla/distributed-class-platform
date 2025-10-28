using System;
using System.IO;
using System.Threading.Tasks;

namespace Common
{
    public class FileStreamHelper
    {
        public async Task<byte[]> Read(string path, long offset, int length)
        {
            byte[] data = new byte[length];

            using (FileStream fs = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true))
            {
                fs.Position = offset;
                int bytesRead = 0;
                while (bytesRead < length)
                {
                    int read = await fs.ReadAsync(data, bytesRead, length - bytesRead);
                    if (read == 0)
                        throw new Exception("File could not be read");
                    bytesRead += read;
                }
            }

            return data;
        }

        public async Task Write(string path, byte[] data)
        {
            FileMode mode = File.Exists(path) ? FileMode.Append : FileMode.Create;

            using (FileStream fs = new FileStream(
                path,
                mode,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true))
            {
                await fs.WriteAsync(data, 0, data.Length);
            }
        }
    }
}
