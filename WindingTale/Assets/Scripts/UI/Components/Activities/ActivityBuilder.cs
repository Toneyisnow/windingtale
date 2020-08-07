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
                    break;
                case PackBase.PackType.Batch:
                    break;

                default:
                    break;
            }

            return null;
        }
    }

}
