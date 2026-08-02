using System.Text.RegularExpressions;
using NesEmu.Memory;
using Xunit.Abstractions;

namespace NesEmuTests.nestest;

public class NestestIntegrationTest
{
    private readonly ITestOutputHelper _output;

    // O xUnit permite ejetar logs de erro usando o ITestOutputHelper
    public NestestIntegrationTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void RunNestest__ShouldMatchGoldenLogPerfectly()
    {
        // 1. Carrega os arquivos (agora apontando para a subpasta)
        string romPath = Path.Combine("nestest", "nestest.nes");
        string logPath = Path.Combine("nestest", "nestest.log");

        Assert.True(File.Exists(romPath), "Arquivo nestest.nes não encontrado!");
        Assert.True(File.Exists(logPath), "Arquivo nestest.log não encontrado!");

        byte[] rom = File.ReadAllBytes(romPath);
        string[] goldenLog = File.ReadAllLines(logPath);

        // 2. Extrai a PRG-ROM (Pula o cabeçalho iNES de 16 bytes)
        // O nestest tem apenas 1 banco de PRG (16KB). 
        byte[] prgRom = new byte[16384];
        Array.Copy(rom, 16, prgRom, 0, 16384);

        // 3. Monta a memória. Em cartuchos NROM de 16KB, a memória de $8000 a $BFFF 
        // é espelhada (repetida) em $C000 a $FFFF.
        var program = new byte[0x10000];
        Array.Copy(prgRom, 0, program, 0x8000, 16384);
        Array.Copy(prgRom, 0, program, 0xC000, 16384);

        var mem = NesMemory.FromBytesArray(program);
        var cpu = new NesEmu.CPU.CPU(mem);

        // 4. Configuração inicial OBRIGATÓRIA do nestest no modo de automação
        cpu.ProgramCounter = 0xC000;
        cpu.RegisterA = 0x00;
        cpu.RegisterX = 0x00;
        cpu.RegisterY = 0x00;
        cpu.SetStatusFlag(0x24); // Status inicial (Interrupt Disable e Unused ligados)
        cpu.SetStackPointer(0xFD);

        // Regex para capturar os valores do arquivo de texto
        // Exemplo da linha: "C000  4C F5 C5  JMP $C5F5                       A:00 X:00 Y:00 P:24 SP:FD PPU:  0, 21 CYC:7"
        var regex = new Regex(@"^([0-9A-F]{4}).*A:([0-9A-F]{2}) X:([0-9A-F]{2}) Y:([0-9A-F]{2}) P:([0-9A-F]{2}) SP:([0-9A-F]{2})", RegexOptions.Compiled);

        int lineIndex = 0;

        foreach (var line in goldenLog)
        {
            lineIndex++;
            var match = regex.Match(line);
            if (!match.Success)
                continue;

            // Extrai o que o gabarito diz que DEVERIA ser o estado atual
            ushort expectedPC = Convert.ToUInt16(match.Groups[1].Value, 16);
            byte expectedA = Convert.ToByte(match.Groups[2].Value, 16);
            byte expectedX = Convert.ToByte(match.Groups[3].Value, 16);
            byte expectedY = Convert.ToByte(match.Groups[4].Value, 16);
            byte expectedP = Convert.ToByte(match.Groups[5].Value, 16);
            byte expectedSP = Convert.ToByte(match.Groups[6].Value, 16);

            // Monta a string do estado ATUAL da sua CPU para ajudar no debug caso falhe
            string actualState = $"PC:{cpu.ProgramCounter:X4} A:{cpu.RegisterA:X2} X:{cpu.RegisterX:X2} Y:{cpu.RegisterY:X2} P:{cpu.GetRegisterStatus():X2} SP:{cpu.GetStackPointer():X2}";
            string expectedState = $"PC:{expectedPC:X4} A:{expectedA:X2} X:{expectedX:X2} Y:{expectedY:X2} P:{expectedP:X2} SP:{expectedSP:X2}";

            // 5. O momento da verdade: Compara antes de executar
            try
            {
                Assert.Equal(expectedPC, cpu.ProgramCounter);
                Assert.Equal(expectedA, cpu.RegisterA);
                Assert.Equal(expectedX, cpu.RegisterX);
                Assert.Equal(expectedY, cpu.RegisterY);
                Assert.Equal(expectedP, cpu.GetRegisterStatus());
                Assert.Equal(expectedSP, cpu.GetStackPointer());
            }
            catch (Exception ex)
            {
                // Se quebrar, nós paramos o teste e ejetamos exatamente ONDE quebrou
                string errorMsg = $"\nFALHA NA LINHA {lineIndex} DO LOG!\nEsperado: {expectedState}\nAtual:    {actualState}\nLinha do Log: {line}";
                _output.WriteLine(errorMsg);
                throw new Exception(errorMsg, ex);
            }

            // 6. Tudo confere! Executa a instrução para avançar para a próxima linha
            cpu.Interpret(limit: 1);
        }
    }
}