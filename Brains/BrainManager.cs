using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrakiaXYZ.BigBrain.Brains
{
    public class BrainManager
    {
        public const int START_LAYER_ID = 9000;
        public const int START_LOGIC_ID = 9000;

        private static BrainManager _instance;
        internal static BrainManager Instance
        {
            get 
            { 
                if (_instance == null)
                {
                    _instance = new BrainManager();
                }

                return _instance; 
            }
        }

        private static int _currentLayerId = START_LAYER_ID;

        internal Dictionary<int, LayerInfo> CustomLayers = new Dictionary<int, LayerInfo>();
        internal Dictionary<Type, int> CustomLogics = new Dictionary<Type, int>();
        internal List<Type> CustomLogicList = new List<Type>();
        internal List<ExcludeLayerInfo> ExcludeLayers = new List<ExcludeLayerInfo>();

        // Hide the constructor so we can have this as a guaranteed singleton
        private BrainManager() { }

        internal class LayerInfo
        {
            public Type customLayerType;
            public List<string> customLayerBrains;
            public int customLayerPriority;
            public int customLayerId;

            public LayerInfo(Type layerType, List<string> layerBrains, int layerPriority, int layerId)
            {
                customLayerType = layerType;
                customLayerBrains = layerBrains;
                customLayerPriority = layerPriority;
                customLayerId = layerId;
            }
        }

        internal class ExcludeLayerInfo
        {
            public List<string> excludeLayerBrains;
            public string excludeLayerName;

            public ExcludeLayerInfo(string layerName, List<string> brains)
            {
                excludeLayerBrains = brains;
                excludeLayerName = layerName;
            }
        }

        public static int AddCustomLayer(Type customLayerType, List<string> customLayerBrains, int customLayerPriority)
        {
            if (!typeof(CustomLayer).IsAssignableFrom(customLayerType))
            {
                throw new ArgumentException($"Custom layer type {customLayerType.FullName} must inherit CustomLayer");
            }

            if (customLayerBrains.Count == 0)
            {
                throw new ArgumentException($"Custom layer type {customLayerType.FullName} must specify at least 1 brain to be added to");
            }

            int customLayerId = _currentLayerId++;
            Instance.CustomLayers.Add(customLayerId, new LayerInfo(customLayerType, customLayerBrains, customLayerPriority, customLayerId));
            return customLayerId;
        }

        public static void RemoveLayer(string layerName, List<string> brainNames)
        {
            Instance.ExcludeLayers.Add(new ExcludeLayerInfo(layerName, brainNames));
        }
    }
}
