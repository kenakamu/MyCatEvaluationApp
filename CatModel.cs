//
// This is auto-generated model from Visual Studio when importing ONNX model.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.AI.MachineLearning;
using Windows.Storage.Streams;


namespace MyCatEvaluationApp
{

    public sealed class Input
    {
        public ImageFeatureValue data; // BitmapPixelFormat: Bgra8, BitmapAlphaMode: Premultiplied, width: 227, height: 227
    }
    
    public sealed class Output
    {
        public TensorString classLabel; // shape(-1,1)
        public IList<IDictionary<string,float>> loss;
    }
    
    public sealed class Model
    {
        private LearningModel model;
        private LearningModelSession session;
        private LearningModelBinding binding;
        public static async Task<Model> CreateFromStreamAsync(IRandomAccessStreamReference stream)
        {
            Model learningModel = new Model();
            learningModel.model = await LearningModel.LoadFromStreamAsync(stream);
            learningModel.session = new LearningModelSession(learningModel.model);
            learningModel.binding = new LearningModelBinding(learningModel.session);
            return learningModel;
        }
        public async Task<Output> EvaluateAsync(Input input)
        {
            binding.Bind("data", input.data);
            var result = await session.EvaluateAsync(binding, "0");
            var output = new Output();
            output.classLabel = result.Outputs["classLabel"] as TensorString;
            output.loss = result.Outputs["loss"] as IList<IDictionary<string,float>>;
            return output;
        }
    }
}
