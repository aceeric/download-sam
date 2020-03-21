using System;

namespace download_sam
{
    class DownLoadException : Exception
    {
        public DownLoadException(string Message) : base(Message) { }
    }
}
