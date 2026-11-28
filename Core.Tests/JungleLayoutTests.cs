using System;
using System.Collections.Generic;
using System.IO;
using WobblyMods.Core.Shapes;
using Xunit;

/// <summary>
/// The jungle on the top floor of the big aquarium can't be seen from the cloud, so these tests stand in for
/// eyes: trees fit under the glass, paths stay open, the treehouse's parts meet.
/// </summary>
public class JungleLayoutTests
{
    private const float Body = 0.45f; // about how wide a Wobbly is, from its middle

    [Fact]
    public void SixtyTreesFitAndAreTheSameEveryTime()
    {
        List<TreeSpot> a = JungleLayout.Trees(60), b = JungleLayout.Trees(60);
        Assert.Equal(60, a.Count);
        for (int i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i].X, b[i].X);
            Assert.Equal(a[i].Scale, b[i].Scale);
        }
        Assert.Equal(2, JungleLayout.Trees(0).Count);   // the treehouse always keeps its two trees
        Assert.InRange(JungleLayout.Trees(80).Count, 60, 80);
    }

    [Fact]
    public void EveryTreeIsInsideTheGlassAndOnFreeGround()
    {
        List<TreeSpot> trees = JungleLayout.Trees(60);
        for (int i = 0; i < trees.Count; i++)
        {
            TreeSpot t = trees[i];
            Assert.InRange(t.Scale, JungleLayout.SmallestTree, 1.26f);
            if (i >= 2) Assert.True(JungleLayout.Free(t.X, t.Z), "tree " + i + " stands on a path");
            foreach (V3 p in RainforestTree.Points(t.Variant))
            {
                V3 q = JungleLayout.Turn(p * t.Scale, t.Yaw);
                float x = t.X + q.X, z = t.Z + q.Z;
                Assert.True(Math.Abs(x) < JungleLayout.HalfX - 0.3f && Math.Abs(z) < JungleLayout.HalfZ - 0.3f, "tree " + i + " pokes through a glass wall");
                Assert.True(q.Y < JungleLayout.Roof(x, z) - 0.2f, "tree " + i + " pokes through the glass roof");
            }
            for (int k = 0; k < i; k++)
            {
                float d = (float)Math.Sqrt(Math.Pow(trees[k].X - t.X, 2) + Math.Pow(trees[k].Z - t.Z, 2));
                Assert.True(d >= JungleLayout.Spacing * 0.75f - 0.01f, "trees " + k + " and " + i + " are only " + d + " m apart");
            }
        }
    }

    [Fact]
    public void GiantsInTheMiddleSmallerTreesAtTheGlass()
    {
        List<TreeSpot> trees = JungleLayout.Trees(60);
        int giants = 0;
        foreach (TreeSpot t in trees) if (t.Scale >= 0.8f) giants++;
        Assert.True(giants >= 25, "only " + giants + " of the trees are full size");
    }

    [Fact]
    public void TheHutSitsBelowTheLeaves()
    {
        List<TreeSpot> trees = JungleLayout.Trees(60);
        for (int i = 0; i < 2; i++)
        {
            float leaves = RainforestTree.CanopyBottom(trees[i].Variant) * trees[i].Scale;
            Assert.True(leaves >= JungleLayout.HutRoofTop, "treehouse tree " + i + ": leaves start at " + leaves + " m, the hut roof tops out at " + JungleLayout.HutRoofTop);
        }
    }

    [Fact]
    public void TheRampMeetsTheFloorAndThePlatform()
    {
        TreehousePlan plan = JungleLayout.Treehouse();
        V3 foot = plan.Ramp.TopEdge(-1, 0), head = plan.Ramp.TopEdge(1, 0);
        Assert.InRange(foot.Y, -0.02f, 0.02f);
        Assert.InRange(head.Y, JungleLayout.Deck - 0.02f, JungleLayout.Deck + 0.02f);
        Assert.InRange(head.X, plan.DeckA.Center.X - plan.DeckA.Size.X / 2f - 0.03f, plan.DeckA.Center.X - plan.DeckA.Size.X / 2f + 0.03f);
        Assert.InRange(plan.Ramp.TiltZ, 8f, 15.5f);   // the building's own ramps are 14 degrees, and those are known to be walkable
    }

    [Fact]
    public void TheBridgeJoinsTheTwoPlatformsWithNoStep()
    {
        TreehousePlan plan = JungleLayout.Treehouse();
        V3 a = plan.Bridge1.TopEdge(-1, 0), m1 = plan.Bridge1.TopEdge(1, 0), m2 = plan.Bridge2.TopEdge(-1, 0), b = plan.Bridge2.TopEdge(1, 0);
        Assert.InRange(a.Y, JungleLayout.Deck - 0.02f, JungleLayout.Deck + 0.02f);
        Assert.InRange(b.Y, JungleLayout.Deck - 0.02f, JungleLayout.Deck + 0.02f);
        Assert.InRange(a.X, plan.DeckA.Center.X + plan.DeckA.Size.X / 2f - 0.03f, plan.DeckA.Center.X + plan.DeckA.Size.X / 2f + 0.03f);
        Assert.InRange(b.X, plan.DeckB.Center.X - plan.DeckB.Size.X / 2f - 0.03f, plan.DeckB.Center.X - plan.DeckB.Size.X / 2f + 0.03f);
        Assert.InRange((m1 - m2).Length, 0f, 0.03f);
        Assert.InRange(m1.Y, JungleLayout.Deck - 0.3f, JungleLayout.Deck - 0.1f);
    }

    [Fact]
    public void TheSlideRunsFromThePlatformToJustAboveTheFloor()
    {
        TreehousePlan plan = JungleLayout.Treehouse();
        V3 top = plan.Slide.TopEdge(0, 1), end = plan.Slide.TopEdge(0, -1);
        Assert.InRange(top.Y, JungleLayout.Deck - 0.02f, JungleLayout.Deck + 0.02f);
        Assert.InRange(top.Z, plan.DeckB.Center.Z - plan.DeckB.Size.Z / 2f - 0.03f, plan.DeckB.Center.Z - plan.DeckB.Size.Z / 2f + 0.03f);
        Assert.InRange(end.Y, 0.15f, 0.35f);
        Assert.InRange(-plan.Slide.TiltX, 20f, 32f);
        Assert.InRange(plan.SlideDown.Length, 0.99f, 1.01f);
        // "Down the slide" really does point from the top to the end.
        V3 along = (end - top).Normalized;
        Assert.InRange(V3.Dot(along, plan.SlideDown), 0.999f, 1.001f);
        // The sides stand on the slide, not under it or beside it in mid-air.
        foreach (JungleBox box in plan.Boxes)
        {
            if (box.Name != "slide side") continue;
            V3 foot = box.Point(0f, -box.Size.Y / 2f, 0f);
            V3 onSlide = plan.Slide.Point(0f, plan.Slide.Size.Y / 2f, 0f);
            Assert.InRange(Math.Abs(foot.Y - onSlide.Y), 0f, 0.12f);
        }
    }

    [Fact]
    public void NoRailStandsAcrossAWayOnOrOffThePlatforms()
    {
        TreehousePlan plan = JungleLayout.Treehouse();
        float d = JungleLayout.Deck + 0.6f;
        var ways = new[]
        {
            new V3(plan.DeckA.Center.X - plan.DeckA.Size.X / 2f, d, JungleLayout.TreeAZ),      // top of the ramp
            new V3(plan.DeckA.Center.X + plan.DeckA.Size.X / 2f, d, JungleLayout.TreeAZ),      // onto the bridge
            new V3(plan.DeckB.Center.X - plan.DeckB.Size.X / 2f, d, JungleLayout.TreeBZ),      // off the bridge
            new V3(JungleLayout.TreeBX, d, plan.DeckB.Center.Z - plan.DeckB.Size.Z / 2f),      // onto the slide
        };
        foreach (V3 way in ways)
        {
            foreach (JungleBox box in plan.Boxes)
            {
                if (!box.Solid || box.TiltX != 0f || box.TiltZ != 0f) continue;
                bool inside = Math.Abs(way.X - box.Center.X) < box.Size.X / 2f + Body && Math.Abs(way.Y - box.Center.Y) < box.Size.Y / 2f
                              && Math.Abs(way.Z - box.Center.Z) < box.Size.Z / 2f + Body;
                Assert.False(inside, box.Name + " at " + box.Center.X + "," + box.Center.Z + " blocks the way at " + way.X + "," + way.Z);
            }
        }
    }

    [Fact]
    public void VineGatesStandOnTheFloorClearOfEverything()
    {
        List<TreeSpot> trees = JungleLayout.Trees(60);
        foreach (GateSpot gate in JungleLayout.Gates())
        {
            V3 a, b;
            JungleLayout.GatePosts(gate, out a, out b);
            foreach (V3 post in new[] { a, b })
            {
                Assert.True(Math.Abs(post.X) < JungleLayout.HalfX - 1f && Math.Abs(post.Z) < JungleLayout.HalfZ - 1f);
                Assert.False(Math.Abs(post.X) < JungleLayout.LagoonX + 0.15f && Math.Abs(post.Z) < JungleLayout.LagoonZ + 0.15f, "a gate post stands in the lagoon");
                Assert.False(Math.Abs(Math.Abs(post.X) - 9.5f) < 0.6f && Math.Abs(post.Z) < 2.2f, "a gate post stands in a bench");
                Assert.True(Math.Sqrt(Math.Pow(post.X - 3.2f, 2) + Math.Pow(post.Z - 8.6f, 2)) > 1.0, "a gate post stands in the sign");
                Assert.False(Math.Abs(post.X) < 1.1f && Math.Abs(post.Z - 7.9f) < 0.5f, "a gate post stands in the diving board");
            }
            foreach (TreeSpot t in trees)
                Assert.True(JungleLayout.DistanceToSegment(t.X, t.Z, a.X, a.Z, b.X, b.Z) > 1.2f, "a tree stands in a vine gate");
        }
    }

    /// <summary>Walk the floor (as a grid) from the top of the building's ramp and check every place worth going can be reached.</summary>
    [Theory]
    [InlineData(60)]
    [InlineData(80)]
    [InlineData(30)]
    public void EveryPlaceOnTheFloorCanBeWalkedTo(int treeCount)
    {
        List<TreeSpot> trees = JungleLayout.Trees(treeCount);
        const float cell = 0.25f;
        int n = (int)(52f / cell);
        Func<int, float> at = i => -26f + (i + 0.5f) * cell;
        var blocked = new bool[n, n];
        int open = 0;
        for (int i = 0; i < n; i++)
            for (int k = 0; k < n; k++)
            {
                blocked[i, k] = Blocked(at(i), at(k), trees);
                if (!blocked[i, k]) open++;
            }

        var seen = new bool[n, n];
        var queue = new Queue<int[]>();
        Action<float, float> start = (x, z) =>
        {
            int i = (int)((x + 26f) / cell), k = (int)((z + 26f) / cell);
            Assert.False(blocked[i, k], "the start of the walk is blocked");
            seen[i, k] = true;
            queue.Enqueue(new[] { i, k });
        };
        start(24f, 8.9f); // stepping off the top of the building's ramp
        int reached = 1;
        while (queue.Count > 0)
        {
            int[] c = queue.Dequeue();
            foreach (int[] step in new[] { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } })
            {
                int i = c[0] + step[0], k = c[1] + step[1];
                if (i < 0 || k < 0 || i >= n || k >= n || blocked[i, k] || seen[i, k]) continue;
                seen[i, k] = true;
                reached++;
                queue.Enqueue(new[] { i, k });
            }
        }

        var places = new Dictionary<string, float[]>
        {
            { "where the bounce pad lands you", new[] { -18.5f, 4f } },
            { "the lagoon walk, east", new[] { 7.3f, 0f } },
            { "the lagoon walk, west", new[] { -7.3f, 0f } },
            { "the lagoon walk, back", new[] { 0f, -8.6f } },
            { "the diving board", new[] { -1.8f, 8.7f } },
            { "the frog home", new[] { -16.3f, -15f } },
            { "the lizard home", new[] { 12f, -16.3f } },
            { "the turtle home", new[] { -12f, 16.3f } },
            { "the foot of the treehouse ramp", new[] { -3.5f, 20f } },
            { "the end of the slide", new[] { 20f, 11.4f } },
        };
        foreach (KeyValuePair<string, float[]> place in places)
        {
            int i = (int)((place.Value[0] + 26f) / cell), k = (int)((place.Value[1] + 26f) / cell);
            Assert.True(seen[i, k], "can't walk to " + place.Key + " with " + treeCount + " trees");
        }
        // And no big pockets of floor walled off by trees.
        Assert.True(reached >= open * 0.97f, "only " + reached + " of " + open + " open squares can be walked to");
    }

    /// <summary>Would a Wobbly standing here be inside something solid (or over a hole)?</summary>
    private static bool Blocked(float x, float z, List<TreeSpot> trees)
    {
        if (Math.Abs(x) > 25.9f - Body || Math.Abs(z) > 25.9f - Body) return true;
        if (Math.Abs(x) < JungleLayout.LagoonX + Body && Math.Abs(z) < JungleLayout.LagoonZ + Body) return true;      // the water
        if (x > 22f - 0.1f - Body && z < 8f && z > -8f - 0.1f - Body) return true;                                    // the ramp well and its rails
        if (x > -25.5f - Body && x < -20.3f + Body && z > 1.8f - Body && z < 6.2f + Body) return true;                // the hole above the bounce pad
        if (InBox(x, z, -19f, -15f, 1.5f, 2.5f) || InBox(x, z, 12f, -19f, 2.5f, 1.5f) || InBox(x, z, -12f, 19f, 2.5f, 1.5f)) return true; // animal homes
        if (InBox(x, z, -9.5f, 0f, 0.45f, 2f) || InBox(x, z, 9.5f, 0f, 0.45f, 2f)) return true;                       // benches
        if (InBox(x, z, 0f, 7.9f, 0.9f, 0.3f) || InBox(x, z, 3.2f, 8.6f, 0.6f, 0.3f)) return true;                    // diving board stand, sign
        foreach (TreeSpot t in trees)
        {
            float r = 0.5f * t.Scale + Body;
            if ((t.X - x) * (t.X - x) + (t.Z - z) * (t.Z - z) < r * r) return true;
        }
        // Under the low end of the treehouse ramp and of the slide there is no headroom.
        TreehousePlan plan = JungleLayout.Treehouse();
        V3 foot = plan.Ramp.TopEdge(-1, 0), head = plan.Ramp.TopEdge(1, 0);
        float lowUntil = foot.X + (head.X - foot.X) * (2.2f / JungleLayout.Deck);
        if (x > foot.X - 0.1f && x < lowUntil && Math.Abs(z - foot.Z) < 0.95f + Body) return true;
        V3 top = plan.Slide.TopEdge(0, 1), end = plan.Slide.TopEdge(0, -1);
        float slideLowUntil = end.Z + (top.Z - end.Z) * (2.2f / JungleLayout.Deck);
        if (z > end.Z && z < slideLowUntil && Math.Abs(x - end.X) < 0.8f + Body) return true;
        return false;
    }

    private static bool InBox(float x, float z, float cx, float cz, float halfX, float halfZ)
    {
        return Math.Abs(x - cx) < halfX + Body && Math.Abs(z - cz) < halfZ + Body;
    }

    [Fact]
    public void BoxMeshFacesOutward()
    {
        var box = new JungleBox { Center = new V3(1f, 2f, 3f), Size = new V3(2f, 1f, 4f), TiltX = -25f };
        ShapeMesh mesh = box.ToMesh();
        Assert.Equal(36, mesh.Triangles.Count);
        for (int i = 0; i < mesh.Triangles.Count; i += 3)
        {
            V3 a = mesh.Vertices[mesh.Triangles[i]], b = mesh.Vertices[mesh.Triangles[i + 1]], c = mesh.Vertices[mesh.Triangles[i + 2]];
            V3 normal = V3.Cross(b - a, c - a);
            Assert.True(V3.Dot(normal, (a + b + c) / 3f - box.Center) > 0f);
        }
    }

    [Fact]
    public void WritesFloorPlanPictures()
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "preview", "forest");
        Directory.CreateDirectory(dir);
        V3 one = new V3(1f, 1f, 1f);
        var parts = new List<ColoredMesh>();
        Action<V3, V3, Rgb> box = (center, size, color) => parts.Add(new ColoredMesh(new JungleBox { Center = center, Size = size }.ToMesh(), color));
        box(new V3(0f, -0.1f, 0f), new V3(52f, 0.2f, 52f), new Rgb(140, 107, 71));                     // the floor
        box(new V3(0f, 0.02f, 0f), new V3(12f, 0.1f, 14f), new Rgb(70, 170, 190));                     // the lagoon
        box(new V3(24f, 0.02f, 0f), new V3(4f, 0.1f, 16f), new Rgb(255, 128, 13));                     // the top of the building's ramp
        box(new V3(-22.9f, 0.02f, 4f), new V3(5.2f, 0.1f, 4.4f), new Rgb(40, 40, 40));                 // the hole above the bounce pad
        box(new V3(-19f, 1.25f, -15f), new V3(3f, 2.5f, 5f), new Rgb(170, 220, 230));                  // frog, lizard, turtle homes
        box(new V3(12f, 1.25f, -19f), new V3(5f, 2.5f, 3f), new Rgb(170, 220, 230));
        box(new V3(-12f, 1.25f, 19f), new V3(5f, 2.5f, 3f), new Rgb(170, 220, 230));
        box(new V3(-9.5f, 0.35f, 0f), new V3(0.9f, 0.7f, 4f), new Rgb(107, 77, 51));
        box(new V3(9.5f, 0.35f, 0f), new V3(0.9f, 0.7f, 4f), new Rgb(107, 77, 51));
        box(new V3(0f, 0.45f, 5.8f), new V3(1.6f, 0.14f, 4.4f), new Rgb(255, 128, 13));                // diving board

        var looks = new Dictionary<JungleLook, Rgb>
        {
            { JungleLook.Plank, new Rgb(158, 112, 69) }, { JungleLook.DarkWood, new Rgb(107, 77, 51) },
            { JungleLook.Orange, new Rgb(255, 128, 13) }, { JungleLook.Green, new Rgb(64, 171, 61) }
        };
        TreehousePlan plan = JungleLayout.Treehouse();
        foreach (JungleBox b in plan.Boxes) if (b.Look != JungleLook.Unseen) parts.Add(new ColoredMesh(b.ToMesh(), looks[b.Look]));
        foreach (JungleRope rope in plan.Ropes)
            parts.Add(new ColoredMesh(Shapes.Tube(new List<V3> { rope.From, rope.To }, new List<float> { rope.Radius, rope.Radius }, 5), new Rgb(107, 77, 51)));
        foreach (GateSpot gate in JungleLayout.Gates())
            foreach (ColoredMesh m in RainforestTree.VineGate(gate.Variant, JungleLayout.GateWidth, JungleLayout.GateHeight))
                parts.Add(new ColoredMesh(m.Mesh.Transformed(one, gate.Yaw, 0f, new V3(gate.X, 0f, gate.Z)), m.Color));

        // Without the trees first (so the paths and the treehouse can be seen), then with them.
        Save(Path.Combine(dir, "plan-no-trees.bmp"), parts, 90f, 0f);
        Save(Path.Combine(dir, "treehouse.bmp"), Near(parts, -4f, 24f, 11f, 25f), 38f, 205f);
        var forest = new List<ColoredMesh>(parts);
        foreach (TreeSpot t in JungleLayout.Trees(60))
            foreach (ColoredMesh m in RainforestTree.Build(t.Variant))
                forest.Add(new ColoredMesh(m.Mesh.Transformed(new V3(t.Scale, t.Scale, t.Scale), t.Yaw, 0f, new V3(t.X, 0f, t.Z)), m.Color));
        Save(Path.Combine(dir, "plan.bmp"), forest, 90f, 0f);
        Save(Path.Combine(dir, "jungle.bmp"), forest, 32f, 215f);
        Assert.True(new FileInfo(Path.Combine(dir, "plan.bmp")).Length > 1000);
    }

    /// <summary>Only the parts whose middle lies in this patch of floor (for a close-up).</summary>
    private static List<ColoredMesh> Near(List<ColoredMesh> parts, float x0, float x1, float z0, float z1)
    {
        var list = new List<ColoredMesh>();
        foreach (ColoredMesh p in parts)
        {
            V3 mid = (p.Mesh.Min + p.Mesh.Max) / 2f;
            V3 size = p.Mesh.Max - p.Mesh.Min;
            if (size.X > 30f) continue;
            if (mid.X >= x0 && mid.X <= x1 && mid.Z >= z0 && mid.Z <= z1) list.Add(p);
        }
        return list;
    }

    private static void Save(string path, List<ColoredMesh> parts, float pitch, float yaw)
    {
        V3 one = new V3(1f, 1f, 1f);
        var turned = new List<ColoredMesh>();
        foreach (ColoredMesh p in parts) turned.Add(new ColoredMesh(p.Mesh.Transformed(one, yaw, 0f, V3.Zero).Transformed(one, 0f, pitch, V3.Zero), p.Color));
        Preview.SaveBmp(path, turned, 900, 900, 0f, new Rgb(150, 200, 235));
    }
}
