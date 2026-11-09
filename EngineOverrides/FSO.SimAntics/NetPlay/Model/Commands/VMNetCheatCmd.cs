using FSO.SimAntics.Utils;
using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    /// <summary>
    /// A command submitting a cheat with a typed parameter T
    /// </summary>
    public class VMNetCheatCmd : VMNetCommandBodyAbstract
    {
        public override bool AcceptFromClient => false; //if a client wants to cheat that should be ignored.

        public VMCheatContext Context;

        public override bool Execute(VM vm, VMAvatar caller)
        {
            if (!vm.TS1 || Context == null) return false;
            // Community lots can be edited from the map without an active Sim.
            if (caller == null && Context.CheatBehavior != VMCheatContext.VMCheatType.MoveObjects) return false;
            if (Context.Executed) // the cheat has already been run
                return false;
            return Context.Execute(vm, caller);
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            if (FromNet) return false; //disable for network play
            //TODO: add Verify() to cheat context to allow cheats to verify themself based off their intended behavior
            return true;
        }

        #region VMSerializeable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            Context.SerializeInto(writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Context = new VMCheatContext();
            Context.Deserialize(reader);
        }
        #endregion
    }
}
