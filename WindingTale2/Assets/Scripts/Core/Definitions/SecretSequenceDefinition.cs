using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Files;

namespace WindingTale.Core.Definitions
{
    /// <summary>
    /// One step of a village secret sequence: which arrow key is pressed. L is the left
    /// button, R the right, the same keys that walk the cursor round the village spots.
    /// </summary>
    public enum SecretOperation
    {
        Left = 0,
        Right = 1,
    }

    /// <summary>
    /// The hidden button sequence that reaches a village's secret spot (pos 5), which the
    /// ordinary left/right walk cannot get to. One per chapter's village: the cursor is
    /// put on StartPosIndex (0-4) and the player enters Operations in order to unlock it.
    ///
    /// Read from Data/SecretSequence.txt, which opens with a record count and then lists
    /// each record as two lines -- the chapter id, then "startPos count op op ...".
    /// </summary>
    public class SecretSequenceDefinition
    {
        /// <summary>The chapter whose village this sequence belongs to.</summary>
        public int ChapterId
        {
            get; set;
        }

        /// <summary>The spot the cursor starts the sequence on, a position index in 0-4.</summary>
        public int StartPosIndex
        {
            get; set;
        }

        /// <summary>The left/right presses to enter, in order.</summary>
        public List<SecretOperation> Operations
        {
            get; set;
        }

        public SecretSequenceDefinition()
        {
            this.Operations = new List<SecretOperation>();
        }

        /// <summary>
        /// Reads one record from the shared token stream: chapter id, start spot, the
        /// operation count, then that many L/R tokens. Returns null when the stream runs
        /// out (chapter id reads back -1), so a caller can loop to the end of the file.
        /// </summary>
        public static SecretSequenceDefinition ReadFromFile(ResourceDataFile reader)
        {
            SecretSequenceDefinition def = new SecretSequenceDefinition();

            def.ChapterId = reader.ReadInt();
            if (def.ChapterId == -1)
            {
                return null;
            }

            def.StartPosIndex = reader.ReadInt();

            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                string op = reader.ReadString();
                def.Operations.Add(op == "L" ? SecretOperation.Left : SecretOperation.Right);
            }

            return def;
        }
    }
}
