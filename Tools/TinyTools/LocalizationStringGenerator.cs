﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyTools
{
    public class LocalizationStringGenerator
    {
        private string originalStringsFolder = @"..\..\..\..\Resources\Original\Strings\";



        private string outputStringsFolder = @"..\..\..\..\WindingTale\Assets\SmartLocalization\Workspace\AutoGenerated\Resources";

        private string outputRootFile = @"Language.txt";

        private string outputChineseFile = @"Language.zh-CN.txt";

        public void Generate()
        {
            // Copy the header file
            using (StreamWriter writer = new StreamWriter(Path.Combine(outputStringsFolder, outputRootFile)))
            using (StreamWriter writer2 = new StreamWriter(Path.Combine(outputStringsFolder, outputChineseFile)))
            {
                using (StreamReader reader = new StreamReader(".\\LocalizationFileHeader.xml"))
                {
                    string content = reader.ReadToEnd();
                    writer.Write(content);
                    writer2.Write(content);

                    writer.WriteLine("");
                    writer2.WriteLine("");

                }
            }

            using (StreamWriter writer = new StreamWriter(Path.Combine(outputStringsFolder, outputRootFile), true))
            using (StreamWriter writer2 = new StreamWriter(Path.Combine(outputStringsFolder, outputChineseFile), true))
            {
                foreach (string filename in Directory.GetFiles(originalStringsFolder, "*.strings", SearchOption.AllDirectories))
                {
                    int start = filename.LastIndexOf(@"\");
                    int end = filename.LastIndexOf(@".");
                    string singlefilename = filename.Substring(start + 1, end - start - 1);

                    using (StreamWriter writersingle = new StreamWriter(Path.Combine(outputStringsFolder, "CharacterList_" + singlefilename + ".txt"), false))
                    {
                        using (StreamReader reader = new StreamReader(filename))
                        {
                            while (!reader.EndOfStream)
                            {
                                string content = reader.ReadLine();
                                if (!content.Contains("="))
                                {
                                    continue;
                                }

                                string[] sps = content.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                                string key = sps[0].Trim().Replace("\"", "");
                                string value = sps[1].Trim();
                                value = value.Substring(0, value.Length - 1).Replace("\"", "");

                                key = singlefilename + "-" + key;
                                writer.WriteLine(string.Format("    <data name=\"{0}\" xml:space=\"preserve\">", key));
                                writer2.WriteLine(string.Format("    <data name=\"{0}\" xml:space=\"preserve\">", key));

                                writer.WriteLine("      <value></value>");
                                writer2.WriteLine(string.Format("      <value>{0}</value>", value));

                                writer.WriteLine("    </data>");
                                writer2.WriteLine("    </data>");

                                writersingle.WriteLine(value);
                            }
                        }
                    }
                }

                writer.WriteLine(@"</root>");
                writer2.WriteLine(@"</root>");
            }
        }

    }
}