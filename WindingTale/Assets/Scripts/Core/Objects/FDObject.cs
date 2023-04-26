using WindingTale.Common;

namespace WindingTale.Core.Objects
{
    public enum ObjectType
    {
        Creature = 0,
        Treature = 1,
    }

    public class FDObject
    {
        public int Id
        {
            get; protected set;
        }

        public ObjectType Type { get; protected set; }

        public FDPosition Position { get; set; }

        public FDObject(int id, ObjectType type)
        {
            Id = id;
            Type = type;
        }

        public 
    }
}