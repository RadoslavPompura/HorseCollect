using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace HorseCollect.Ctrl
{
    public class Logger
    {
        public Logger()
        { 
        }

        public void severe(string status)
        {
            try
            {
                string logFilepath = System.Web.Hosting.HostingEnvironment.MapPath("/");

                string logText = (string.Format("[{0}] {1}", Utils.getCurrentUKTime().ToString("yyyy-MM-dd HH:mm:ss"), status));
                LogToFile(logFilepath + "\\Log\\" + string.Format("log_{0}.txt", Utils.getCurrentUKTime().ToString("yyyy-MM-dd")), logText);
                //rtLog.AppendText(logText);
                //rtLog.ScrollToCaret();
            }
            catch (Exception)
            {

            }
        }

        private void LogToFile(string filename, string result)
        {
            try
            {
                if (string.IsNullOrEmpty(filename))
                    return;
                StreamWriter streamWriter = new StreamWriter((Stream)System.IO.File.Open(filename, FileMode.Append, FileAccess.Write, FileShare.Read), Encoding.UTF8);
                if (!string.IsNullOrEmpty(result))
                    streamWriter.WriteLine(result);
                streamWriter.Close();
            }
            catch (System.Exception ex)
            {
            }
        }
    }
}