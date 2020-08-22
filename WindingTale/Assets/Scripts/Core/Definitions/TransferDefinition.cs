using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Definitions
{

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
            }

            return def;
        }
    }

    public class TransferDefinition
    {
        public int DefinitionId
        {
            get; set;
        }

        public static TransferDefinition ReadFromFile(ResourceDataFile reader)
        {
            TransferDefinition def = new TransferDefinition();


            return def;
        }
    }




}