using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components.Data;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.ObjectModels;
using WindingTale.UI.Dialogs;

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

                case PackBase.PackType.RefreshCreature:
                    return BuildRefreshCreatureActivity(pack as RefreshCreaturePack);

                case PackBase.PackType.RefreshAllCreatures:
                    return BuildRefreshAllCreaturesActivity(pack as RefreshAllCreaturesPack);

                case PackBase.PackType.ShowCreatureInfo:
                    return BuildShowCreatureInfoActivity(pack as ShowCreatureInfoPack);

                case PackBase.PackType.Talk:
                    return BuildTalkActivity(pack as TalkPack);

                case PackBase.PackType.Prompt:
                    return BuildPromptActivity(pack as PromptPack);

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

        private static ActivityBase BuildRefreshCreatureActivity(RefreshCreaturePack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException("RefreshCreaturePack");
            }

            CallbackActivity activity = new CallbackActivity(
                    (gameInterface) => { gameInterface.RefreshCreature(pack.Creature); });

            return activity;
        }

        private static ActivityBase BuildRefreshAllCreaturesActivity(RefreshAllCreaturesPack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException("RefreshCreaturePack");
            }

            CallbackActivity activity = new CallbackActivity(
                    (gameInterface) => { gameInterface.RefreshAllCreatures(); });

            return activity;
        }

        private static ActivityBase BuildTalkActivity(TalkPack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException("pack");
            }

            TalkActivity activity;
            if (pack.ConversationId != null)
            {
                activity = new TalkActivity(pack.Creature, pack.ConversationId);
            }
            else
            {
                activity = new TalkActivity(pack.Creature, pack.MessageId);
            }

            return activity;
        }

        private static ActivityBase BuildPromptActivity(PromptPack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException("pack");
            }

            CallbackActivity activity = new CallbackActivity((gameInterface) =>
            {
                /// gameInterface.ShowPromptDialog(pack.AnimationId, pack.Content);
            });

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

        private static ActivityBase BuildShowCreatureInfoActivity(ShowCreatureInfoPack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException("pack");
            }

            FDCreature creature = pack.Creature;

            CreatureDialog.ShowType showType = CreatureDialog.ShowType.SelectAllItem;
            switch(pack.InfoType)
            {
                case CreatureInfoType.SelectAllItem:
                    showType = CreatureDialog.ShowType.SelectAllItem;
                    break;
                case CreatureInfoType.SelectEquipItem:
                    showType = CreatureDialog.ShowType.SelectEquipItem;
                    break;
                case CreatureInfoType.SelectUseItem:
                    showType = CreatureDialog.ShowType.SelectUseItem;
                    break;
                case CreatureInfoType.SelectMagic:
                    showType = CreatureDialog.ShowType.SelectMagic;
                    break;
                case CreatureInfoType.View:
                    showType = CreatureDialog.ShowType.ViewItem;
                    break;
                default:
                    showType = CreatureDialog.ShowType.SelectAllItem;
                    break;
            }

            CallbackActivity activity = new CallbackActivity(
                    (gameInterface) => { gameInterface.ShowCreatureDialog(pack.Creature, showType); });

            if (pack.InfoType == CreatureInfoType.View && creature.Data.HasMagic())
            {
                SequenceActivity sequenceActivity = new SequenceActivity();
                sequenceActivity.Add(activity);

                CallbackActivity activity2 = new CallbackActivity(
                    (gameInterface) => { gameInterface.ShowCreatureDialog(pack.Creature, CreatureDialog.ShowType.ViewMagic); });

                sequenceActivity.Add(activity2);
                return sequenceActivity;
            }

            return activity;
        }
    }
}
