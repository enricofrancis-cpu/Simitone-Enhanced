using System.IO;
using System.Linq;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetChangeControlCmd : VMNetCommandBodyAbstract
    {
        public short TargetID;

        public override bool Execute(VM vm, VMAvatar caller)
        {
            //switch this client's controlled sim to another
            //only used for TS1.

            //not suitable for networking right now. If you want to do anything weird with ts1 multiplayer, needs these additions:
            // - block stealing sims controlled by others
            // - multiplayer handling of "selected sim" global

            var target = vm.GetObjectById(TargetID) as VMAvatar;
            if (target == null || target.Dead) return false;
            if (vm.TS1 && vm.TS1State?.CurrentFamily?.RuntimeSubset?.Contains(target.Object.OBJ.GUID) != true)
                return false;

            if (caller != null && caller.PersistID == ActorUID)
            {
                //relinquish previous control
                vm.Context.ObjectQueries.RemoveAvatarPersist(caller.PersistID);
                caller.PersistID = 0;
            }

            target.PersistID = ActorUID;
            vm.Context.ObjectQueries.RegisterAvatarPersist(target, target.PersistID);
            vm.SetGlobalValue(3, target.ObjectID);

            if (VM.UseWorld && target.WorldUI != null)
            {
                vm.Context.World.CenterTo(target.WorldUI);
            }

            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            return vm.TS1;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(TargetID);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            TargetID = reader.ReadInt16();
        }

        #endregion
    }
}
