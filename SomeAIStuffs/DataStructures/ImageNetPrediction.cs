using Microsoft.ML.Data;

namespace SomeAIStuffs.DataStructures
{
    public class ImageNetPrediction
    {
        [ColumnName("grid")]
        public float[] PredictedLabels;
    }
}