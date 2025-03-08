using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System;

namespace DrakiaXYZ.BigBrain.Internal
{
    using BaseNodeAbstractClass = GClass170<GClass26>;

    internal class CustomLogicWrapper : BaseNodeAbstractClass
    {
        private CustomLogic customLogic;

        public CustomLogicWrapper(Type logicType, BotOwner bot) : base(bot)
        {
            customLogic = (CustomLogic)Activator.CreateInstance(logicType, new object[] { bot });
        }

        public override void UpdateNodeByBrain(GClass26 data)
        {
            customLogic.Update((CustomLayer.ActionData)data);
        }

        public void Start()
        {
            customLogic.Start();
        }

        public void Stop()
        {
            customLogic.Stop();
        }

        internal CustomLogic CustomLogic()
        {
            return customLogic;
        }
    }
}
