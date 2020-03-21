using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace download_sam
{
    class DownloadAsync
    {
        public static bool DoDownload(string FromUrl, string ToFqpn, int MaxSeconds)
        {
            bool Completed = false;

            List<Task> tasks = new List<Task>();

            tasks.Add(Task.Factory.StartNew(() =>
            {
                WebClient client = new WebClient();
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback += (send, certificate, chain, sslPolicyErrors) => { return true; };
                client.DownloadFile(new Uri(FromUrl), ToFqpn); // throws if it fails
                //await downloadfileasync? with token?
                Completed = true;
            }));

            tasks.Add(Task.Factory.StartNew(() => Thread.Sleep(TimeSpan.FromSeconds(MaxSeconds))));

            try
            {
                // if the download finishes before the timeout then Completed will be true
                Task.WaitAny(tasks.ToArray());
            }
            catch { }
            // TODO -- cancellation
            // https://codereview.stackexchange.com/questions/117743/background-task-with-instant-abort-capability-in-c
            return Completed;
        }
    }
}
