using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Common
{
    public class FDMovePath
    {
        public List<FDPosition> Vertexes
        {
            get; private set;
        }

        public FDPosition Desitination
        {
            get
            {
                if (this.Vertexes == null || this.Vertexes.Count == 0)
                {
                    return null;
                }

                return this.Vertexes[this.Vertexes.Count - 1];
            }
        }

        public FDMovePath()
        {
            this.Vertexes = new List<FDPosition>();
        }

        /// <summary>
        /// Single one position for move path
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static FDMovePath Create(FDPosition position)
        {
            FDMovePath movePath = new FDMovePath();
            movePath.Vertexes.Add(position);

            return movePath;
        }

        public static FDMovePath Create(FDPosition pos1, FDPosition pos2)
        {
            FDMovePath movePath = new FDMovePath();
            movePath.Vertexes.Add(pos1);
            movePath.Vertexes.Add(pos2);

            return movePath;
        }
        public static FDMovePath Create(FDPosition pos1, FDPosition pos2, FDPosition pos3)
        {
            FDMovePath movePath = new FDMovePath();
            movePath.Vertexes.Add(pos1);
            movePath.Vertexes.Add(pos2);
            movePath.Vertexes.Add(pos3);
            return movePath;
        }

        public void Push(FDPosition position)
        {
            this.Vertexes.Add(position);
        }

        public void InsertToHead(FDPosition position)
        {
            this.Vertexes.Insert(0, position);
        }

    }
}
