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
    private int? _firstCorrectSubmitted;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        Module.OnActivate = Initialization;
    }

    private void Initialization()
    {
        _clues = new List<Clue>();

        // Add the size indication
        var sizeSystem = Rnd.Range(0, 5);
        var primes = new[] { 3, 5, 7 };
        var size = Enumerable.Range(3, 5).SelectMany(width => Enumerable.Range(3, 5).Select(height => new { Width = width, Height = height }))
            .Where(sz => sizeSystem != 0 || (primes.Contains(sz.Width) && primes.Contains(sz.Height)))
            .PickRandom();
        Clue clue = null;
        switch (sizeSystem)
        {
            case 0: clue = new Clue((size.Width > size.Height ? "{0}" : size.Width < size.Height ? "({0})" : Rnd.Range(0, 2) == 0 ? "{0}" : "({0})").Fmt(size.Width * size.Height), false, false, 128); break;
            case 1: clue = new Clue("{0}×{1}".Fmt(size.Width, size.Height), false, false, 128, null, "{0}x{1}".Fmt(size.Width, size.Height)); break;
            case 2: clue = new Clue("{1} by {0}".Fmt(size.Width, size.Height), false, false, 128); break;
            case 3: clue = new Clue("{0}*{1}".Fmt(size.Width * size.Height, size.Height), false, false, 128); break;
            case 4: clue = new Clue("{0} : {1}".Fmt(size.Width * size.Height, size.Width), false, false, 128); break;
        }
        _clues.Add(clue);
        Debug.LogFormat(@"[Coordinates #{3}] Showing grid size {0}×{1} as {2}", size.Width, size.Height, clue.LoggingText, _moduleId);

        var coordCh = 'a';
        var grid = new char[size.Width * size.Height];
        for (int i = 0; i < size.Width * size.Height; i++)
            grid[i] = '.';

        var coordinates = Enumerable.Range(0, size.Width * size.Height).ToList();
        coordinates.Shuffle();
        var illegalCoords = new List<int>();
        var num = 0;

        // Generate 6 illegal coordinates
        for (; num < 6; num++)
        {
            illegalCoords.Add(coordinates[num]);
            clue = addClue(false, coordinates[num], size.Width, size.Height);
            Debug.LogFormat(@"[Coordinates #{3}] Showing illegal coordinate {0}={1} as {2}", coordCh, loggingCoords(coordinates[num], size.Width), clue.LoggingText, _moduleId);
            grid[coordinates[num]] = coordCh;
            coordCh++;
        }

        // Generate the correct coordinate twice with different coordinate systems
        clue = addClue(true, coordinates[num], size.Width, size.Height);
        Debug.LogFormat(@"[Coordinates #{0}] Showing correct coordinate *={1} as {2}", _moduleId, loggingCoords(coordinates[num], size.Width), clue.LoggingText);
        clue = addClue(true, coordinates[num], size.Width, size.Height, avoidSystem: clue.System);
        Debug.LogFormat(@"[Coordinates #{0}] Showing correct coordinate *={1} as {2}", _moduleId, loggingCoords(coordinates[num], size.Width), clue.LoggingText);
        grid[coordinates[num]] = '*';

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
        _firstCorrectSubmitted = null;

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

            if (_clues[_selectedIndex].IsCorrect && (_firstCorrectSubmitted == null || _firstCorrectSubmitted == _selectedIndex))
            {
                Debug.LogFormat("[Coordinates #{0}] Pressed submit button on {1}: first correct answer.", _moduleId, _clues[_selectedIndex].LoggingText);
                _firstCorrectSubmitted = _selectedIndex;
            }
            else if (_clues[_selectedIndex].IsCorrect)
            {
                Debug.LogFormat("[Coordinates #{0}] Pressed submit button on {1}: second correct answer. Module solved.", _moduleId, _clues[_selectedIndex].LoggingText);
                Module.HandlePass();
                _clues = null;
            }
            else
            {
                Debug.LogFormat("[Coordinates #{0}] Pressed submit button on wrong answer {1}.", _moduleId, _clues[_selectedIndex].LoggingText);
                Module.HandleStrike();
            }

            return false;
        };

        UpdateDisplay(true);
    }

    private string loggingCoords(int coord, int width)
    {
        return char.ConvertFromUtf32(coord % width + 'A') + (coord / width + 1);
    }

    const float _buttonAnimationDurationIn = .1f;
    const float _buttonAnimationDurationOut = .25f;
    const float _textAnimationDuration = .4f;

    private IEnumerator ButtonAnimation(KMSelectable btn)
    {
        var transform = btn.transform;
        var x = transform.localPosition.x;
        var z = transform.localPosition.z;

        var elapsed = 0f;
        while (elapsed < _buttonAnimationDurationIn)
        {
            yield return null;
            elapsed += Time.deltaTime;
            transform.localPosition = new Vector3(x, 0.005f + 0.01f * (1 - elapsed / _buttonAnimationDurationIn), z);
        }
        transform.localPosition = new Vector3(x, 0.005f, z);

        yield return new WaitForSeconds(.05f);

        elapsed = 0f;
        while (elapsed < _buttonAnimationDurationOut)
        {
            yield return null;
            elapsed += Time.deltaTime;
            transform.localPosition = new Vector3(x, 0.005f + 0.01f * (elapsed / _buttonAnimationDurationOut), z);
        }
        transform.localPosition = new Vector3(x, 0.015f, z);
    }

    private IEnumerator TextAnimation(TextMesh oldText, TextMesh newText, bool up)
    {
        var color = oldText.color;

        var elapsed = 0f;
        while (elapsed < _textAnimationDuration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            var i = (elapsed / _textAnimationDuration) * 3;
            oldText.color = new Color(color.r, color.g, color.b, i > 2 ? 0 : (2 - i) / 2);
            newText.color = new Color(color.r, color.g, color.b, i < 1 ? 0 : (i - 1) / 2);
        }
        oldText.color = new Color(color.r, color.g, color.b, 0);
        newText.color = new Color(color.r, color.g, color.b, 1);
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

    private Clue addClue(bool isCorrect, int coord, int width, int height, int? avoidSystem = null)
    {
        var x = coord % width;
        var y = coord / width;

        // System 0 is clockface, which we can’t use if width and height are both even
        tryAgain:
        var system = width % 2 == 0 && height % 2 == 0 ? Rnd.Range(1, 15) : Rnd.Range(0, 15);
        if (system == avoidSystem)
            goto tryAgain;

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
                _clues.Add(new Clue(s, isCorrect, false, system == 0 && dx == 0 && dy == 0 ? 92 : 64, system, s.Contains('’') ? s.Replace("’", "'") : null));
                break;

            case 3: _clues.Add(new Clue("[{0},{1}]".Fmt(x, y), isCorrect, false, 128, system)); break;
            case 4: _clues.Add(new Clue("{0}{1}".Fmt((char) ('A' + x), y + 1), isCorrect, false, 128, system)); break;
            case 5: _clues.Add(new Clue("<{1}, {0}>".Fmt(x, y), isCorrect, false, 128, system)); break;
            case 6: _clues.Add(new Clue("{1}, {0}".Fmt(x + 1, y + 1), isCorrect, false, 128, system)); break;
            case 7: _clues.Add(new Clue("({0},{1})".Fmt(x, height - 1 - y), isCorrect, false, 128, system)); break;
            case 8: _clues.Add(new Clue("{0}-{1}".Fmt((char) ('A' + x), height - y), isCorrect, false, 128, system)); break;
            case 9: _clues.Add(new Clue("“{1}, {0}”".Fmt(x, height - 1 - y), isCorrect, false, 128, system, @"""{1}, {0}""".Fmt(x, height - 1 - y))); break;
            case 10: _clues.Add(new Clue("{1}/{0}".Fmt(x + 1, height - y), isCorrect, false, 128, system)); break;
            case 11: _clues.Add(new Clue("[{0}]".Fmt(coord), isCorrect, false, 128, system)); break;
            case 12: _clues.Add(new Clue(ordinal(coord + 1), isCorrect, false, 128, system)); break;
            case 13: _clues.Add(new Clue("#{0}".Fmt((height - 1 - y) * width + x + 1), isCorrect, false, 128, system)); break;

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
                _clues.Add(new Clue(zh, isCorrect, true, 128, system, "Chinese {0}".Fmt(origZhIx)));
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

    private string twitchSimplify(string str)
    {
        var origStr = str;
        str = str
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace(",", "")
            .Replace("north", "n")
            .Replace("south", "s")
            .Replace("west", "w")
            .Replace("east", "e")
            .Replace("up", "u")
            .Replace("down", "d")
            .Replace("left", "l")
            .Replace("right", "r")
            .Replace("top", "t")
            .Replace("bottom", "b")
            .Replace("middle", "m")
            .Replace("center", "c")
            .Replace("from", "");
        Debug.LogFormat(@"simplified ""{0}"" to ""{1}"".", origStr, str);
        return str;
    }

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (_clues == null)
            yield break;

        var stuff = command.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

        if (stuff.Length == 1 && stuff[0].Equals("cycle", StringComparison.InvariantCultureIgnoreCase))
        {
            for (int i = 0; i < _clues.Count; i++)
            {
                yield return new WaitForSeconds(_clues[_selectedIndex].Text.Length > 8 ? 2.7f : 1.9f);
                yield return Right;
                yield return new WaitForSeconds(.1f);
                yield return Right;
            }
        }
        else if (stuff.Length == 2 && stuff[0].Equals("submit", StringComparison.InvariantCultureIgnoreCase))
        {
            for (int i = 0; i < _clues.Count; i++)
            {
                if (i > 0)
                {
                    yield return Right;
                    yield return new WaitForSeconds(.1f);
                    yield return Right;
                }

                if (twitchSimplify(_clues[_selectedIndex].Text).StartsWith(twitchSimplify(stuff[1]), StringComparison.InvariantCultureIgnoreCase) ||
                    (_clues[_selectedIndex].AltText != null && twitchSimplify(_clues[_selectedIndex].AltText).StartsWith(twitchSimplify(stuff[1]), StringComparison.InvariantCultureIgnoreCase)))
                {
                    yield return Submit;
                    yield return new WaitForSeconds(.1f);
                    yield return Submit;
                    yield break;
                }
            }
        }
    }
}
