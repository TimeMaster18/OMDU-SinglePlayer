﻿namespace SingleplayerLauncher.Mods
{
    public class NoTrapCap : Mod
    {
        private const int CHANGE_INDEX = 0x15444A3;

        public override bool InstallMod()
        {
            UPKFile.OverrideSingleByte(0, CHANGE_INDEX);
            return true;
        }

        public override bool UninstallMod()
        {
            UPKFile.OverrideSingleByte(1, CHANGE_INDEX);
            return true;
        }
    }
}
