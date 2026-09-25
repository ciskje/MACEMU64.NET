// VISUAL.H colors/keys port: VGA text attributes + key codes
namespace MacEmu.UI;

public static class Vga
{
    // VGA 16-color palette (standard)
    public static readonly Color[] Palette =
    [
        Color.FromArgb(0, 0, 0), Color.FromArgb(0, 0, 170), Color.FromArgb(0, 170, 0), Color.FromArgb(0, 170, 170),
        Color.FromArgb(170, 0, 0), Color.FromArgb(170, 0, 170), Color.FromArgb(170, 85, 0), Color.FromArgb(170, 170, 170),
        Color.FromArgb(85, 85, 85), Color.FromArgb(85, 85, 255), Color.FromArgb(85, 255, 85), Color.FromArgb(85, 255, 255),
        Color.FromArgb(255, 85, 85), Color.FromArgb(255, 85, 255), Color.FromArgb(255, 255, 85), Color.FromArgb(255, 255, 255),
    ];

    // Original COLORxx = 0x??00 -> high byte is VGA attribute (bg<<4|fg), ignore blink bit
    public const int COLOR0 = 0x17, COLOR1 = 0x1F, COLOR2 = 0x71, COLOR3 = 0x9A,
        COLOR4 = 0x1B, COLOR5 = 0x03, COLOR6 = 0x1C, COLOR7 = 0x2B, COLOR8 = 0x1A,
        COLOR9 = 0x1E, COLOR10 = 0x1E, COLOR11 = 0x02, COLOR12 = 0x31, COLOR13 = 0x3F,
        COLOR14 = 0x0C, COLOR15 = 0x2E, COLOR16 = 0x2C, COLOR17 = 0x0F, COLOR18 = 0x07,
        COLOR19 = 0x01, COLOR20 = 0xB1;

    public static Color Fg(int attr) => Palette[attr & 0x0F];
    public static Color Bg(int attr) => Palette[(attr >> 4) & 0x07]; // drop blink
}
