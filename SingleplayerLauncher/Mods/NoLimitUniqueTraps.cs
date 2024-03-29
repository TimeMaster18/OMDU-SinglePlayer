﻿namespace SingleplayerLauncher.Mods
{
    public class NoLimitUniqueTraps : Mod
    {
        private const int CHANGE_INDEX = 0x16157FE; // ( 5F B0 00 00 ): UseCountLimiter

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