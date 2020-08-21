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

            if (textContent == null)
            {
                throw new ArgumentNullException(textContent);
            }

            splittedText = textContent.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            index = 0;
        }

        public int ReadInt()
        {
            if (index >= splittedText.Length)
            {
                return -1;
            }

            int result = 0;
            if(Int32.TryParse(splittedText[index++], out result))
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

    }

}