using EFT;
using System.Text;

namespace DrakiaXYZ.BigBrain.Brains
{
    public abstract class CustomLogic<T> where T : CustomLayer.ActionData
    {
        public BotOwner BotOwner { get; private set; }

        public CustomLogic(BotOwner botOwner)
        {
            BotOwner = botOwner;
        }

        public virtual void Start() { }
        public virtual void Stop() { }

        public abstract void Update(T data);

        public virtual void BuildDebugText(StringBuilder stringBuilder) { }
    }

    public abstract class CustomLogic : CustomLogic<CustomLayer.ActionData>
    {
        public CustomLogic(BotOwner botOwner) : base(botOwner) { }
    }
}
