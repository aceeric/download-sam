using System;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using CryptLib;
using System.Text.RegularExpressions;
using Amazon.S3.Transfer;
using System.IO;

namespace download_sam
{
    class S3
    {
        /// <summary>
        /// Check to see if the SAM monthly file has already been downloaded by looking in the S3 bucket for a file matching the
        /// SAM filename pattern and year and month matching current system time year and month.
        /// </summary>
        /// <returns>This month's file if it exists in the bucket with the "sam/" prefix, else string.Empty</returns>
        public static string FileExists()
        {
            Globals.Log.InformationMessage("Checking for monthly file matching pattern 'SAM_PUBLIC_MONTHLY_{0}\\d\\d.ZIP' in the S3 bucket: {1} with prefix {2}", Globals.FileYYYYMM, AppSettingsImpl.Bucket.Value, AppSettingsImpl.Prefix.Initialized ? AppSettingsImpl.Prefix.Value : "(root)");

            string accessKey = AppSettingsImpl.AccessKey.Value;
            string secretKey = AppSettingsImpl.SecretKey.Value;
            if (AppSettingsImpl.Encrypted.Value)
            {
                accessKey = Crypto.Unprotect(accessKey);
                secretKey = Crypto.Unprotect(secretKey);
            }
            IAmazonS3 client = AWSClientFactory.CreateAmazonS3Client(accessKey, secretKey, RegionEndpoint.USEast1);
            try
            {
                ListObjectsRequest request = new ListObjectsRequest
                {
                    BucketName = AppSettingsImpl.Bucket.Value,
                    Prefix = AppSettingsImpl.Prefix.Initialized ? AppSettingsImpl.Prefix.Value : string.Empty,
                    MaxKeys = 1000
                };
                ListObjectsResponse response;
                string MonthlyFilePattern = string.Format("SAM_PUBLIC_MONTHLY_{0}\\d\\d.ZIP", Globals.FileYYYYMM);
                do
                {
                    response = client.ListObjects(request);
                    foreach (S3Object entry in response.S3Objects)
                    {
                        Match m = Regex.Match(entry.Key, MonthlyFilePattern);
                        if (m.Value != string.Empty)
                        {
                            return entry.Key;
                        }
                    }
                    request.Marker = response.NextMarker;
                } while (response.IsTruncated);
            }
            catch (Exception e)
            {
                throw new DownLoadException("An error occurred attempting to access S3: " + e.Message);
            }
            return string.Empty;
        }

        /// <summary>
        /// Copy the passed file to the S3 bucket
        /// </summary>
        /// <param name="Fqpn">Fully-qualified pathname of the local file. </param>

        public static void CopyToS3(string Fqpn)
        {
            Globals.Log.InformationMessage("Begin copy file to URL: S3://{0}/{1}", AppSettingsImpl.Bucket.Value, MakeKeyName(Fqpn));

            string accessKey = AppSettingsImpl.AccessKey.Value;
            string secretKey = AppSettingsImpl.SecretKey.Value;
            if (AppSettingsImpl.Encrypted.Value)
            {
                accessKey = Crypto.Unprotect(accessKey);
                secretKey = Crypto.Unprotect(secretKey);
            }
            IAmazonS3 client = AWSClientFactory.CreateAmazonS3Client(accessKey, secretKey, RegionEndpoint.USEast1);
            TransferUtility utility = new TransferUtility(client);
            TransferUtilityUploadRequest request = new TransferUtilityUploadRequest()
            {
                BucketName = AppSettingsImpl.Bucket.Value,
                Key = MakeKeyName(Fqpn),
                FilePath = Fqpn
            };
            try
            {
                utility.Upload(request);
            }
            catch (Exception e)
            {
                throw new DownLoadException(string.Format("Attempt to upload file to S3 failed with exception: {0}", e.Message));
            }

            Globals.Log.InformationMessage("Copied file to S3");
        }

        /// <summary>
        /// Creates an S3 Key Name from the passed Fully Qualified Path name, using the Prefix value from the settings
        /// or command line if provided. E.g. if the Fqpn is "c:\x\y\z\abc.txt" and the S3 prefix is "q" then the
        /// generated key name will be "q/abc.txt"
        /// </summary>
        /// <param name="Fqpn">Fully Qualified Path name of the fileto upload to S3</param>
        /// <returns>The S3 key name.</returns>

        private static string MakeKeyName(string Fqpn)
        {
            string Prefix = AppSettingsImpl.Prefix.Initialized ? AppSettingsImpl.Prefix.Value + "/" : string.Empty;
            string FileName;
            if (AppSettingsImpl.KeyName.Initialized)
            {
                FileName = AppSettingsImpl.KeyName.Value;
            }
            else
            {
                FileName = Path.GetFileName(Fqpn);
            }
            return Prefix + FileName;
        }
    }
}
