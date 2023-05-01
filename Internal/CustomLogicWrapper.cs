using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrakiaXYZ.BigBrain.Internal
{
    internal class CustomLogicWrapper : GClass104
    {
        private CustomLogic customLogic;

        public CustomLogicWrapper(Type logicType, BotOwner bot) : base(bot)
        {
            customLogic = (CustomLogic)Activator.CreateInstance(logicType, new object[] { bot });
        }

        public override void Update()
        {
            customLogic.Update();
        }

        public void Start()
        {
            customLogic.Start();
        }

        public void Stop()
        {
            customLogic.Stop();
        }
    }
}
