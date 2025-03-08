using EFT;
using System;
using System.Text;

namespace DrakiaXYZ.BigBrain.Brains
{
    public abstract class CustomLayer
    {
        public BotOwner BotOwner { get; private set; }
        public int Priority { get; private set; }
        public Action CurrentAction { get; set; } = null;

        public CustomLayer(BotOwner botOwner, int priority) 
        {
            BotOwner = botOwner;
            Priority = priority;
        }

        public abstract string GetName();
        public abstract bool IsActive();
        public abstract Action GetNextAction();
        public abstract bool IsCurrentActionEnding();

        public virtual void Start() { }
        public virtual void Stop() { }

        public virtual void BuildDebugText(StringBuilder stringBuilder) { }
        
        public class Action
        {
            public Type Type { get; set; }
            public string Reason { get; set; }
            public ActionData Data { get; set; }

            public Action(Type logicType, string reason, ActionData data = null)
            {
                Type = logicType;
                Reason = reason;
                Data = data;
            }
        }

        public class ActionData : GClass26
        {

        }
    }
}
