using SharpSQLTools.Domain;
using SharpSQLTools.FunModule;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;

namespace SharpSQLTools
{
    class Program
    {
        public static void OnInfoMessage(object mySender, SqlInfoMessageEventArgs args)
        {
            var value = String.Empty;
            foreach (SqlError err in args.Errors)
            {
                value = err.Message;
                Console.WriteLine(value);
            }
        }

        /// <summary>
        /// 数据库连接
        /// </summary>
        static SqlConnection SqlConnet(string target, string username, string password)
        {
            SqlConnection Conn = null;
            var connectionString = $"Server = \"{target}\";Database = \"master\";User ID = \"{username}\";Password = \"{password}\";";
            try
            {
                Conn = new SqlConnection(connectionString);
                Conn.InfoMessage += new SqlInfoMessageEventHandler(OnInfoMessage);
                Conn.Open();
                Console.WriteLine("[*] Database connection is successful!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[!] Error log: \r\n" + ex.Message);
                Environment.Exit(0);
            }
            return Conn;
        }

        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Info.ShowUsage();                
                return;
            }

            var Conn = SqlConnet(args[0], args[1], args[2]);
            var setting = new Setting(Conn);
            var filesOptions = new FilesOptions(Conn, setting);
            var execOptions = new ExecOptions(Conn, setting);

            try
            {
                do
                {
                    Console.Write("SQL> ");
                    string str = Console.ReadLine();
                    if (str.ToLower() == "exit") { Conn.Close(); break; }
                    else if (str.ToLower() == "help") { Info.ShowModuleUsage(); continue; }
                    
                    string[] cmdline = str.Split(new char[] { ' ' }, 3);
                    String s = String.Empty;
                    for (int i = 1; i < cmdline.Length; i++) { s += cmdline[i] + " "; }

                    switch (cmdline[0].ToLower())
                    {
                        case "enable_xp_cmdshell":
                            setting.Enable_xp_cmdshell();
                            break;
                        case "disable_xp_cmdshell":
                            setting.Disable_xp_cmdshell();
                            break;
                        case "xp_cmdshell":
                            execOptions.xp_cmdshell(s);
                            break;
                        case "enable_ole":
                            setting.Enable_ola();
                            break;
                        case "disable_ole":
                            setting.Disable_ole();
                            break;
                        case "sp_cmdshell":
                            execOptions.sp_cmdshell(s);
                            break;
                        case "upload":
                            filesOptions.UploadFiles(cmdline[1], cmdline[2]);
                            break;
                        case "download":
                            filesOptions.DownloadFiles(cmdline[2], cmdline[1]);
                            break;
                        default:
                            Console.WriteLine(Batch.RemoteExec(Conn, str, true));
                            break;
                    }
                    if (!ConnectionState.Open.Equals(Conn.State))
                    {
                        Console.WriteLine("[!] Disconnect....");
                        break;
                    }
                }
                while (true);
            }
            catch (Exception ex)
            {
                Conn.Close();
                Console.WriteLine("[!] Error log: \r\n" + ex.Message);
            }
        }
    }
}
