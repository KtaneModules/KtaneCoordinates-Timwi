using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Coordinates;
using UnityEngine;

using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Coordinates
/// Created by Timwi
/// </summary>
public class CoordinatesModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;

    public Font Chinese, NonChinese;
    public Material ChineseFontMat, NonChineseFontMat;
    public KMSelectable Left, Submit, Right;
    public TextMesh Text1, Text2;
    public MeshRenderer Text1Renderer, Text2Renderer;

    private bool _currentTextIs1;
    private List<Clue> _clues;
    private int _selectedIndex;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        Module.OnActivate = Initialization;
    }

    private void Initialization()
    {
        var numbers = new List<int>();
        numbers.AddRange(Bomb.GetSerialNumberNumbers());

        var indicatorTable = new Dictionary<string, int> { { "BOB", 5 }, { "FRQ", 2 }, { "SIG", 4 }, { "CAR", 4 }, { "IND", 0 }, { "SND", 1 }, { "CLR", 2 }, { "MSA", 3 }, { "TRN", 3 }, { "FRK", 1 }, { "NSA", 0 } };
        numbers.AddRange(Bomb.GetIndicators().OrderBy(x => x).Select(ind => indicatorTable[ind] + (Bomb.IsIndicatorOn(ind) ? 1 : 0)));

        var portTable = new Dictionary<string, int> { { "DVI", 5 }, { "Parallel", 2 }, { "PS2", 0 }, { "RJ45", 3 }, { "Serial", 1 }, { "StereoRCA", 4 } };
        numbers.AddRange(Bomb.GetPorts().OrderBy(x => x).Select(port => portTable[port]));
        numbers.Add(2);

        Debug.LogFormat(@"[Coordinates #{1}] List of numbers: {0}", numbers.JoinString(", "), _moduleId);

        _clues = new List<Clue>();
        var size = Enumerable.Range(2, 7).SelectMany(width => Enumerable.Range(2, 7).Select(height => new { Width = width, Height = height }))
            .Where(sz => sz.Width * sz.Height > numbers.Count)
            .PickRandom();

        // Add the size indication
        var primes = new[] { 2, 3, 5, 7 };
        Clue clue = null;
        switch (Rnd.Range(primes.Contains(size.Width) && primes.Contains(size.Height) ? 0 : 1, 5))
        {
            case 0: clue = new Clue((size.Width > size.Height ? "{0}" : size.Width < size.Height ? "({0})" : Rnd.Range(0, 2) == 0 ? "{0}" : "({0})").Fmt(size.Width * size.Height), false, false, 128); break;
            case 1: clue = new Clue("{0}×{1}".Fmt(size.Width, size.Height), false, false, 128, "{0}x{1}".Fmt(size.Width, size.Height)); break;
            case 2: clue = new Clue("{1} by {0}".Fmt(size.Width, size.Height), false, false, 128); break;
            case 3: clue = new Clue("{0}*{1}".Fmt(size.Width * size.Height, size.Height), false, false, 128); break;
            case 4: clue = new Clue("{0} : {1}".Fmt(size.Width * size.Height, size.Width), false, false, 128); break;
        }
        _clues.Add(clue);
        Debug.LogFormat(@"[Coordinates #{4}] Showing grid size {0}×{1} as {2}{3}", size.Width, size.Height, clue.Text, clue.LoggingExtra == null ? null : " ({0})".Fmt(clue.LoggingExtra), _moduleId);

        var coordinates = Enumerable.Range(0, size.Width * size.Height).ToList();
        var illegalCoords = new List<int>();
        var ix = 0;
        foreach (var num in numbers)
        {
            ix = (ix + num) % coordinates.Count;
            illegalCoords.Add(coordinates[ix]);
            coordinates.RemoveAt(ix);
        }

        var coordCh = 'A';
        var grid = new char[size.Width * size.Height];
        for (int i = 0; i < size.Width * size.Height; i++)
            grid[i] = '.';

        // Add up to 6 illegal coordinates
        for (int i = Math.Min(6, illegalCoords.Count); i >= 1; i--)
        {
            var icIx = Rnd.Range(0, illegalCoords.Count);
            var illegalCoord = illegalCoords[icIx];
            illegalCoords.RemoveAt(icIx);
            clue = addClue(false, illegalCoord, size.Width, size.Height);
            Debug.LogFormat(@"[Coordinates #{5}] Showing illegal coordinate {0}=[{1}, {2}] as {3}{4}", coordCh, illegalCoord % size.Width, illegalCoord / size.Width, clue.Text.Replace("\n", " "), clue.LoggingExtra == null ? null : " ({0})".Fmt(clue.LoggingExtra), _moduleId);
            grid[illegalCoord] = coordCh;
            coordCh++;
        }

        // Add one of the legal coordinates
        var correctCoordinate = coordinates.PickRandom();
        clue = addClue(true, correctCoordinate, size.Width, size.Height);
        Debug.LogFormat(@"[Coordinates #{4}] Showing correct coordinate *=[{1}, {2}] as {0}{3}", clue.Text.Replace("\n", " "), correctCoordinate % size.Width, correctCoordinate / size.Width, clue.LoggingExtra == null ? null : " ({0})".Fmt(clue.LoggingExtra), _moduleId);
        grid[correctCoordinate] = '*';

        // Log the grid
        Debug.LogFormat("[Coordinates #{0}] Grid:\n{1}", _moduleId, Enumerable.Range(0, size.Height).Select(row =>
            (grid[size.Width * row] == '*' ? "" : " ") +
            Enumerable.Range(0, size.Width)
                .Select(col => new { Char = grid[col + size.Width * row], Col = col, Coord = col + size.Width * row })
                .Select(inf => inf.Char == '*' ? "[*]" : (inf.Char != '.' ? inf.Char : illegalCoords.Contains(inf.Coord) ? '#' : '.') + (inf.Col == size.Width - 1 || grid[inf.Col + 1 + size.Width * row] == '*' ? "" : " "))
                .JoinString())
            .JoinString("\n"));

        _clues.Shuffle();

        _selectedIndex = 0;
        Left.OnInteract = delegate
        {
            Left.AddInteractionPunch(.5f);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Left.transform);
            StartCoroutine(ButtonAnimation(Left));
            if (_clues == null)
                return false;

            _selectedIndex = (_selectedIndex + _clues.Count - 1) % _clues.Count;
            UpdateDisplay(false);
            return false;
        };

        Right.OnInteract = delegate
        {
            Right.AddInteractionPunch(.5f);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Right.transform);
            StartCoroutine(ButtonAnimation(Right));
            if (_clues == null)
                return false;

            _selectedIndex = (_selectedIndex + 1) % _clues.Count;
            UpdateDisplay(true);
            return false;
        };

        Submit.OnInteract = delegate
        {
            Submit.AddInteractionPunch(1f);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Submit.transform);
            StartCoroutine(ButtonAnimation(Submit));
            if (_clues == null)
                return false;

            if (_clues[_selectedIndex].IsCorrect)
            {
                Module.HandlePass();
                _clues = null;
            }
            else
            {
                Debug.LogFormat("[Coordinates #{1}] Pressed submit button on wrong answer {0}.", _clues[_selectedIndex].Text, _moduleId);
                Module.HandleStrike();
            }

            return false;
        };

        UpdateDisplay(true);
    }

    private IEnumerator ButtonAnimation(KMSelectable btn)
    {
        var transform = btn.transform;
        var x = transform.localPosition.x;
        var z = transform.localPosition.z;
        for (int i = 0; i <= 5; i++)
        {
            yield return null;
            transform.localPosition = new Vector3(x, 0.005f + 2 * (5 - i) * 0.001f, z);
        }
        yield return new WaitForSeconds(.05f);
        for (int i = 0; i <= 10; i++)
        {
            yield return null;
            transform.localPosition = new Vector3(x, 0.005f + i * 0.001f, z);
        }
    }

    private IEnumerator TextAnimation(TextMesh oldText, TextMesh newText, bool up)
    {
        var color = oldText.color;
        var n = 18;
        for (int i = 0; i <= n; i++)
        {
            oldText.color = new Color(color.r, color.g, color.b, i > 2 * n / 3 ? 0f : (2 * n / 3 - i) / (float) (2 * n / 3));
            newText.color = new Color(color.r, color.g, color.b, i < n / 3 ? 0f : (i - n / 3) / (float) (2 * n / 3));
            yield return null;
        }
    }

    private void UpdateDisplay(bool up)
    {
        var oldText = _currentTextIs1 ? Text1 : Text2;
        var newText = _currentTextIs1 ? Text2 : Text1;
        var newRenderer = _currentTextIs1 ? Text2Renderer : Text1Renderer;

        newText.text = _clues[_selectedIndex].Text;
        newText.font = _clues[_selectedIndex].IsChinese ? Chinese : NonChinese;
        newText.fontSize = _clues[_selectedIndex].FontSize;
        newRenderer.material = _clues[_selectedIndex].IsChinese ? ChineseFontMat : NonChineseFontMat;

        StartCoroutine(TextAnimation(oldText, newText, up));
        _currentTextIs1 = !_currentTextIs1;
    }

    private Clue addClue(bool isCorrect, int coord, int width, int height)
    {
        var x = coord % width;
        var y = coord / width;

        // System 0 is clockface, which we can’t use if width and height are both even
        var system = width % 2 == 0 && height % 2 == 0 ? Rnd.Range(1, 15) : Rnd.Range(0, 15);

        switch (system)
        {
            case 0:
            case 1:
            case 2:
                var nearestLocation = Ut.NewArray(
                    system == 0 ? null : new { Index = 0, X = 0, Y = 0 },
                    width % 2 == 0 ? null : new { Index = 1, X = width / 2, Y = 0 },
                    system == 0 ? null : new { Index = 2, X = width - 1, Y = 0 },
                    height % 2 == 0 ? null : new { Index = 3, X = 0, Y = height / 2 },
                    system == 0 || width % 2 == 0 || height % 2 == 0 ? null : new { Index = 4, X = width / 2, Y = height / 2 },
                    height % 2 == 0 ? null : new { Index = 5, X = width - 1, Y = height / 2 },
                    system == 0 ? null : new { Index = 6, X = 0, Y = height - 1 },
                    width % 2 == 0 ? null : new { Index = 7, X = width / 2, Y = height - 1 },
                    system == 0 ? null : new { Index = 8, X = width - 1, Y = height - 1 }
                )
                    .Where(inf => inf != null).MinElements(inf => Math.Abs(inf.X - x) + Math.Abs(inf.Y - y)).PickRandom();

                var dx = x - nearestLocation.X;
                var dy = y - nearestLocation.Y;
                var s = "";
                if (dx != 0)
                    s += "{0} {1}".Fmt(Math.Abs(dx), (dx > 0 ? new[] { "right", "east" } : new[] { "left", "west" }).PickRandom());
                if (dx != 0 && dy != 0)
                    s += ", ";
                if (dy != 0)
                    s += "{0} {1}".Fmt(Math.Abs(dy), (dy > 0 ? new[] { "down", "south" } : new[] { "up", "north" }).PickRandom());
                if (dx != 0 || dy != 0)
                    s += " from\n";
                s += Ut.NewArray(
                    new[] { null, "12 o’clock", null, "9 o’clock", null, "3 o’clock", null, "6 o’clock" },
                    new[] { "north-west corner", "north center", "north-east corner", "west center", "center", "east center", "south-west corner", "south center", "south-east corner" },
                    new[] { "top left", "top middle", "top right", "middle left", "middle center", "middle right", "bottom left", "bottom center", "bottom right" }
                )[system][nearestLocation.Index];
                _clues.Add(new Clue(s, isCorrect, false, system == 0 && dx == 0 && dy == 0 ? 92 : 64));
                break;

            case 3: _clues.Add(new Clue("[{0},{1}]".Fmt(x, y), isCorrect, false, 128)); break;
            case 4: _clues.Add(new Clue("{0}{1}".Fmt((char) ('A' + x), y + 1), isCorrect, false, 128)); break;
            case 5: _clues.Add(new Clue("<{1}, {0}>".Fmt(x, y), isCorrect, false, 128)); break;
            case 6: _clues.Add(new Clue("{1}, {0}".Fmt(x + 1, y + 1), isCorrect, false, 128)); break;
            case 7: _clues.Add(new Clue("({0},{1})".Fmt(x, height - 1 - y), isCorrect, false, 128)); break;
            case 8: _clues.Add(new Clue("{0}-{1}".Fmt((char) ('A' + x), height - y), isCorrect, false, 128)); break;
            case 9: _clues.Add(new Clue("“{1}, {0}”".Fmt(x, height - 1 - y), isCorrect, false, 128)); break;
            case 10: _clues.Add(new Clue("{1}/{0}".Fmt(x + 1, height - y), isCorrect, false, 128)); break;
            case 11: _clues.Add(new Clue("[{0}]".Fmt(coord), isCorrect, false, 128)); break;
            case 12: _clues.Add(new Clue(ordinal(coord + 1), isCorrect, false, 128)); break;
            case 13: _clues.Add(new Clue("#{0}".Fmt((height - 1 - y) * width + x + 1), isCorrect, false, 128)); break;

            case 14:    // Chinese!
                var zhIx = (width - 1 - x) * height + y + 1;
                var origZhIx = zhIx;
                var zh = "";
                if (zhIx % 10 != 0)
                    zh += "?一二三四五六七八九"[zhIx % 10];
                zhIx /= 10;
                if (zhIx != 0)
                {
                    zh = "十" + zh;
                    if (zhIx > 1)
                        zh = "??二三四五六七八九"[zhIx] + zh;
                }
                _clues.Add(new Clue(zh, isCorrect, true, 128, "{0} in Chinese".Fmt(origZhIx)));
                break;
        }
        return _clues.Last();
    }

    private string ordinal(int n)
    {
        if ((n / 10) % 10 == 1)
            return n + "th";
        switch (n % 10)
        {
            case 1: return n + "st";
            case 2: return n + "nd";
            case 3: return n + "rd";
            default: return n + "th";
        }
    }
}
