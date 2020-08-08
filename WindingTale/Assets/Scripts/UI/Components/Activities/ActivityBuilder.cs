using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components.Packs;

namespace WindingTale.UI.Components.Activities
{
    public class ActivityBuilder
    {
        public static ActivityBase BuildFromPack(PackBase pack)
        {
            if(pack == null)
            {
                throw new ArgumentNullException("pack");
            }

            switch (pack.Type)
            {
                case PackBase.PackType.ComposeCreature:
                    ComposeCreaturePack composeCreature = pack as ComposeCreaturePack;
                    if (composeCreature != null)
                    {
                        CallbackActivity act = new CallbackActivity(
                            (callback) => { callback.PlaceCreature(composeCreature.Creature); });
                        return act;
                    }
                    break;
                case PackBase.PackType.MoveCreature:

                    return BuildMoveCreature(pack as CreatureMovePack);
                case PackBase.PackType.Batch:
                    BatchActivity batch = new BatchActivity();
                    BatchPack batchPack = pack as BatchPack;
                    if (batchPack != null)
                    {
                        foreach(PackBase p in batchPack.Packs)
                        {
                            if (p == null)
                            {
                                throw new ArgumentNullException("BatchActivity");
                            }

                            var a = BuildFromPack(p);
                            if (a != null)
                            {
                                batch.Add(a);
                            }
                        }
                    }
                    return batch;

                default:
                    break;
            }

            return null;
        }

        private static ActivityBase BuildMoveCreature(CreatureMovePack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException("CreatureMovePack");
            }

            MoveCreatureActivity moveCreature = new MoveCreatureActivity(pack.CreatureId, pack.MovePath);
            return moveCreature;
        }


    }

}
