using SQLite.Net;
using SQLite.Net.Platform.WinRT;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Web.Http;

namespace HUwpBaseLib.Logs
{
    public static class Log
    {
        public static List<LogOutputDirections> _outputDirectionList { get; set; }

        private static BlockingCollection<string> _logCache4File = new BlockingCollection<string>();

        private static BlockingCollection<SqliteLogEntity> _logCache4Sqlite = new BlockingCollection<SqliteLogEntity>();

        public static void Init(List<LogOutputDirections> outputDirectionList)
        {
            _outputDirectionList = outputDirectionList;



            if (_outputDirectionList.Contains(LogOutputDirections.LocalFile))
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        if (_logCache4File.Count == 0)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(10));
                            continue;
                        }

                        var tmpLogList = new List<string>();

                        while (_logCache4File.Count > 0)
                        {
                            var lineLog = _logCache4File.Take();
                            tmpLogList.Add(lineLog);
                        }

                        var localFolder = ApplicationData.Current.LocalFolder;
                        var nowDateStr = DateTime.Now.ToString("yyMMdd");

                        try
                        {
                            var file = await localFolder.CreateFileAsync($"{nowDateStr}.log", CreationCollisionOption.OpenIfExists);
                            await FileIO.AppendLinesAsync(file, tmpLogList);
                        }
                        catch (Exception ex)
                        {
                            Log.e($"Log.Init file writing ... ex : \n{ex}");
                        }
                    }
                });
            }



            if (_outputDirectionList.Contains(LogOutputDirections.LocalSqliteDb))
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        if (_logCache4Sqlite.Count == 0)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(10));
                            continue;
                        }

                        var tmpLogList = new List<SqliteLogEntity>();

                        while (_logCache4Sqlite.Count > 0)
                        {
                            var log = _logCache4Sqlite.Take();
                            tmpLogList.Add(log);
                        }

                        using (var db = new SQLiteConnection(new SQLitePlatformWinRT(), Path.Combine(ApplicationData.Current.LocalFolder.Path, "log.sqlite")))
                        {
                            var tiList = db.GetTableInfo(typeof(SqliteLogEntity).Name);

                            if (!tiList.Any())
                            {
                                db.CreateTable<SqliteLogEntity>();
                            }

                            var list = db.Table<SqliteLogEntity>().Take(10).ToList();

                            db.InsertAll(tmpLogList);
                        }
                    }
                });
            }




        }

        public static void e(string format, params object[] args)
        {
            _WriteLogAsync("ERROR", format, args);
        }

        public static void w(string format, params object[] args)
        {
            _WriteLogAsync("WARNI", format, args);
        }

        public static void i(string format, params object[] args)
        {
            _WriteLogAsync("INFOR", format, args);
        }

        public static void d(string format, params object[] args)
        {
            _WriteLogAsync("DEBUG", format, args);
        }

        public static void v(string format, params object[] args)
        {
            _WriteLogAsync("VERBO", format, args);
        }

        public static void http_req(HttpRequestMessage request, bool showHeader = true, string body = null, int bodyLimitCount = 100, string logLevel = "DEBUG")
        {
            _WriteLogAsync(logLevel, "");
            var reqTopLog = string.Format("HTTP >>> {0} {1}", request.Method, request.RequestUri);
            _WriteLogAsync(logLevel, reqTopLog);

            if (showHeader)
            {
                foreach (var kv in request.Headers)
                {
                    _WriteLogAsync(logLevel, string.Format("HTTP >>> HEADER : {0} : {1}", kv.Key, kv.Value));
                }
            }

            if (!string.IsNullOrEmpty(body))
            {
                _WriteLogAsync(logLevel, string.Format("HTTP >>> BODY LEN : {0}", body.Length));
                _WriteLogAsync(logLevel, string.Format("HTTP >>> BODY : {0} ...", body.Substring(0, bodyLimitCount)));
            }

            _WriteLogAsync(logLevel, "");
        }

        public static void http_res(HttpResponseMessage response, string body = null, int bodyLimitCount = 100, string logLevel = "DEBUG")
        {
            _WriteLogAsync(logLevel, "");
            var resTopLog = string.Format("HTTP <<< {0} {1}", response.RequestMessage.Method, response.RequestMessage.RequestUri);
            _WriteLogAsync(logLevel, resTopLog);

            var resStatusLog = string.Format("HTTP <<< STATUS : {0}({1})",
                (int)response.StatusCode,
                response.StatusCode);
            _WriteLogAsync(logLevel, resStatusLog);

            if (!string.IsNullOrEmpty(body))
            {
                _WriteLogAsync(logLevel, string.Format("HTTP <<< BODY LEN : {0}", body.Length));
                _WriteLogAsync(logLevel, string.Format("HTTP <<< BODY : {0} ...", body.Substring(0, bodyLimitCount)));
            }

            _WriteLogAsync(logLevel, "");
        }

        public static void exception(Exception ex, string logLevel = "WARNI")
        {
            _WriteLogAsync(logLevel, "");
            _WriteLogAsync(logLevel, "");
            _WriteLogAsync(logLevel, "");

            _WriteLogAsync(logLevel, string.Format("EXCEPTION !!! : {0}", ex));

            _WriteLogAsync(logLevel, "");
            _WriteLogAsync(logLevel, "");
            _WriteLogAsync(logLevel, "");
        }

        private static void _WriteLogAsync(string logLevel, string format, params object[] args)
        {
            var log = args.Any() ? string.Format(format, args) : format;
            var now = DateTime.Now;

            var decoLog = string.Format("[{0}][{1}] {2}",
                logLevel,
                now.ToString("yy/MM/dd|HH:mm:ss"),
                log);

            if (_outputDirectionList.Contains(LogOutputDirections.VsConsole))
            {
                Debug.WriteLine(decoLog);
            }

            if (_outputDirectionList.Contains(LogOutputDirections.LocalFile))
            {
                _logCache4File.Add(decoLog);
            }

            if (_outputDirectionList.Contains(LogOutputDirections.LocalSqliteDb))
            {
                _logCache4Sqlite.Add(new SqliteLogEntity()
                {
                    LogLevel = logLevel,
                    Time = now,
                    Log = log,
                });
            }

            if (_outputDirectionList.Contains(LogOutputDirections.AzureTable))
            {

            }
        }
    }
}
