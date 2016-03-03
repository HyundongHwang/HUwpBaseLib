using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Security.Cryptography;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.Web.Syndication;

namespace HUwpBaseLib.Utils
{
    public static class HublUtils
    {
        public static async Task<string> HttpGetStringAsync(string url, CancellationToken token = new CancellationToken())
        {
            var client = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            var res = await client.SendAsync(req, token);
            var resStream = await res.Content.ReadAsStreamAsync();
            var resStr = await res.Content.ReadAsStringAsync();
            return resStr;
        }

        public static async Task HttpGetFileAsync(string url, StorageFile targetFile, Action<int, int> progressAction, CancellationToken token = new CancellationToken())
        {
            var client = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            var res = await client.SendAsync(req, token);
            var resStream = await res.Content.ReadAsStreamAsync();
            var buf = new byte[1000];
            var resMax = res.Content.Headers.ContentLength > 0 ? res.Content.Headers.ContentLength : 100;
            progressAction.Invoke(0, (int)resMax);

            using (var tr = await targetFile.OpenTransactedWriteAsync())
            {
                while (true)
                {
                    var readLen = await resStream.ReadAsync(buf, 0, buf.Length);

                    if (readLen <= 0)
                        break;

                    var ibuf = CryptographicBuffer.CreateFromByteArray(buf);
                    ibuf.Length = (uint)readLen;
                    await tr.Stream.WriteAsync(ibuf);
                    progressAction.Invoke((int)tr.Stream.Size, (int)resMax);
                }

                await tr.CommitAsync();
                progressAction.Invoke((int)resMax, (int)resMax);
            }
        }

        public static string GetExt(string url)
        {
            var ext = "";

            try
            {
                var absPath = new Uri(url).AbsolutePath;
                ext = Path.GetExtension(absPath);
            }
            catch (Exception ex)
            {
            }

            return ext;
        }

        public static string CombineUrl(string url, string addend)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            if (string.IsNullOrEmpty(addend))
                return url;




            var uri = new Uri(url);
            var trimedPath = addend.TrimStart(new[] { '/' });
            var combinedUrl = $"{uri.Scheme}://{uri.Host}/{trimedPath}";
            return combinedUrl;
        }

        public static string GetComputerInfo()
        {
            var eas = new EasClientDeviceInformation();
            var ai = AnalyticsInfo.VersionInfo;
            var package = Package.Current;



            var hwId = eas.Id.ToString();




            var hwManufacturer = eas.SystemManufacturer;
            var hwProductName = eas.SystemProductName;
            var hwSku = eas.SystemSku;




            var osCat = ai.DeviceFamily;

            var sv = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
            var v = ulong.Parse(sv);
            var v1 = (v & 0xFFFF000000000000L) >> 48;
            var v2 = (v & 0x0000FFFF00000000L) >> 32;
            var v3 = (v & 0x00000000FFFF0000L) >> 16;
            var v4 = (v & 0x000000000000FFFFL);
            var osVersion = $"{v1}.{v2}.{v3}.{v4}";

            var osArchitecture = package.Id.Architecture.ToString();



            var appName = package.DisplayName;
            var appId = package.Id.Name;

            var pv = package.Id.Version;
            var appVersion = $"{pv.Major}.{pv.Minor}.{pv.Build}.{pv.Revision}";

            return $@"
hwId : {hwId}
hwManufacturer : {hwManufacturer} 
hwProductName : {hwProductName} 
osCat : {osCat} 
osVersion : {osVersion} 
osArchitecture : {osArchitecture} 
appName : {appName} 
appId : {appId} 
pv : {pv} 
appVersion : {appVersion} 
            ";
        }
    }
}
