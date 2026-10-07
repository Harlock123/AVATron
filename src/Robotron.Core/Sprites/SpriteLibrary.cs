namespace Robotron.Core.Sprites;

/// All game images. Original pixel art authored for this project at the arcade's documented object
/// sizes (RESEARCH.md §Sizes). No graphics were extracted from ROMs. Sizes matter: images are also the
/// collision masks.
public static class SpriteLibrary
{
    // ---- Player (8x12). Walk cycle uses frames [0,1,0,2]. ----
    const string PlayerDownTop = "..WWWW..|..WCCW..|..SSSS..|.RRRRRR.|R.RRRR.R|R.RRRR.R|S.YYYY.S|..BBBB..";
    const string PlayerUpTop = "..WWWW..|..WWWW..|..SSSS..|.RRRRRR.|R.RRRR.R|R.RRRR.R|S.YYYY.S|..BBBB..";
    const string PlayerSideTop = "..WWW...|.CCWW...|..SSS...|..RRRR..|..RRRR..|.SRRRR..|..YYYY..|..BBBB..";
    static readonly string[] PlayerFrontLegs = ["..B..B..|..B..B..|..B..B..|.WW..WW.", "..B..B..|.B....B.|.B....B.|WW....WW", "..B..B..|..B..B..|..B..B..|..WWWW.."];
    static readonly string[] PlayerSideLegs = ["..B.B...|..B.B...|..B.B...|.WW.WW..", "..B..B..|.B...B..|B.....B.|WW....WW", "..BB....|..BB....|..BB....|.WWW...."];

    public static readonly Sprite[] PlayerDown = Walk("player-down", PlayerDownTop, PlayerFrontLegs);
    public static readonly Sprite[] PlayerUp = Walk("player-up", PlayerUpTop, PlayerFrontLegs);
    public static readonly Sprite[] PlayerLeft = Walk("player-left", PlayerSideTop, PlayerSideLegs);
    public static readonly Sprite[] PlayerRight = PlayerLeft.Select(Mirror).ToArray();
    public static readonly Sprite LifeIcon = Sprite.Parse("life", "..WW..|..SS..|.RRRR.|R.RR.R|..YY..|..BB..|.B..B.|WW..WW");

    // ---- Grunt (10x13), 3 walk frames ----
    const string GruntTop = "...YYYY...|...YGGY...|...YYYY...|.RRRRRRRR.|YRRRYYRRRY|Y.RRRRRR.Y|Y.RRRRRR.Y|Y.RRRRRR.Y|...RRRR...";
    public static readonly Sprite[] Grunt = Walk("grunt", GruntTop,
        ["...R..R...|...R..R...|...R..R...|..YY..YY..", "...R..R...|..R....R..|..R....R..|.YY....YY.", "...R..R...|...R..R...|....RR....|...YYYY..."]);

    // ---- Hulk (14x16), 3 walk frames ----
    const string HulkTop = ".....RRRR.....|.....RYYR.....|.....RRRR.....|..GGGGGGGGGG..|.GGGGGGGGGGGG.|GGG.GG11GG.GGG|GG..GGGGGG..GG|GG..GGGGGG..GG|GG..GGGGGG..GG|SS..GGGGGG..SS|....GGGGGG....|....GGGGGG....";
    public static readonly Sprite[] Hulk = Walk("hulk", HulkTop,
        ["....GG..GG....|....GG..GG....|....GG..GG....|...KKK..KKK...", "....GG..GG....|...GG....GG...|...GG....GG...|..KKK....KKK..", "....GG..GG....|....GG..GG....|.....GGGG.....|....KKKKKK...."]);

    // ---- Family ----
    const string MommyTop = "..YYYY..|.YSSSSY.|.YSSSSY.|..SSSS..|..PPPP..|.SPPPPS.|S.PPPP.S|..PPPP..|.PPPPPP.|.PPPPPP.|PPPPPPPP";
    public static readonly Sprite[] Mommy = Walk("mommy", MommyTop, ["..S..S..|..S..S..|.RR..RR.", "..S..S..|.S....S.|RR....RR", "..S..S..|..S..S..|..RRRR.."]);
    const string DaddyTop = "...KKKK...|...SSSS...|...SCCS...|...SSSS...|.BBBRRBBB.|B.BBRRBB.B|B.BBBBBB.B|S.BBBBBB.S|..BBBBBB..";
    public static readonly Sprite[] Daddy = Walk("daddy", DaddyTop, ["...B..B...|...B..B...|...B..B...|..KK..KK..", "...B..B...|..B....B..|..B....B..|.KK....KK.", "...B..B...|...B..B...|....BB....|...KKKK..."]);
    const string MikeyTop = ".YYYY.|.SSSS.|.SCCS.|..SS..|.RRRR.|R.RR.R|S.RR.S|.BBBB.";
    public static readonly Sprite[] Mikey = Walk("mikey", MikeyTop, [".B..B.|.B..B.|WW..WW", ".B..B.|B....B|W....W", ".B..B.|.B..B.|..WW.."]);

