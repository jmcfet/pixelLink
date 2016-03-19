using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logging
{
    public class LogFile
    {
        private string fileName;
        private Object thisLock = new Object();
        public LogFile()
        {
            fileName = "C:\\TestLogData\\john.txt";
        }
        public LogFile(string fileName)
        {
            this.fileName = fileName;
        }
        ////Use LogFile to document the test run results
        /// <summary>
        /// The MyLogFile method is used to document details of each test run.
        /// </summary>
        public void MyLogFile(string strCategory, string strMessage)
        {
            lock(thisLock)
            {
                // Store the script names and test results in a output text file.
                using (StreamWriter writer = new StreamWriter(new FileStream(fileName, FileMode.Append)))
                {
                    writer.WriteLine("{0}{1}", strCategory, strMessage);
                }
            }
        }
    }
}
