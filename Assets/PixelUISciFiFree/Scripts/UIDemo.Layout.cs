using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HeyHeyThere.PixelUISciFiFree
{
    /// <summary>
    /// The demo's screen: a ship's HUD, a systems window, a cargo hold, a target lock and a comms line
    /// over a starfield.
    /// </summary>
    public partial class UIDemo
    {
        static readonly Color Background = new Color(0.02f, 0.03f, 0.06f);

        /// <summary>Stars and a planet behind the UI, so the panels show they let a little through.</summary>
        static Texture2D Backdrop()
        {
            var t = Paper(428, 241, out var rect);
            var rng = new System.Random(7);
            for (int i = 0; i < 140; i++)
            {
                int x = rng.Next(0, 428), y = rng.Next(0, 241);
                var v = 0.25f + 0.75f * (float)rng.NextDouble();
                rect(x, y, 1, 1, new Color(v, v, Mathf.Min(1, v * 1.1f)));
            }
            Circle(t, 330, 180, 70, new Color(0.09f, 0.1f, 0.2f));
            Circle(t, 318, 170, 58, new Color(0.12f, 0.14f, 0.27f));
            t.Apply();
            return t;
        }

        void Layout()
        {
            Hud();
            Systems();
            Cargo();
            Target();
            Comms();
            Switcher(6, 222, 2);
        }

        void Hud()
        {
            var rows = Place(Column(screen, 1), 6, 5);
            foreach (var (icon, colour, value) in new[] { ("heart", "Red", 72), ("shield", "Blue", 48), ("energy", "Gold", 90) })
            {
                var r = Row(rows, 2);
                Icon(r, icon);
                Centre((RectTransform)Bar(r, colour, value, 70, 8).transform);
            }
            var money = Place(Row(screen, 1), 290, 5);
            foreach (var (icon, text) in new[] { ("credit", "4096"), ("crystal", "12"), ("fuel", "68%") })
            {
                Icon(money, icon);
                Label(money, text);
                Gap(money, 4);
            }
        }

        void Systems()
        {
            var box = Window("SHIP SYSTEMS", 6, 62, 150);
            Slider(box, "Thrust", 70, 36);
            Slider(box, "Shields", 40, 36);
            Check(box, "Autopilot", true);
            Check(box, "Cloak", false);
            Dropdown(box, 0, "Cruise", "Combat", "Stealth");
            Input(box, "Callsign");
            var buttons = Row(box);
            foreach (var t in new[] { "Abort", "Engage" })
                Expand(Button(buttons, t));
        }

        void Cargo()
        {
            var box = Window("CARGO HOLD", 164, 28, 258);
            var pages = Tabs(box, "Items", "Crew");
            var items = Grid(pages[0], 9, 2);
            var stock = new[] { ("fuel", 3), ("battery", 2), ("crystal", 12), ("chip", 4), ("keycard", 1), ("health", 5),
                ("laser", 1), ("rocket", 2), ("cargo", 6), ("wrench", 1), ("satellite", 0), ("asteroid", 9), ("radar", 0),
                ("robot", 0), ("alien", 0), ("star", 0), ("energy", 0), ("planet", 0) }
                .Where(p => Has(p.Item1)).Take(16).ToArray();
            for (int i = 0; i < 18; i++)
            {
                var slot = Make("InsetPanel", items);
                if (i >= stock.Length)
                    continue;
                var (name, count) = stock[i];
                Tip(Icon(slot, name), Title(name));
                var pad = slot.GetComponent<VerticalLayoutGroup>().padding;
                if (count > 1)
                    Over(slot, count.ToString(), new Vector2(pad.left + 19, pad.top + 10), true, null, Color.black);
            }
            foreach (var (icon, text) in new[] { ("astronaut", "Cmdr. Vega"), ("robot", "Unit K-9") })
            {
                var r = Row(pages[1]);
                Icon(r, icon);
                Expand(Label(r, text));
                Button(r, "Assign");
            }
            var hold = Row(box);
            Icon(hold, "cargo");
            Label(hold, "Hold 60%");
            var fill = Bar(hold, "", 60, 0, 8);
            Expand(fill);
            Centre((RectTransform)fill.transform);
            var actions = Row(box);
            foreach (var (icon, text) in new[] { ("check", "Use"), ("trash", "Jettison"), ("info", "Scan") })
                Expand(Button(actions, text, icon));
        }

        void Target()
        {
            var frame = Place(Make(theme.Has("BracketPanel") ? "BracketPanel" : "Panel", screen), 100, 4);
            MinSize(frame, 56, 52);
            frame.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            Icon(frame, "reticle_lock", 1, Sprite(sprites, "reticle_lock"));
            Label(frame, "LOCKED").Text.alignment = TMPro.TextAlignmentOptions.Top;
        }

        void Comms()
        {
            var panel = Place(Make("Panel", screen), 6, 187);
            MinSize(panel, 416);
            var r = Row(panel, 6);
            Icon(Make("InsetPanel", r), "astronaut");
            Expand(Label(r, "Incoming transmission from Outpost 7.\nDock at bay 3 and mind the asteroids."));
            Centre((RectTransform)Button(r, "", "arrow_right").transform);
        }
    }
}
