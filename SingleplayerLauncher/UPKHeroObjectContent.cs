﻿using SingleplayerLauncher.Model;
using System;
using System.Linq;

namespace SingleplayerLauncher
{

    public sealed class UPKHeroObjectContent
    {

        public UPKFile HeroObjectContent { get; private set; }
        private readonly GameInfo GameInfo = GameInfo.Instance;

        private const int LoadoutSlotByteSize = 4;
        private const int LoadoutSlotsNumber = 9;
        // Where the actual array of the loadout starts is + 28 bytes from the start of the loadout header.
        // Header + Field Type + Field Size in bytes + Array (start?) index + Array number of elements (8 + 8 + 4 + 4 + 4)
        private const int LoadoutArraySizeOffset = 16;
        private const int LoadoutArrayElementCountOffset = 24;
        private const int LoadoutSlotsOffset = 28;
        private const int LoadoutSectionLength = LoadoutSlotsOffset + 4 * LoadoutSlotsNumber;

        private const int GuardianSlotsNumber = 2;
        // Where the actual array of the guardians starts is + 28 bytes from the start of the guardians header.
        // Header + Field type + Field Size in bytes + Array (start?) index + Array number of elements (8 + 8 + 4 + 4 + 4)
        private const int GuardiansArraySizeOffset = 16;
        private const int GuardiansArrayElementCountOffset = 24;
        private const int GuardiansOffset = 28;

        // Header + Field type + Size in bytes + (start?) index  (8 + 8 + 4 + 4)
        private const int SkinOffsetFromHeader = 24;

        // Header + Field type + Size in bytes + (start?) index  (8 + 8 + 4 + 4)
        private const int HealthOffsetFromHeader = 24;
        private const int HealthMaxOffsetFromHeader = 24;
        private const int DefaultArchetypesSectionSize = 32;

        public UPKHeroObjectContent(UPKFile heroObjectContent)
        {
            HeroObjectContent = heroObjectContent;
        }

        public void ApplyLoadoutChanges()
        {
            foreach (UPKFile.Section section in GameInfo.Loadout.Hero.ToRemoveSections)
            {
                HeroObjectContent.RemoveByteSection(section);
            }

            ApplyTrapsGear(); // May insert bytes
            ApplyHealthFix(); // May insert bytes
            //ApplyTraits();

            if (HeroObjectContent.RemovedBytesCount > 0)
            {
                // Hero weaver tree (is after Inventory/Guardians) 
                int positionToFillRemovedBytesWithZeros = HeroObjectContent.FindBytesKMP(GameInfo.Loadout.Hero.WeaverTreeDefault.Header);
                FillRemovedBytes(positionToFillRemovedBytesWithZeros);
            }

            ApplyGuardians(); // Should go after everything else since it's where we are inserting the extra bytes and needs to know the size

            // From here, only edits bytes
            ApplySkin();
        }
        private void ApplyTrapsGear()
        {
            if (GameInfo.Loadout == null || GameInfo.Loadout.SlotItems == null || GameInfo.Loadout.SlotItems.Length != LoadoutSlotsNumber)
            {
                throw new Exception("Loadout must have 9 traps/gear");
            }

            int startIndexLoadout = HeroObjectContent.FindBytesKMP(GameInfo.Loadout.Hero.DefaultInventoryClasses.Header);


            if (startIndexLoadout == -1)
            {
                // Get position after Archetype and Add Header and Field Type
                int archetypesSize = GameInfo.Loadout.Hero.DefaultInventoryArchetypes.Size == null ? DefaultArchetypesSectionSize : (int)GameInfo.Loadout.Hero.DefaultInventoryArchetypes.Size;
                startIndexLoadout = HeroObjectContent.FindBytesKMP(GameInfo.Loadout.Hero.DefaultInventoryArchetypes.Header) + archetypesSize;

                HeroObjectContent.InsertZeroedBytes(startIndexLoadout, LoadoutSectionLength);

                HeroObjectContent.OverrideBytes(GameInfo.Loadout.Hero.DefaultInventoryClasses.Header, startIndexLoadout);
                HeroObjectContent.OverrideBytes(GameInfo.Loadout.Hero.ArrayProperty.Header, startIndexLoadout + GameInfo.Loadout.Hero.DefaultInventoryClasses.Header.Length);
            }

            int arraySizeIndex = startIndexLoadout + LoadoutArraySizeOffset;
            int arrayElementCountIndex = startIndexLoadout + LoadoutArrayElementCountOffset;
            int loadoutSlotsIndex = startIndexLoadout + LoadoutSlotsOffset;

            // IF there aren't 9 slots set up we create them and insert necessary bytes
            int loadoutSlotsUsed = HeroObjectContent.GetByte(arrayElementCountIndex);
            if (loadoutSlotsUsed != LoadoutSlotsNumber)
            {
                HeroObjectContent.OverrideSingleByte((LoadoutSlotsNumber + 1) * LoadoutSlotByteSize, arraySizeIndex); // Array Size (+1 to count the size field itself too)
                HeroObjectContent.OverrideSingleByte(LoadoutSlotsNumber, arrayElementCountIndex); // Array Element Count ( the 4 bytes inbetween are "index 0")

                // Add new slots (as many as missing)
                int indexAfterUsedSlots = loadoutSlotsIndex + (loadoutSlotsUsed * LoadoutSlotByteSize);

                HeroObjectContent.InsertZeroedBytes(indexAfterUsedSlots, (LoadoutSlotsNumber - loadoutSlotsUsed) * LoadoutSlotByteSize);
            }

            // Convert and apply Loadout
            byte[] loadoutBytes = GameInfo.Loadout.SlotItems.SelectMany(slotItem => slotItem.IdByHeroName[GameInfo.Loadout.Hero.Name]).ToArray();
            HeroObjectContent.OverrideBytes(loadoutBytes, loadoutSlotsIndex);
        }

