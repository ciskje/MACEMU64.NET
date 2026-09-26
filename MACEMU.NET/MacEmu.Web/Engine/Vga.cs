// VISUAL.H colors port without System.Drawing: plain RGB palette + attributes.
namespace MacEmu.Web.Engine;

public static class Vga
{
    // VGA 16-color palette (standard), same values as MacEmu.UI/Vga.cs.
    public static readonly byte[,] Palette =
    {
        { 0, 0, 0 }, { 0, 0, 170 }, { 0, 170, 0 }, { 0, 170, 170 },
        { 170, 0, 0 }, { 170, 0, 170 }, { 170, 85, 0 }, { 170, 170, 170 },
        { 85, 85, 85 }, { 85, 85, 255 }, { 85, 255, 85 }, { 85, 255, 255 },
        { 255, 85, 85 }, { 255, 85, 255 }, { 255, 255, 85 }, { 255, 255, 255 },
    };

    public const int COLOR0 = 0x17, COLOR1 = 0x1F, COLOR2 = 0x71, COLOR3 = 0x9A,
        COLOR4 = 0x1B, COLOR5 = 0x03, COLOR6 = 0x1C, COLOR7 = 0x2B, COLOR8 = 0x1A,
        COLOR9 = 0x1E, COLOR10 = 0x1E, COLOR11 = 0x02, COLOR12 = 0x31, COLOR13 = 0x3F,
        COLOR14 = 0x0C, COLOR15 = 0x2E, COLOR16 = 0x2C, COLOR17 = 0x0F, COLOR18 = 0x07,
        COLOR19 = 0x01, COLOR20 = 0xB1;

    public static int Fg(int attr) => attr & 0x0F;
    public static int Bg(int attr) => (attr >> 4) & 0x07; // drop blink
}
