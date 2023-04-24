using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Common
{
    public class ResourceDataFile
    {
        private int index = 0;

        private string[] splittedText = null;

        public ResourceDataFile(string resourcePath)
        {
            TextAsset text = Resources.Load<TextAsset>(resourcePath);

            string textContent = text.text;
            textContent = textContent.Replace("\r\n", " ");
            textContent = textContent.Replace("\t", " ");
            textContent = textContent.Replace("\n", " ");

            if (textContent == null)
            {
                throw new ArgumentNullException(textContent);
            }

            splittedText = textContent.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            index = 0;
        }

        public string ReadString()
        {
            if (index >= splittedText.Length)
            {
                return string.Empty;
            }

            return splittedText[index++];
        }

        public int ReadInt()
        {
            string str = ReadString();
            if (string.IsNullOrEmpty(str))
            {
                return -1;
            }

            int result = 0;
            if(Int32.TryParse(str, out result))
            {
                return result;
            }
            else
            {
                return -1;
            }
        }

        public bool ReadBoolean()
        {
            return (this.ReadInt() == 1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public FDSpan ReadSpan()
        {
            int min = this.ReadInt();
            int max = this.ReadInt();

            return new FDSpan(min, max);
        }
    }

}