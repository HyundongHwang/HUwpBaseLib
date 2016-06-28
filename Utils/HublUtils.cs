using HUwpBaseLib.Logs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFi;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Syndication;
using Windows.Foundation;
using Windows.Security.Cryptography.Certificates;
using System.Runtime.InteropServices.WindowsRuntime;

namespace HUwpBaseLib.Utils
{
    public static class HublUtils
    {
        public static async Task ConnectWifiAsync(string ssidNameSubStr, string id = null, string pw = null)
        {
            Log.d($"wifi searching ...");
            var deviceInfoList = await DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());

            if (!deviceInfoList.Any())
                throw new Exception("wifi device not found !");



            Log.d($"wifi device is exist !");
            var firstInfo = deviceInfoList.First();
            Log.d($"firstInfo.Id : {firstInfo.Id}");
            Log.d($"firstInfo.Name : {firstInfo.Name}");
            var firstAdapter = await WiFiAdapter.FromIdAsync(firstInfo.Id);
            Log.d($"firstAdapter.ScanAsync ...");
            await firstAdapter.ScanAsync();

            var allNwInfoList = firstAdapter.NetworkReport.AvailableNetworks.Select(nw => new
            {
                nw.Ssid,
                nw.SignalBars,
                nw.Bssid,
                NetworkAuthenticationType = nw.SecuritySettings.NetworkAuthenticationType.ToString(),
                NetworkEncryptionType = nw.SecuritySettings.NetworkEncryptionType.ToString()
            });

            if (!allNwInfoList.Any())
                throw new Exception("allNwInfoList not found !");



            Log.d($"allNwInfoList : {JsonConvert.SerializeObject(allNwInfoList, Formatting.Indented)}");
            var targetNetwork = firstAdapter.NetworkReport.AvailableNetworks.FirstOrDefault(nw => nw.Ssid.ToLower().Contains(ssidNameSubStr.ToLower()));

            if (targetNetwork == null)
                throw new Exception("targetNetwork not found !");



            Log.d($"targetNetwork : {targetNetwork.Ssid}");
            Log.d($"targetNetwork ConnectAsync ...");

            PasswordCredential passwordCredential = null;

            if (string.IsNullOrWhiteSpace(id))
            {
                passwordCredential = new PasswordCredential
                {
                    Password = pw,
                };
            }
            else
            {
                passwordCredential = new PasswordCredential
                {
                    UserName = id,
                    Password = pw,
                };
            }

            var wifiResult = await firstAdapter.ConnectAsync(targetNetwork, WiFiReconnectionKind.Automatic, passwordCredential);
            Log.d($"wifiResult.ConnectionStatus : {wifiResult.ConnectionStatus}");

            if (wifiResult.ConnectionStatus != WiFiConnectionStatus.Success)
                throw new Exception($"wifiResult.ConnectionStatus : {wifiResult.ConnectionStatus.ToString()}");
        }



        public static async Task<string> HttpGetStringAsync(string url, CancellationToken token = new CancellationToken())
        {
            using (var httpFilter = new HUwpHttpFilter())
            using (var client = new HttpClient(httpFilter))
            using (var response = await client.SendRequestAsync(new HttpRequestMessage(HttpMethod.Get, new Uri(url))))
            {
                response.EnsureSuccessStatusCode();
                var resStr = await response.Content.ReadAsStringAsync();
                return resStr;
            }
        }



        public static async Task HttpGetFileAsync(string url, StorageFile targetFile, Action<int, int> progressAction, CancellationToken token = new CancellationToken())
        {
            using (var httpFilter = new HUwpHttpFilter())
            using (var client = new HttpClient(httpFilter))
            using (var response = await client.SendRequestAsync(new HttpRequestMessage(HttpMethod.Get, new Uri(url)), HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                using (var resStream = await response.Content.ReadAsInputStreamAsync())
                using (var classicResStream = resStream.AsStreamForRead())
                {
                    var resContentLength = response.Content.Headers.ContentLength;

                    using (var fileStream = await targetFile.OpenStreamForWriteAsync())
                    {
                        var buf = new byte[1000000];
                        var offset = 0;
                        fileStream.Seek(0, SeekOrigin.Begin);

                        while (true)
                        {
                            int read = await classicResStream.ReadAsync(buf, 0, buf.Length, token);
                            progressAction.Invoke(offset, (int)resContentLength);

                            if (read <= 0)
                                break;

                            await fileStream.WriteAsync(buf, 0, read, token);
                            offset += read;
                        }

                        progressAction.Invoke((int)resContentLength, (int)resContentLength);
                    }
                }
            }
        }



        public static async Task<string> HttpPostStrAsync(string url, string body, CancellationToken token = new CancellationToken())
        {
            using (var httpFilter = new HUwpHttpFilter())
            using (var client = new HttpClient(httpFilter))
            using (var response = await client.PostAsync(new Uri(url), new HttpStringContent(body)))
            {
                response.EnsureSuccessStatusCode();
                var resStr = await response.Content.ReadAsStringAsync();
                return resStr;
            }
        }



        public static async Task<string> HttpPostStreamAsync(string url, StorageFile reqFile, CancellationToken token = new CancellationToken())
        {
            using (var httpFilter = new HUwpHttpFilter())
            using (var client = new HttpClient(httpFilter))
            {
                var form = new HttpMultipartFormDataContent();
                var inStream = await reqFile.OpenAsync(FileAccessMode.Read);
                form.Add(new HttpStreamContent(inStream), "file", reqFile.Name);
                var response = await client.PostAsync(new Uri(url), form);
                response.EnsureSuccessStatusCode();
                var resStr = await response.Content.ReadAsStringAsync();
                return resStr;
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







    public class HUwpHttpFilter : IHttpFilter
    {
        private HttpBaseProtocolFilter _httpFilter = new HttpBaseProtocolFilter();

        public HUwpHttpFilter()
        {
            //_httpFilter.CacheControl.ReadBehavior = HttpCacheReadBehavior.Default;
            _httpFilter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
            //_httpFilter.CacheControl.ReadBehavior = HttpCacheReadBehavior.OnlyFromCache;
            //_httpFilter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.Default;
            _httpFilter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;

            // ---------------------------------------------------------------------------
            // WARNING: Only test applications should ignore SSL errors.
            // In real applications, ignoring server certificate errors can lead to MITM
            // attacks (while the connection is secure, the server is not authenticated).
            //
            // The SetupServer script included with this sample creates a server certificate that is self-signed
            // and issued to fabrikam.com, and hence we need to ignore these errors here. 
            // ---------------------------------------------------------------------------
            _httpFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            _httpFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
        }

        public void Dispose()
        {
            _httpFilter.Dispose();
        }

        public IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> SendRequestAsync(HttpRequestMessage request)
        {
            return AsyncInfo.Run<HttpResponseMessage, HttpProgress>(async (cancellationToken, progress) =>
            {
                Log.http_req(request);
                var response = await _httpFilter.SendRequestAsync(request).AsTask(cancellationToken, progress);
                cancellationToken.ThrowIfCancellationRequested();
                Log.http_res(response);
                return response;
            });
        }
    }











}
