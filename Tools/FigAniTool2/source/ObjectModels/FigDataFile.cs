using FigAniTool2.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FigAniTool2.ObjectModels
{
    public class FigDataFile
    {
        private string fileFullName = null;

        private List<FigAnimation> Animations = null;


        public FigDataFile(string fileFullName)
        {
            this.fileFullName = fileFullName;
        }

        public FigAnimation GetAnimation(int index)
        {
            if (this.Animations == null || index < 0 || index >= this.Animations.Count)
            {
                return null;
            }

            return this.Animations[index];
        }


        public void LoadData()
        {
            this.Animations = new List<FigAnimation>();
            using (FileStream streamData = new FileStream(fileFullName, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(streamData, Encoding.UTF8))
                {
                    reader.Skip(6);

                    int aniCount = 1;
                    int address0 = reader.ReadInt32();
                    List<int> aniAddresses = new List<int>();
                    aniAddresses.Add(address0);
                    while (reader.BaseStream.Position < address0)
                    {
                        aniAddresses.Add(reader.ReadInt32());
                        aniCount++;
                    }

                    for (int aniIndex = 0; aniIndex < aniCount - 1; aniIndex++)
                    {
                        if (aniAddresses[aniIndex] >= reader.BaseStream.Length)
                        {
                            break;
                        }

                        reader.Seek(aniAddresses[aniIndex], SeekOrigin.Begin);

                        // Read Animation
                        FigAnimation animation = new FigAnimation();
                        int frameCount = reader.ReadByte();

                        bool hasRemoteFrame = reader.ReadBoolean();
                        if (hasRemoteFrame)
                        {
                            animation.RemoteFrameIndex = reader.ReadByte();
                        }
                        else
                        {
                            animation.RemoteFrameIndex = -1;
                            reader.ReadByte();
                        }

                        if (reader.BaseStream.Position >= reader.BaseStream.Length)
                        {
                            break;
                        }

                        reader.ReadByte();
                        reader.ReadInt32();
                        List<int> frameAddresses = new List<int>(frameCount);
                        for (int f = 0; f < frameCount; f++)
                        {
                            frameAddresses.Add(reader.ReadInt32());
                        }

                        for (int f = 0; f < frameCount; f++)
                        {
                            reader.Seek(aniAddresses[aniIndex] + frameAddresses[f]);

                            FigFrame frame = new FigFrame();

                            frame.PositionX = reader.ReadInt16();
                            frame.PositionY = reader.ReadInt16();

                            frame.IsHit = reader.ReadBoolean();
                            frame.EffectAudioId = reader.ReadByte();
                            frame.Latency = reader.ReadByte();

                            reader.ReadInt16();

                            frame.Image = FDImage.ReadFromBinary(reader);

                            string characterId = (aniIndex / 3).ToString("X2");
                            int type = aniIndex % 3;

                            animation.Frames.Add(frame);
                        }

                        this.Animations.Add(animation);
                    }
                }
            }
        }
    }
}
