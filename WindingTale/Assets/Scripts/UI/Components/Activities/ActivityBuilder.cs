using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components.Algorithms;
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
                case PackBase.PackType.CreatureCompose:
                    return BuildComposeCreatureActivity(pack as CreatureComposePack);

                case PackBase.PackType.CreatureMove:
                    return BuildMoveCreatureActivity(pack as CreatureMovePack);

                case PackBase.PackType.CreatureRefresh:
                    return BuildRefreshCreatureActivity(pack as CreatureRefreshPack);

                case PackBase.PackType.CreatureDead:
                    return BuildCreatureDeadActivity(pack as CreatureDeadPack);

                case PackBase.PackType.CreatureDispose:
                    return BuildCreatureDisposeActivity(pack as CreatureDisposePack);

                case PackBase.PackType.CreatureRefreshAll:
                    return BuildRefreshAllCreaturesActivity(pack as CreatureRefreshAllPack);

                case PackBase.PackType.CreatureShowInfo:
                    return BuildShowCreatureInfoActivity(pack as CreatureShowInfoPack);

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

                case PackBase.PackType.BattleFight:
                    return BuildBattleFightActivity(pack as BattleFightPack);

                default:
                    break;
            }

            return null;
        }

        private static ActivityBase BuildComposeCreatureActivity(CreatureComposePack pack)
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

            CreatureMoveActivity moveCreature = new CreatureMoveActivity(pack.CreatureId, pack.MovePath);
            return moveCreature;
        }

        private static ActivityBase BuildRefreshCreatureActivity(CreatureRefreshPack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException("RefreshCreaturePack");
            }

            CallbackActivity activity = new CallbackActivity(
                    (gameInterface) => { gameInterface.RefreshCreature(pack.Creature); });

            return activity;
        }

        private static ActivityBase BuildCreatureDeadActivity(CreatureDeadPack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException("CreatureDeadPack");
            }

            CreatureDeadActivity activity = new CreatureDeadActivity(pack.CreatureIds);
            return activity;
        }

        private static ActivityBase BuildCreatureDisposeActivity(CreatureDisposePack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException("CreatureDeadPack");
            }

            CallbackActivity activity = new CallbackActivity(
                    (gameInterface) => { gameInterface.DisposeCreature(pack.CreatureId); });

            return activity;
        }

        private static ActivityBase BuildRefreshAllCreaturesActivity(CreatureRefreshAllPack pack)
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

        private static ActivityBase BuildShowCreatureInfoActivity(CreatureShowInfoPack pack)
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

        private static CallbackActivity BuildBattleFightActivity(BattleFightPack fightPack)
        {
            if (fightPack == null)
            {
                throw new ArgumentNullException("fightPack");
            }

            FDCreature subject = fightPack.Subject;
            FDCreature target = fightPack.Target;
            FightInformation fightInfo = fightPack.FightInformation;

            CallbackActivity activity = new CallbackActivity(
                    (gameInterface) => { gameInterface.BattleFight(subject, target, fightInfo); });

            return activity;
        }
    }
}
