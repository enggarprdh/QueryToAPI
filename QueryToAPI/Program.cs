using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using QueryToAPI.Models;
using NLog;
using NLog.Layouts;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Data;
using QueryToAPI.Services;

namespace QueryToAPI
{
    class Program
    {
        private static Logger logger;
        private static Stopwatch _watch;
        private static string ExePath;
        private static string RootPath;
        private static string QueryPath;
        private static string LogPath;
        private static Service Service;
        static void Main(string[] args)
        {
            ExePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            RootPath = Path.Combine(ExePath, "..");
            QueryPath = Path.Combine(RootPath, "Query");
            LogPath = Path.Combine(RootPath, "Logs");
            Service = new Service();

            logger = new Logger("log");
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(Process)
                .WithNotParsed(HandleParseError);
        }

        static void Process(Options opt)
        {
            try
            {

                LoggerConfigure(opt);

                if (string.IsNullOrEmpty(opt.URL))
                    throw new Exception("url API not set");
                if (string.IsNullOrEmpty(opt.QueryFile))
                    throw new Exception($"Query file not found : {opt.QueryFile}");
                if (string.IsNullOrEmpty(opt.DBList))
                    throw new Exception($"DBList file not found : {opt.DBList}");


                string queryStr = string.Empty;
                using (var sr = new StreamReader(opt.QueryFile, true))
                    queryStr = sr.ReadToEnd();

                IEnumerable<string> tmp_ConnectionSrings = File.ReadLines(opt.DBList, Encoding.Default);
                List<string> ConnectionSrings = new List<string>();

                List<string> TmpFiles = new List<string>();

                if (opt.DBListFilters.Count() > 0)
                    ConnectionSrings = EnumFiltered(tmp_ConnectionSrings, opt.DBListFilters);
                else
                    ConnectionSrings = tmp_ConnectionSrings.ToList();

                Parallel.ForEach(ConnectionSrings, (connstr) =>
                {

                    using (SqlManager sqlman = new SqlManager(connstr, logger))
                    {
                        try
                        {
                            var data = sqlman.SqlToDataTable(queryStr, connstr);
                            if (data != null && data.Rows.Count > 0)
                            {

                                for (var i = 0; i < data.Rows.Count; i++)
                                {
                                    var jsonTemplate = string.Empty;
                                    using (var jsonReader = new StreamReader(opt.JSONTemplate, true))
                                        jsonTemplate = jsonReader.ReadToEnd();

                                    if (string.IsNullOrEmpty(jsonTemplate))
                                        throw new Exception($"JSON Template not found");

                                    foreach (string qtr in opt.JSONTextToReplace)
                                    {
                                        //skip if wrong format 
                                        if (!qtr.Contains("="))
                                            continue;

                                        string lefttext = qtr.Split('=')[0].ToString();
                                        string righttext = qtr.Split('=')[1].ToString();
                                        var dataValue = data.Rows[i][lefttext].ToString();
                                        if (lefttext.Length > 0 && righttext.Length > 0)
                                            jsonTemplate = jsonTemplate.Replace(lefttext, dataValue);

                                    }

                                    var headerStr = string.Empty;
                                    using (var sr = new StreamReader(opt.HeaderKey, true))
                                        headerStr = sr.ReadToEnd();

                                    var headers = new List<Header>();
                                    var headerStrList = headerStr.Split('\n');
                                    foreach (var head in headerStrList)
                                    {
                                        if (!head.Contains("="))
                                            continue;
                                        var header = new Header();

                                        var key = head.Split('=')[0].ToString().Trim();
                                        var value = head.Split('=')[1].ToString().Trim();
                                        header.Name = key;
                                        header.Value = value;
                                        headers.Add(header);
                                    }

                                    var url = opt.URL.Split('/');
                                    var action = url[url.Length - 1];
                                    var baseUrl = opt.URL.Replace(action, string.Empty);
                                    var response = Service.POST(baseUrl, action, jsonTemplate, headers);
                                    var tableName = opt.ResponseTable;
                                    var serverName = opt.ResponseServer;
                                    var dbName = opt.ResponseDB;
                                    var connRespon = $"Data Source={serverName};Initial Catalog={dbName};Integrated Security=True;Connection Timeout=30;";
                                    var qryCheckTable = @"	IF OBJECT_ID('{0}') IS NULL 
                                                     BEGIN
                                                        CREATE TABLE[dbo].[{0}](

                                                            [ID][uniqueidentifier] NULL,
				                                            [URL] [varchar](50) NULL,
                                                            [RequestBody] [varchar](max) NULL,
				                                            [ResponseText] [varchar](max) NULL,
				                                            [ModDate] [datetime] NULL,

                                                            ) ON[PRIMARY]

                                                  END";
                                    qryCheckTable = string.Format(qryCheckTable, tableName);

                                    using (SqlManager sqlRes = new SqlManager(connRespon, logger))
                                    {
                                        try
                                        {
                                            sqlRes.CreateTable(connRespon, qryCheckTable);
                                            sqlRes.CreateLog(connRespon, response, opt.URL, tableName, jsonTemplate);
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.Error($"Export Failed : {connRespon}", ex);
                                        }
                                    }

                                    logger.Debug($"Export Success : {connstr}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Export Failed : {connstr}", ex);
                        }

                    }


                });
            }
            catch (Exception ex)
            {
                logger.Debug($"{ex.Message}");
            }

        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
            //handle errors
            foreach (var err in errs)
            {
                logger.Error($"Error : {err.Tag}");
            }
        }

        static List<string> EnumFiltered(IEnumerable<string> lines, IEnumerable<string> filters)
        {

            return filters.AsParallel()
                   .SelectMany(searchPattern =>
                          lines.Where(x => x.Contains(searchPattern))
                          ).ToList();

        }

        static void LoggerConfigure(Options opts)
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile");
            if (opts.LogFile != null)
            {
                if (Path.GetFileName(opts.LogFile) == opts.LogFile)
                    logfile.FileName = $"{Path.Combine(Path.Combine(RootPath, "Logs"), opts.LogFile)}";
                else
                    logfile.FileName = $"{opts.LogFile}";
            }
            else
                logfile.FileName = $"{Path.Combine(Path.Combine(RootPath, "Logs"), $"{DateTime.Now.ToString("yyyyMMdd")}.log")}";

            logfile.MaxArchiveFiles = 60;
            logfile.ArchiveAboveSize = 10240000;

            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
            if (opts.Verbose)
                config.AddRule(LogLevel.Trace, LogLevel.Fatal, logconsole);
            else
                config.AddRule(LogLevel.Error, LogLevel.Fatal, logconsole);

            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile);

            // design layout for file log rotation
            CsvLayout layout = new CsvLayout();
            layout.Delimiter = CsvColumnDelimiterMode.Comma;
            layout.Quoting = CsvQuotingMode.Auto;
            layout.Columns.Add(new CsvColumn("Start Time", "${longdate}"));
            layout.Columns.Add(new CsvColumn("Elapsed Time", "${elapsed-time}"));
            layout.Columns.Add(new CsvColumn("Machine Name", "${machinename}"));
            layout.Columns.Add(new CsvColumn("Login", "${windows-identity}"));
            layout.Columns.Add(new CsvColumn("Level", "${uppercase:${level}}"));
            layout.Columns.Add(new CsvColumn("Message", "${message}"));
            layout.Columns.Add(new CsvColumn("Exception", "${exception:format=toString}"));
            logfile.Layout = layout;

            // design layout for console log rotation
            SimpleLayout ConsoleLayout = new SimpleLayout("${longdate}:${message}\n${exception}");
            logconsole.Layout = ConsoleLayout;

            // Apply config           
            NLog.LogManager.Configuration = config;
        }
    }
}
