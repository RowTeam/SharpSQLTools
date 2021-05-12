using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace SharpSQLTools.FunModule
{
    class ExecOptions
    {
        SqlConnection Conn;
        Setting setting;

        public ExecOptions(SqlConnection Conn, Setting setting)
        {
            this.Conn = Conn;
            this.setting = setting;
        }

        /// <summary>
        /// xp_cmdshell 执行命令
        /// </summary>
        /// <param name="Command">命令</param>
        public void xp_cmdshell(String Command)
        {
            var sqlstr = $@"exec master..xp_cmdshell '{Command}'";
            Console.WriteLine(Batch.RemoteExec(Conn, sqlstr, true));
        }

        /// <summary>
        /// sp_cmdshell 执行命令
        /// </summary>
        /// <param name="Command">命令</param>
        public void sp_cmdshell(String Command)
        {
            if (setting.Check_configuration("Ole Automation Procedures", 0))
            {
                if (setting.Enable_ola()) return;
            }
            var sqlstr = String.Format(@"
                    declare @shell int,@exec int,@text int,@str varchar(8000); 
                    exec sp_oacreate 'wscript.shell',@shell output 
                    exec sp_oamethod @shell,'exec',@exec output,'c:\windows\system32\cmd.exe /c {0}'
                    exec sp_oamethod @exec, 'StdOut', @text out;
                    exec sp_oamethod @text, 'ReadAll', @str out
                    select @str", Command);
            Console.WriteLine(Batch.RemoteExec(Conn, sqlstr, true));
        }
    }
}
