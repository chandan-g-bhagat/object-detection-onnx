using System.Text;
using System.Threading.Tasks;

namespace SomeAIStuffs.YoloParser
{
    public class DimensionsBase
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Height { get; set; }
        public float Width { get; set; }
    }

    public class BoundingBoxDimensions : DimensionsBase
    { }
}