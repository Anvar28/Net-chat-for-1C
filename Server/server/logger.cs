using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace logers
{
    public interface ILoger
    {
        void Write(string str);
    }

    // Логер в консоль
    class logConsol: ILoger
    {
        public void Write(string str)
        {
            Console.WriteLine(DateTime.Now.ToString() + " " + str);
        }
    }

    // Логер в файл
    class logFile : ILoger
    {
        private string path;

        public logFile(string lPath)
        {
            path = lPath;
        }

        public void Write(string str)
        {
            using (StreamWriter sw = new StreamWriter(path, true, System.Text.Encoding.Default))
            {
                sw.WriteLine(DateTime.Now.ToString() + " " + str);
            }
        }
    }

    // Логер на базе делегата 
    delegate void logDelegat(string str);
    class logCallback : ILoger
    {
        private logDelegat callback;

        public logCallback(logDelegat lCallback)
        {
            callback = lCallback;
        }

        public void Write(string str)
        {
            callback(str);
        }
    }
}
