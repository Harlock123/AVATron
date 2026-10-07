using AVATron.Core.Entities;
using AVATron.Core.Waves;

namespace AVATron.Core.Simulation;

/// Places a wave's objects. Grunts, Hulks and Electrodes never start inside a safety box around the
/// player's start (the box shrinks over the first ten waves); Brains start near an edge; Sphereoids at the
/// left/right edge; Quarks at the top/bottom edge; the family anywhere.
public static class WaveBuilder
{
    /// Safety box per wave in pixels (arcade bytes x2) and lines: (x0, x1, y0, y1).
    public static (int X0, int X1, int Y0, int Y1) SafetyBox(int wave) => wave switch
    {
        1 => (0x1A * 2, 0x7A * 2, 0x40, 0xB0),
        2 => (0x1A * 2, 0x7A * 2, 0x48, 0xA8),
        3 => (0x2A * 2, 0x6A * 2, 0x50, 0xA0),
        4 => (0x30 * 2, 0x60 * 2, 0x54, 0x9D),
        < 10 => (0x35 * 2, 0x59 * 2, 0x5D, 0x96),
        _ => (0x38 * 2, 0x5C * 2, 0x62, 0x94),
    };

    /// Electrodes keep a slightly larger margin (reconstruction: +4 px / +4 lines).
    const int ElectrodeExtraMargin = 4;

    public static void Populate(World w, WaveCounts c)
    {
        var rng = w.Rng;
        var box = SafetyBox(w.Wave);
        int order = 0;
        int shape = Sprites.SpriteLibrary.ElectrodeShapeForWave(w.Wave);

        for (int i = 0; i < c.Electrodes; i++)
        {
            var e = new Electrode { Shape = shape };
            PlaceOutsideBox(w, e, box, ElectrodeExtraMargin);
            w.Electrodes.Add(e);
        }
        var family = new List<Human>();
        void AddFamily(EntityKind k, int n)
        {
            for (int i = 0; i < n; i++)
            {
                var h = new Human(k);
                h.X = rng.Next(Arena.Left, Arena.Right - h.Image.Width + 1) * Arena.One;
                h.Y = rng.Next(Arena.Top, Arena.Bottom - h.Image.Height + 1) * Arena.One;
                family.Add(h);
            }
        }
        AddFamily(EntityKind.Mommy, c.Mommies);
        AddFamily(EntityKind.Daddy, c.Daddies);
        AddFamily(EntityKind.Mikey, c.Mikeys);
        w.Family.AddRange(family);

        for (int i = 0; i < c.Grunts; i++)
        {
            var g = new Grunt { AppearOrder = order++ };
            PlaceOutsideBox(w, g, box, 0);
            g.StepTimer = w.RandU(w.RobSpd);
            w.Enemies.Add(g);
        }
        int rr = 0;
        for (int i = 0; i < c.Hulks; i++)
        {
            var h = new Hulk { AppearOrder = order++ };
            PlaceOutsideBox(w, h, box, 0);
            // About 3 in 4 hulks are assigned a family member (round-robin); the rest hunt the player.
            if (family.Count > 0 && rng.Next(256) <= 0xC0) h.Target = family[rr++ % family.Count];
            w.Enemies.Add(h);
        }
        for (int i = 0; i < c.Brains; i++)
        {
            var b = new Brain { AppearOrder = order++ };
            int wdt = b.Image.Width, hgt = b.Image.Height;
            switch (rng.Next(4))
            {
                case 0: b.X = rng.Next(Arena.Left, Arena.Left + 32) * Arena.One; b.Y = rng.Next(Arena.Top, Arena.Bottom - hgt) * Arena.One; break;
                case 1: b.X = rng.Next(Arena.Right - wdt - 32, Arena.Right - wdt) * Arena.One; b.Y = rng.Next(Arena.Top, Arena.Bottom - hgt) * Arena.One; break;
                case 2: b.Y = rng.Next(Arena.Top, Arena.Top + 32) * Arena.One; b.X = rng.Next(Arena.Left, Arena.Right - wdt) * Arena.One; break;
                default: b.Y = rng.Next(Arena.Bottom - hgt - 32, Arena.Bottom - hgt) * Arena.One; b.X = rng.Next(Arena.Left, Arena.Right - wdt) * Arena.One; break;
            }
            b.Init(w);
            w.Enemies.Add(b);
        }
        for (int i = 0; i < c.Sphereoids; i++)
        {
            var s = new Sphereoid { AppearOrder = order };
            s.X = (rng.Next(2) == 0 ? Arena.Left + 4 : (0x8F - 8) * 2) * Arena.One;
            s.Y = rng.Next(Arena.Top, Arena.Bottom - s.Image.Height) * Arena.One;
            s.Init(w);
            w.Enemies.Add(s);
        }
        for (int i = 0; i < c.Quarks; i++)
        {
            var q = new Quark { AppearOrder = order };
            q.Y = (rng.Next(2) == 0 ? Arena.Top + 2 : 234 - 14) * Arena.One;
            q.X = rng.Next(Arena.Left, Arena.Right - q.Image.Width) * Arena.One;
            q.Init(w);
            w.Enemies.Add(q);
        }
    }

    /// Random spot clear of the safety box and of objects already placed (up to 64 tries, then any spot outside the box).
    static void PlaceOutsideBox(World w, Entity e, (int X0, int X1, int Y0, int Y1) box, int margin)
    {
        var rng = w.Rng;
        int wdt = e.Image.Width, hgt = e.Image.Height;
        for (int attempt = 0; ; attempt++)
        {
            int x = rng.Next(Arena.Left, Arena.Right - wdt + 1), y = rng.Next(Arena.Top, Arena.Bottom - hgt + 1);
            bool inBox = x + wdt > box.X0 - margin && x < box.X1 + margin && y + hgt > box.Y0 - margin && y < box.Y1 + margin;
            if (inBox) continue;
            e.X = x * Arena.One; e.Y = y * Arena.One;
            if (attempt >= 64 || !OverlapsPlaced(w, e)) return;
        }
    }

    static bool OverlapsPlaced(World w, Entity e)
    {
        foreach (var o in w.Electrodes) if (o != e && e.Touches(o)) return true;
        foreach (var o in w.Enemies) if (e.Touches(o)) return true;
        return false;
    }
}
