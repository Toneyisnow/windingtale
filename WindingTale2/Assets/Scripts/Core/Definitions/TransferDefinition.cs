using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Files;

namespace WindingTale.Core.Definitions
{

    /// <summary>
    /// Every career change open to one creature definition: a block of Data/Transfer, which
    /// reads "<from definition> <count>" and then one TransferDefinition row per change.
    /// </summary>
    public class TransfersDefinition
    {
        public int DefinitionId
        {
            get; private set;
        }

        public List<TransferDefinition> Transfers
        {
            get; private set;
        }

        public static TransfersDefinition ReadFromFile(ResourceDataFile reader)
        {
            TransfersDefinition def = new TransfersDefinition();

            def.DefinitionId = reader.ReadInt();
            def.Transfers = new List<TransferDefinition>();

            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                TransferDefinition transfer = TransferDefinition.ReadFromFile(reader);
                transfer.DefinitionId = def.DefinitionId;
                def.Transfers.Add(transfer);
            }

            return def;
        }
    }

    /// <summary>
    /// One career change: "<to definition> <required item, 0 for none>" and then the six
    /// ranges (AP DP DX HP MP MV) whose rolls are added to the creature's stats, as the
    /// original's TransferDefinition reads it. DefinitionId -- the creature changing -- comes
    /// from the enclosing TransfersDefinition block, not from the row.
    /// </summary>
    public class TransferDefinition
    {
        public int DefinitionId
        {
            get; set;
        }

        public int ToDefinitionId
        {
            get; private set;
        }

        public int RequireItemId
        {
            get; private set;
        }

        public FDSpan ApRange
        {
            get; private set;
        }

        public FDSpan DpRange
        {
            get; private set;
        }
        public FDSpan DxRange
        {
            get; private set;
        }

        public FDSpan HpRange
        {
            get; private set;
        }

        public FDSpan MpRange
        {
            get; private set;
        }

        public FDSpan MvRange
        {
            get; private set;
        }

        public static TransferDefinition ReadFromFile(ResourceDataFile reader)
        {
            TransferDefinition def = new TransferDefinition();

            def.ToDefinitionId = reader.ReadInt();
            def.RequireItemId = reader.ReadInt();
            def.ApRange = reader.ReadSpan();
            def.DpRange = reader.ReadSpan();
            def.DxRange = reader.ReadSpan();
            def.HpRange = reader.ReadSpan();
            def.MpRange = reader.ReadSpan();
            def.MvRange = reader.ReadSpan();

            return def;
        }
    }
}