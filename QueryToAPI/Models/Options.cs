using System;
using System.Collections.Generic;
using CommandLine;

namespace QueryToAPI.Models
{
    public class Options
    {

        [Option(HelpText = "Print process output to console")]
        public virtual bool Verbose { get; set; }
        [Option('l',"LogFile", Required = false, HelpText = "Set LogFile.")]
        public string LogFile { get; set; }
        [Option('s', "ServerName", Required = false, HelpText = "Set Server Name.")]
        public string ServerName { get; set; }
        [Option('d', "DBName", Required = false, HelpText = "Set DB Name QueryToAPI.")]
        public string DBName { get; set; }
        [Option("TableName", Required = false, HelpText = "Set Table Name QueryToAPI.")]
        public virtual string TableName { get; set; }
        [Option('q', "QueryFile", Required = false, HelpText = "Set Path Query File.")]
        public string QueryFile { get; set; }
        [Option('h', "HeaderKey", Required = false, HelpText = "Set Key of Header from API.")]
        public virtual string HeaderKey { get; set; }
        [Option('u', "Url", Required = false, HelpText = "Set url API.")]
        public string URL { get; set; }
        [Option( "ResponseServer", Required = false, HelpText = "Select Server to catch response from API.")]
        public string ResponseServer { get; set; }
        [Option("ResponseDB", Required = false, HelpText = "Select database to catch response from API.")]
        public string ResponseDB { get; set; }
        [Option("ResponseTable", Required = false, HelpText = "Select table to catch response from API.")]
        public string ResponseTable { get; set; }
        [Option("DBList", Default = "DBList.txt", HelpText = "Full path configuration file for Database Connection List. eg: E:\\QueryToAPI\\DBList.txt")]
        public virtual string DBList { get; set; }
        [Option("DBListFilters", HelpText = "filter connection string from DB List to be executed. Eg: AMI JKT")]
        public virtual IEnumerable<string> DBListFilters { get; set; }

        [Option("JSONTextToReplace", HelpText = "replace string on query, usually use for specific filter on query. eg: ParamAreaCode=JKT ParamSalesPoint=SSLI")]
        public virtual IEnumerable<string> JSONTextToReplace { get; set; }
        [Option("JSONTemplate",  HelpText = "Full path configuration file for Database Connection List. eg: E:\\QueryToAPI\\DBList.txt")]
        public virtual string JSONTemplate { get; set; }


    }
}
