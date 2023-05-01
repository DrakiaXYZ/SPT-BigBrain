using EFT;

namespace DrakiaXYZ.BigBrain.Brains
{
    public abstract class CustomLogic
    {
        public BotOwner BotOwner { get; private set; }

        public CustomLogic(BotOwner botOwner)
        {
            BotOwner = botOwner;
        }

        public virtual void Start() { }
        public virtual void Stop() { }

        public abstract void Update();
    }
}
