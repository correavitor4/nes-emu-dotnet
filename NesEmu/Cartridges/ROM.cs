using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace NesEmu.Cartridges
{
    public class ROM
    {
        public List<byte> prgRom;
        public List<byte> chrRom;
        public byte Mapper { get; private set; }
        public ScreenMirroring screenMirroring;
        private static readonly byte[] NES_TAG = [0x4E, 0x45, 0x53, 0x1A];
        private const int PRG_ROM_PAGE_SIZE = 16384;
        private const int CHR_ROM_PAGE_SIZE = 8192;
        public int PrgRomSize { get; private set; }
        public int ChrRomSize { get; private set; }
        public bool SkipTrainer { get; private set; }
        public int PrgRomStart { get; private set; }
        public int ChrRomStart { get; private set; }
        public byte[] PrgRom { get; private set; }
        public byte[] ChrRom { get; private set; }


        public ROM(List<byte> raw)
        {
            CheckINesTag(raw);
            HandleMapper(raw);
            CheckINesVersion(raw);
            ScreenMirroring screenMirroring = GetScreenMirroring(raw);
            PrgRomSize = raw[4] * PRG_ROM_PAGE_SIZE;
            ChrRomSize = raw[5] * CHR_ROM_PAGE_SIZE;
            SkipTrainer = (raw[6] & 0b100) != 0;
            PrgRomStart = 16 + ((Func<int>)(() =>
            {
                if (SkipTrainer) return 512;
                return 0;
            }))();
            ChrRomStart = PrgRomStart + PrgRomSize;
            PrgRom = raw.Skip(PrgRomStart).Take(PrgRomSize).ToArray();
            ChrRom = raw.Skip(ChrRomStart).Take(ChrRomSize).ToArray();
        }

        private ScreenMirroring GetScreenMirroring(List<byte> raw)
        {
            bool verticalMirroring = (raw[6] & 0b1) != 0;
            bool fourScreen = (raw[6] & 0b1000) != 0;
            return (fourScreen, verticalMirroring) switch
            {
                (true, _) => ScreenMirroring.FourScreen,
                (false, true) => ScreenMirroring.Vertical,
                (false, false) => ScreenMirroring.Horizontal,
            };
        }

        private void CheckINesTag(List<byte> raw)
        {
            // 1. Verifica se o arquivo lido tem a assinatura correta do iNES
            if (raw[0] != NES_TAG[0] ||
                raw[1] != NES_TAG[1] ||
                raw[2] != NES_TAG[2] ||
                raw[3] != NES_TAG[3])
            {
                throw new Exception("Arquivo inválido! O arquivo não está no formato iNES oficial.");
            }
        }

        private void HandleMapper(List<byte> raw)
        {
            int mapperTmp = (raw[7] & 0b1111_0000) | (raw[6] >> 4);
            Mapper = (byte)mapperTmp;
        }

        private void CheckINesVersion(List<byte> raw)
        {
            int iNesVersion = (raw[7] >> 2) & 0b11;
            if (iNesVersion != 0)
                throw new Exception("NES2.0 format is not supported");
        }
    }
}