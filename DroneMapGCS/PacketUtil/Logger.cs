using System;
using System.IO;

namespace PacketUtil
{
    public class Logger 
    {
        public int Mode=Packet.START_LOGGING_CSV;

        public static string LogPath = Path.Combine(Environment.CurrentDirectory, "log");

        static Logger()
        {
            // LogPath 디렉터리가 존재하지 않으면 생성
            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }
        }

        private static string path = "";
        public static string GetPath()
        {
            return path;
        }

        private string filename;

        public Logger(int mode,string newpath, string filename, string? header = null)
        {
            this.Mode = mode;
            path = System.IO.Path.Combine(LogPath, newpath);
            Directory.CreateDirectory(path);
            this.filename = path + "//" + filename;

            if (header != null)
                WriteLine(header);
        }

        public Logger(int mode, string filename)
        {
            this.Mode = mode;
            this.filename = filename;

        }

        public void WriteLine(string msg)
        {
            using (StreamWriter writer = new StreamWriter(this.filename, append: true))
            {
                writer.WriteLine(msg);
            }
        }

        public void WriteBytes(byte[] msg, int length)
        {
            using (FileStream fs = new FileStream(this.filename, FileMode.Append, FileAccess.Write))
            {
                fs.Write(msg, 0, length);
            }
        }
    }
}