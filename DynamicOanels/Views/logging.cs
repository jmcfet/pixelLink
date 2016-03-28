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
            fileName = "C:\\PixelLink\\log.txt";
        }
        public LogFile(string fileName)
        {
            this.fileName = fileName;
        }
        ////Use LogFile to document the test run results
        /// <summaryfileName
        /// The MyLogFile method is used to document details of each test run.
        /// </summary>
        public void MyLogFile(string strCategory, string strMessage)
        {
            lock(thisLock)
            {
                if (!File.Exists(fileName))
                {
                    using (FileStream fs = File.Create(fileName))
                    {

                    }
                }

                // Store the script names and test results in a output text file.
                using (StreamWriter writer = new StreamWriter(new FileStream(fileName, FileMode.Append)))
                {
                    writer.WriteLine("{0}{1}", strCategory, strMessage);
                }
            }
        }
    }
}
