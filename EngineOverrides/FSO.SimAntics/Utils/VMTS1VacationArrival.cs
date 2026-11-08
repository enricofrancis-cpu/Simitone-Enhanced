using System.Linq;
using FSO.Files.Formats.IFF.Chunks;
using FSO.LotView.Model;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.Primitives;

namespace FSO.SimAntics.Utils
{
    public static class VMTS1VacationArrival
    {
        public static bool IsVacationLot(int lot) => lot >= 40 && lot <= 49;

        public static VMAvatar Prepare(VM vm, FAMI family, uint travelerGuid)
        {
            if (family == null || !family.FamilyGUIDs.Contains(travelerGuid)) return null;

            family.SelectWholeFamily();
            vm.TS1State.ActivateFamily(vm, family);
            var landmarks = vm.Entities.Where(entity => entity.Position != LotTilePos.OUT_OF_WORLD &&
                (entity.Object.OBJ.GUID == 0x865A6812 || entity.Object.OBJ.GUID == 0xD564C66B ||
                 entity.Object.OBJ.GUID == 0x303CD603 || entity.Object.OBJ.GUID == 0xEF121974 ||
                 entity.Object.OBJ.GUID == 0x1D95C9B0)).ToArray();

            VMAvatar selected = null;
            foreach (var guid in family.RuntimeSubset.OrderBy(guid => guid != travelerGuid))
            {
                var avatar = vm.Context.ObjectQueries.Avatars.OfType<VMAvatar>().FirstOrDefault(person =>
                    !person.Dead && person.Object.OBJ.GUID == guid);
                if (avatar == null)
                {
                    avatar = vm.Context.CreateObjectInstance(guid, LotTilePos.OUT_OF_WORLD,
                        Direction.NORTH)?.BaseObject as VMAvatar;
                    if (avatar == null) continue;
                    vm.Scheduler.RescheduleInterrupt(avatar);
                }

                avatar.SetPersonData(VMPersonDataVariable.TS1FamilyNumber, (short)family.ChunkID);
                if (avatar.Position == LotTilePos.OUT_OF_WORLD && avatar.Container == null)
                {
                    if (!landmarks.Any(landmark => VMFindLocationFor.FindLocationFor(
                        avatar, landmark, vm.Context, VMPlaceRequestFlags.Default)))
                    {
                        // Custom destinations can omit the stock arrival landmarks.
                        var architecture = vm.Context.Architecture;
                        for (short y = 2; y < architecture.Height - 2 && avatar.Position == LotTilePos.OUT_OF_WORLD; y++)
                        {
                            for (short x = 2; x < architecture.Width - 2; x++)
                            {
                                var position = LotTilePos.FromBigTile(x, y, 1);
                                if (avatar.SetPosition(position, Direction.NORTH, vm.Context).Status == VMPlacementError.Success)
                                    break;
                            }
                        }
                    }
                }
                avatar.AvatarState.Permissions = VMTSOAvatarPermissions.Owner;
                if (selected == null && !avatar.IsPet && avatar.Position != LotTilePos.OUT_OF_WORLD)
                    selected = avatar;
            }
            vm.SetGlobalValue(3, selected?.ObjectID ?? 0);
            return selected;
        }
    }
}
