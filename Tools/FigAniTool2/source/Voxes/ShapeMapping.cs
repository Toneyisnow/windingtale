using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FigAniTool2.Voxes
{
    public class ShapeMapping
    {
        public Dictionary<string, List<int>> mappings = null;

        public ShapeMapping()
        {

        }

        public bool ContainsValue(int value)
        {
            if (this.mappings == null)
            {
                return false;
            }

            foreach(List<int> values in this.mappings.Values)
            {
                if (values.Contains(value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
