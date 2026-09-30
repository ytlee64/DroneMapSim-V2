using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ExcelToCode
{
    public static class ErrorHelper
    {
        public static void ShowFatalAndExit(
            string title,
            string contextMessage,
            Exception? ex = null,
            int exitCode = 1,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFile = "",
            [CallerLineNumber] int callerLine = 0)
        {
            string className;
            try
            {
                var st = new StackTrace(1, true);
                var frame = st.GetFrame(0);
                var method = frame?.GetMethod();
                className = method?.DeclaringType?.FullName ?? Path.GetFileNameWithoutExtension(callerFile);
            }
            catch
            {
                className = Path.GetFileNameWithoutExtension(callerFile);
            }

            string header = $"{className}.{callerMember} ({Path.GetFileName(callerFile)}:{callerLine})";
            string msg = $"{contextMessage}\n\n위치: {header}";
            if (ex != null)
            {
                msg += $"\n\n예외: {ex.GetType().Name}: {ex.Message}\n\n스택:\n{ex.StackTrace}";
            }

            
            Console.Error.WriteLine($"{title}\n{msg}");
            Console.Error.WriteLine("Press Enter to exit...");
            Console.ReadLine();
             
         
            Environment.Exit(exitCode);
        }
    }
}
