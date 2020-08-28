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
                    return BuildComposeCreatureActivity(pack as ComposeCreaturePack);

                case PackBase.PackType.MoveCreature:
                    return BuildMoveCreatureActivity(pack as CreatureMovePack);

                case PackBase.PackType.Talk:
                    return BuildTalkActivity(pack as TalkPack);

                case PackBase.PackType.Batch:
                    return BuildBatchActivity(pack as BatchPack);

                case PackBase.PackType.ShowMenu:
                    return BuildShowMenuActivity(pack as ShowMenuPack);

                case PackBase.PackType.CloseMenu:
                    return new CallbackActivity(
                        (callback) => { callback.ClearCancellableObjects(); });

                case PackBase.PackType.ShowRange:
                    return BuildShowRangeActivity(pack as ShowRangePack);

                case PackBase.PackType.ClearRange:
                    return new CallbackActivity(
                        (callback) => { callback.ClearCancellableObjects(); });

                default:
                    break;
            }

            return null;
        }

        private static ActivityBase BuildComposeCreatureActivity(ComposeCreaturePack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException("CreatureMovePack");
            }
            
            CallbackActivity act = new CallbackActivity(
                    (callback) => { callback.PlaceCreature(pack.CreatureId, pack.AnimationId, pack.Position); });
            
            return act;
        }

        private static ActivityBase BuildMoveCreatureActivity(CreatureMovePack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException("CreatureMovePack");
            }

            MoveCreatureActivity moveCreature = new MoveCreatureActivity(pack.CreatureId, pack.MovePath);
            return moveCreature;
        }

        private static ActivityBase BuildTalkActivity(TalkPack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException("pack");
            }

            TalkActivity activity = new TalkActivity(pack.CreatureId, pack.Conversationid);
            return activity;

        }

        private static ActivityBase BuildBatchActivity(BatchPack pack)
        {
            BatchActivity batch = new BatchActivity();
            if (pack != null)
            {
                foreach (PackBase p in pack.Packs)
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
        }

        private static ActivityBase BuildShowMenuActivity(ShowMenuPack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException("pack");
            }

            ShowMenuActivity activity = new ShowMenuActivity(pack);
            return activity;
        }

        private static ActivityBase BuildShowRangeActivity(ShowRangePack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException("pack");
            }

            CallbackActivity activity = new CallbackActivity(
                    (gameInterface) => { gameInterface.PlaceIndicators(pack.Range); });

            return activity;
        }
    }
}
