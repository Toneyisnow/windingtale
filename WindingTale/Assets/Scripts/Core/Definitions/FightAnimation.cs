using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindingTale.Common;

namespace WindingTale.Core.Definitions
{
    public class FightAnimation
    {
        public int AnimationId
        {
            get; private set;
        }

        public bool HasSkillAnimation
        {
            get; private set;
        }

        public int IdleFrameCount
        {
            get; private set;
        }

        public int AttackFrameCount
        {
            get; private set;
        }

        public int SkillFrameCount
        {
            get; private set;
        }

        public Dictionary<int, int> AttackPercentageByFrame
        {
            get; private set;
        }

        public int RemoteAttackFrame
        {
            get; private set;
        }


        public static FightAnimation ReadFromFile(ResourceDataFile reader)
        {
            FightAnimation definition = new FightAnimation();

            definition.AnimationId = reader.ReadInt();
            if (definition.AnimationId < 0)
            {
                return null;
            }

            reader.ReadInt();
            definition.IdleFrameCount = reader.ReadInt();
            definition.AttackFrameCount = reader.ReadInt();

            definition.AttackPercentageByFrame = new Dictionary<int, int>();
            for (int i = 0; i < definition.AttackFrameCount; i++)
            {
                int val = reader.ReadInt();
                if (val > 0)
                {
                    definition.AttackPercentageByFrame[i] = val;
                }
            }
            definition.RemoteAttackFrame = reader.ReadInt();

            return definition;
        }


    }
}
