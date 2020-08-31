using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Components.Packs
{
    public class PromptPack : PackBase
    {
        public int AnimationId
        {
            get; private set;
        }

        public string Content
        {
            get; private set;
        }

        public PromptPack(int animationId, string content)
        {
            this.Type = PackType.Prompt;

            this.AnimationId = animationId;
            this.Content = content;
        }
    }
}