        public static byte[] ConcatByteArrays(params byte[][] arrays) => arrays.SelectMany(x => x).ToArray();

        private void ApplyGuardians()
        {
            if (GameInfo.Loadout == null || GameInfo.Loadout.Guardians == null || GameInfo.Loadout.Guardians.Length != 2)
            {
                throw new Exception("Loadout must have 2 guardians");
            }

            int startIndexGuardians = HeroObjectContent.FindBytesKMP(GameInfo.Loadout.Hero.DefaultGuardianArchetypes.Header);

            if (startIndexGuardians == -1)
            {
                // Get position after Loadout and Add Header and Field Type
                startIndexGuardians = HeroObjectContent.FindBytesKMP(GameInfo.Loadout.Hero.DefaultInventoryClasses.Header) + LoadoutSectionLength;

                HeroObjectContent.OverrideBytes(GameInfo.Loadout.Hero.DefaultGuardianArchetypes.Header, startIndexGuardians);
                HeroObjectContent.OverrideBytes(GameInfo.Loadout.Hero.ArrayProperty.Header, startIndexGuardians + GameInfo.Loadout.Hero.DefaultGuardianArchetypes.Header.Length);
            }

            // Hero weaver tree (is after Inventory/Guardians)
            int endIndex = HeroObjectContent.FindBytesKMP(GameInfo.Loadout.Hero.WeaverTreeDefault.Header, startIndexGuardians + GuardiansOffset);
            int totalSize = endIndex - (startIndexGuardians + GuardiansArrayElementCountOffset); // Everything after header

            int guardiansArraySizeIndex = startIndexGuardians + GuardiansArraySizeOffset;
            int guardiansArrayElementCountIndex = startIndexGuardians + GuardiansArrayElementCountOffset;
            int guardiansIndex = startIndexGuardians + GuardiansOffset;

            byte[] firstGuardian = GameInfo.Loadout.Guardians[0].CodeNameBytes; // Extra space after each guardian is already included
            byte[] secondGuardian = GameInfo.Loadout.Guardians[1].CodeNameBytes;

            byte[] sizeFirstGuardian = new byte[] { (byte)firstGuardian.Length, 0x00, 0x00, 0x00 }; // Add the 0x00 to complete the 4 bytes field

            int secondGuardianOffset = firstGuardian.Length + sizeFirstGuardian.Length + GuardiansOffset + 4; // 4 from second guardian size itself
            byte[] sizeSecondGuardian = new byte[] { (byte)(totalSize - secondGuardianOffset), 0x00, 0x00, 0x00 }; // Counting extra space

            int emptySpaceOffset = secondGuardianOffset + secondGuardian.Length;

            byte[] emptySpace = Enumerable.Repeat((byte)0x00, totalSize - emptySpaceOffset).ToArray();
            // Combining arrays to SizeGuardian1 + Guardian1 + SizeGuardian2 + Guardian2 + emptySpace
            byte[] guardiansBytes = sizeFirstGuardian.Concat(firstGuardian).Concat(sizeSecondGuardian).Concat(secondGuardian).Concat(emptySpace).ToArray();

            HeroObjectContent.OverrideBytes(guardiansBytes, guardiansIndex);

            HeroObjectContent.OverrideSingleByte((byte)totalSize, guardiansArraySizeIndex); // Size (counts array element count and both guardians and their sizes but not itself or index so -8)
            //TODO check single byte and avoid override?
            HeroObjectContent.OverrideSingleByte(GuardianSlotsNumber, guardiansArrayElementCountIndex); // Array Element Count 
        }