    // ---- Brain (14x16), 2 walk frames ----
    const string BrainTop = "...MMMMMMMM...|..MM1MM1MMMM..|.MMMMM1MM1MMM.|.M1MMMMMMMM1M.|.MMMM1MM1MMMM.|..MMMMMMMMMM..|....WCCCCW....|....CRCCRC....|.....CCCC.....|....BBBBBB....|...B.BBBB.B...|...B.BBBB.B...|...C.BBBB.C...|.....BBBB.....";
    public static readonly Sprite[] Brain = Walk("brain", BrainTop, [".....B..B.....|....CC..CC....", "....B....B....|...CC....CC..."]);

    // ---- Sphereoid (16x15), 8-frame pulsing rings, generated; shots test a solid stand-in ("fat" image) ----
    public static readonly Sprite[] Sphereoid = Enumerable.Range(0, 8).Select(SphereoidFrame).ToArray();
    public static readonly Sprite SphereoidShotMask = Sprite.Solid("sphereoid-fat", 16, 15);

    // ---- Enforcer (10x11) and its spark (8x7) ----
    public static readonly Sprite Enforcer = Sprite.Parse("enforcer",
        "...WWWW...|..WWWWWW..|.WCWWWWCW.|.WWWWWWWW.|..KKKKKK..|.KK.KK.KK.|KKKKKKKKKK|K..KKKK..K|...KKKK...|...K..K...|..KK..KK..");
    public static readonly Sprite[] Spark =
    [
        Sprite.Parse("spark0", "1......1|.1....1.|..1..1..|...22...|..1..1..|.1....1.|1......1"),
        Sprite.Parse("spark1", "...11...|...11...|...22...|11222211|...22...|...11...|...11..."),
    ];

    // ---- Quark (16x15), rotating spokes, generated ----
    public static readonly Sprite[] Quark = Enumerable.Range(0, 4).Select(QuarkFrame).ToArray();
    public static readonly Sprite QuarkShotMask = Sprite.Solid("quark-fat", 16, 15);

    // ---- Tank (14x16) with 2 tread frames, and its shell (8x7) ----
    const string TankTop = ".....1111.....|....111111....|...11111111...|...1..11..1...|...11111111...|.....KKKK.....|..KKKKKKKKKK..|.KK22222222KK.|.KKKKKKKKKKKK.|.KKKKKKKKKKKK.";
    public static readonly Sprite[] Tank =
    [
        Sprite.Parse("tank0", TankTop + "|KKKKKKKKKKKKKK|K.K.K.K.K.K.KK|KKKKKKKKKKKKKK|.K.K.K.K.K.K.K|KKKKKKKKKKKKKK|.............."),
        Sprite.Parse("tank1", TankTop + "|KKKKKKKKKKKKKK|.K.K.K.K.K.K.K|KKKKKKKKKKKKKK|K.K.K.K.K.K.KK|KKKKKKKKKKKKKK|.............."),
    ];
    public static readonly Sprite Shell = Sprite.Parse("shell", "...WW...|..WCCW..|.WCWWCW.|WCWCCWCW|.WCWWCW.|..WCCW..|...WW...");

    // ---- Brain cruise missile head (6x4) ----
    public static readonly Sprite CruiseMissile = Sprite.Parse("missile", ".1111.|112211|112211|.1111.");
    public static readonly Sprite CruiseMissileShotMask = Sprite.Solid("missile-fat", 6, 4);

    // ---- Electrodes (10x9): ten shape slots; wave w uses ElectrodeShapeForWave(w) ----
    public static readonly Sprite[] Electrode =
    [
        E("star",      "1...1...1|.1..1..1.|..1.1.1..|...121...|111222111|...121...|..1.1.1..|.1..1..1.|1...1...1"),
        E("snowflake", "....1....|.1..1..1.|..1.2.1..|...222...|11.222.11|...222...|..1.2.1..|.1..1..1.|....1...."),
        E("square",    "111111111|1.......1|1.22222.1|1.2...2.1|1.2...2.1|1.2...2.1|1.22222.1|1.......1|111111111"),
        E("triangle",  "....1....|....1....|...121...|...121...|..12221..|..12.21..|.1222221.|.1.....1.|111111111"),
        E("bar",       "...111...|...121...|...121...|...121...|...121...|...121...|...121...|...121...|...111..."),
        E("diamond",   "....1....|...121...|..12.21..|.12...21.|12.....21|.12...21.|..12.21..|...121...|....1...."),
        E("spike",     "....1....|....1....|....2....|...222...|112222211|...222...|....2....|....1....|....1...."),
        E("spiral",    "111111111|1.......1|1.22222.1|1.2...2.1|1.2.1.2.1|1.2.1...1|1.2.11111|1.2......|1.2222222"),
        E("year",      "11..111..|..1.1.1..|.1..1.1..|111.111..|.........|222.2.2..|222.222..|2.2...2..|222...2.."),
    ];