        private void ApplySkin()
        {
            if (GameInfo.Loadout == null || GameInfo.Loadout.Skin == null)
            {
                throw new Exception("Hero must have a skin");
            }

            int startIndexSkin = HeroObjectContent.FindBytesKMP(GameInfo.Loadout.Hero.CurrentSkinClass.Header);
            HeroObjectContent.OverrideBytes(GameInfo.Loadout.Skin.Id, startIndexSkin + SkinOffsetFromHeader);
        }

        private void ApplyHealthFix()
        {
            int startIndexHealth = HeroObjectContent.FindBytesKMP(GameInfo.Loadout.Hero.Health.Header);
            int startIndexHealthMax = HeroObjectContent.FindBytesKMP(GameInfo.Loadout.Hero.HealthMax.Header);
            // Original Game Formula. Past lvl 100 it has diminishing returns (first operand of the sum stops incrementing and the second one starts doing so at a much lower rate)
            int accountLevel = GameInfo.Battleground.Difficulty.AccountLevel;
            double healthMultiplier = Math.Pow(1.00763, (Math.Min(accountLevel, 100) - 1)) + (0.00033 * Math.Max((accountLevel - 100), 0));
            int baseHealth = GameInfo.Loadout.Hero.BaseHealth;

            double health = baseHealth * healthMultiplier;

            byte[] healthAsByteArray = BitConverter.GetBytes((float)health);
            HeroObjectContent.OverrideBytes(healthAsByteArray, startIndexHealth + HealthOffsetFromHeader);

            int healthSectionSize = HealthOffsetFromHeader + healthAsByteArray.Length;
            if (startIndexHealthMax == -1 && HeroObjectContent.RemovedBytesCount >= healthSectionSize)
            {
                // Get position after Health and Add HealthMax Section
                startIndexHealthMax = startIndexHealth + healthSectionSize;
                HeroObjectContent.InsertBytes(HeroObjectContent.GetSubArray(startIndexHealth, healthSectionSize), startIndexHealthMax);
                HeroObjectContent.OverrideBytes(GameInfo.Loadout.Hero.HealthMax.Header, startIndexHealthMax);
            }

            if (startIndexHealthMax != -1)
            {
                HeroObjectContent.OverrideBytes(healthAsByteArray, startIndexHealthMax + HealthMaxOffsetFromHeader);
            }
        }

        private void FillRemovedBytes(int insertIndex)
        {
            HeroObjectContent.InsertZeroedBytes(insertIndex, 0);
        }
    }
}