    /// Shape slot per wave, cycling every ten waves (wave 9 repeats wave 1's shape) — RESEARCH.md §Electrode.
    public static int ElectrodeShapeForWave(int wave) => ElectrodeOrder[(Math.Max(1, wave) - 1) % 10];
    static readonly int[] ElectrodeOrder = [0, 1, 2, 3, 4, 5, 6, 7, 0, 8];

    // ---- Prog (12x16): a reprogrammed human, stretched and recoloured, per family member ----
    public static readonly Sprite[] ProgFromMommy = Mommy.Select(ToProg).ToArray();
    public static readonly Sprite[] ProgFromDaddy = Daddy.Select(ToProg).ToArray();
    public static readonly Sprite[] ProgFromMikey = Mikey.Select(ToProg).ToArray();

    // ---- Misc ----
    public static readonly Sprite Skull = Sprite.Parse("skull", "...WWWWWW...|..WWWWWWWW..|.WWWWWWWWWW.|.WW..WW..WW.|.WW..WW..WW.|.WWWWWWWWWW.|..WWW..WWW..|...WWWWWW...|...W.WW.W...|...WWWWWW...|....WWWW....");

    /// Player laser images: horizontal 6x1, vertical 2x6, diagonals 6x6 (arcade sizes).
    public static readonly Sprite ShotHorizontal = Sprite.Solid("shot-h", 6, 1);
    public static readonly Sprite ShotVertical = Sprite.Solid("shot-v", 2, 6);
    public static readonly Sprite ShotDiagDown = Sprite.Parse("shot-dd", "W.....|.W....|..W...|...W..|....W.|.....W");   // "\"
    public static readonly Sprite ShotDiagUp = Sprite.Parse("shot-du", ".....W|....W.|...W..|..W...|.W....|W.....");     // "/"

    public static Sprite ShotFor(int dx, int dy) =>
        dy == 0 ? ShotHorizontal : dx == 0 ? ShotVertical : (dx == dy ? ShotDiagDown : ShotDiagUp);

    // ---------------------------------------------------------------------------------------
    static Sprite[] Walk(string name, string top, string[] legs) =>
        legs.Select((l, i) => Sprite.Parse($"{name}{i}", top + "|" + l)).ToArray();

    static Sprite E(string name, string art9) =>
        Sprite.Parse("electrode-" + name, string.Join('|', art9.Split('|').Select(r => r + ".")));

    public static Sprite Mirror(Sprite s)
    {
        var px = new byte[s.Pixels.Length];
        for (int y = 0; y < s.Height; y++)
            for (int x = 0; x < s.Width; x++) px[y * s.Width + x] = s.Pixels[y * s.Width + (s.Width - 1 - x)];
        return new Sprite(s.Name + "-m", s.Width, s.Height, px);
    }

    static Sprite ToProg(Sprite h)
    {
        const int w = 12, hh = 16;
        var px = new byte[w * hh];
        int ox = (w - h.Width) / 2;
        for (int y = 0; y < hh; y++)
        {
            int sy = y * h.Height / hh;
            for (int x = 0; x < h.Width; x++)
            {
                byte c = h.Pixels[sy * h.Width + x];
                if (c != Pal.Clear) px[y * w + x + ox] = c is Pal.Cyan or Pal.White ? Pal.Cycle2 : Pal.Cycle1;
            }
        }
        return new Sprite("prog-" + h.Name, w, hh, px);
    }

    static Sprite SphereoidFrame(int f)
    {
        const int w = 16, h = 15;
        var px = new byte[w * h];
        double cx = 7.5, cy = 7;
        // two rings that breathe out of phase
        double r1 = 2 + (f % 8) * 0.75, r2 = 2 + ((f + 4) % 8) * 0.75;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                double d = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy) * 1.05);
                if (Math.Abs(d - r1) < 0.6) px[y * w + x] = Pal.Cycle1;
                else if (Math.Abs(d - r2) < 0.6) px[y * w + x] = Pal.Cycle2;
            }
        return new Sprite($"sphereoid{f}", w, h, px);
    }

    static Sprite QuarkFrame(int f)
    {
        const int w = 16, h = 15;
        var px = new byte[w * h];
        double cx = 7.5, cy = 7;
        for (int spoke = 0; spoke < 4; spoke++)
        {
            double a = (f * 22.5 + spoke * 90) * Math.PI / 180;
            for (double r = 0; r <= 7.2; r += 0.25)
            {
                int x = (int)Math.Round(cx + Math.Cos(a) * r), y = (int)Math.Round(cy + Math.Sin(a) * r * 0.95);
                if (x is >= 0 and < w && y is >= 0 and < h) px[y * w + x] = r < 3 ? Pal.Cycle2 : Pal.Cycle1;
            }
        }
        // small hub
        for (int y = 6; y <= 8; y++) for (int x = 7; x <= 8; x++) px[y * w + x] = Pal.White;
        return new Sprite($"quark{f}", w, h, px);
    }
}
