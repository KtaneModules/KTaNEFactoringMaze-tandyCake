using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class FactoringMazeScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] buttons;
    public Material[] buttonMats;
    public Material solveColor;
    private Color[] textColors = new Color[] { new Color(0, 0, 0), new Color(1, 1, 1) };

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    private int[] primes = new int[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29 };
    string[] maze = new string[16].Select(x => x = string.Empty).ToArray(); //Makes an array of 16 empty's
    int[] primeGrid = new int[16];
    char[] loggingMaze = "• • • • •         • • • • •         • • • • •         • • • • •         • • • • •".ToCharArray();
    int[] chosenPrimes;

    int curPos;
    int endPos;

    int[] offsets = new int[] { 4, 1, -4, -1 };
    string directions = "ULDR";
    string[] directionNames = new string[] { "Up", "Left", "Down", "Right" };
    string[] nodeNames = new string[] { "A1", "B1", "C1", "D1", "A2", "B2", "C2", "D2", "A3", "B3", "C3", "D3", "A4", "B4", "C4", "D4" };

    int currentNode;
    bool[] visited = new bool[16];
    Stack<int> visitOrder = new Stack<int>();
    int startingNode;

    void Awake () {
        moduleId = moduleIdCounter++;

        foreach (KMSelectable thinmgy in buttons) 
            thinmgy.OnInteract += delegate () { Press(thinmgy); return false; };
    }

    void Press(KMSelectable button)
    {
        button.AddInteractionPunch(0.25f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        if (moduleSolved)
            return;
        int offset = curPos - Array.IndexOf(buttons, button);
        if (GetAdjacents(curPos).Contains(Array.IndexOf(buttons, button)))
        {
            char dirPressed = directions[Array.IndexOf(offsets, offset)];
            if (maze[curPos].Contains(dirPressed))
            {
                Debug.LogFormat("[Factoring Maze #{0}] You moved {1} from {2}.", moduleId, directionNames[Array.IndexOf(offsets, offset)], nodeNames[curPos]);
                SetColors(curPos, 0);
                curPos = Array.IndexOf(buttons, button);
                SetColors(curPos, 1);
                if (curPos == endPos) StartCoroutine(Solve());
            }
            else
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[Factoring Maze #{0}] You tried to move {1} from {2}. Strike!", moduleId, directionNames[Array.IndexOf(offsets, offset)], nodeNames[curPos]);
            }
        }
    }

    void Start ()
    {
        GenerateMaze();
        LogMaze();
        GetStartEnd();
        GetPrimes();
        PlacePrimes();
        LogPrimes();
        Debug.LogFormat("[Factoring Maze #{0}] The shortest possible path is {1}.", moduleId, FindPath(curPos, endPos));
    }

    void GetStartEnd()
    {
        if (Bomb.GetSerialNumberNumbers().All(x => x == 0)) endPos = 0;
        else endPos = (Product(Bomb.GetSerialNumberNumbers()) - 1) % 16;
        do curPos = UnityEngine.Random.Range(0, 16);
        while (FindPath(curPos, endPos).Length < 3);
        SetColors(curPos, 1); //Set start pos to white.
        Debug.LogFormat("[Factoring Maze #{0}] Your starting position is in row {1} and column {2}.", moduleId, curPos / 4 + 1, curPos % 4 + 1);
        Debug.LogFormat("[Factoring Maze #{0}] Your goal's position is in row {1} and column {2}.", moduleId, endPos / 4 + 1, endPos % 4 + 1);
    }

    List<int> GetAdjacents(int position)
    {
        List<int> adjacents = new List<int>();
        if (position % 4 != 0) adjacents.Add(position - 1);
        if (position % 4 != 3) adjacents.Add(position + 1);
        if (position > 3) adjacents.Add(position - 4);
        if (position < 12) adjacents.Add(position + 4);
        return adjacents;
    }

    void SetColors(int position, int color)
    {
        buttons[position].GetComponent<MeshRenderer>().material = buttonMats[color];
        buttons[position].GetComponentInChildren<TextMesh>().color = textColors[1 - color];
    }

    IEnumerator Solve()
    {
        SetColors(curPos, 0);
        moduleSolved = true;
        GetComponent<KMBombModule>().HandlePass();
        Debug.LogFormat("[Factoring Maze #{0}] Goal position reached. Module solved!", moduleId);
        bool unicorn = UnityEngine.Random.Range(0, 100) == 0;
        for (int i = 0; i < 16; i++)
        {
            buttons[i].GetComponentInChildren<TextMesh>().text = unicorn ? "HOW" : "✓";
            buttons[i].GetComponentInChildren<TextMesh>().characterSize = unicorn ? 3.5f : 7;
            yield return new WaitForSecondsRealtime(0.1f);
        }
        yield return new WaitForSecondsRealtime(0.2f);
        for (int i = 0; i < 16; i++)
            buttons[i].GetComponent<MeshRenderer>().material = unicorn ? buttonMats[0] : solveColor;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
    }

    string AddNewline(string input)
    {
        return (input.Length > 3) ? input.Insert(3, "\n") : input;
    }
    float GetSize(string input)
    {
        switch (input.Length)
        {
            case 1: return 7; 
            case 2: return 5.5f;
            case 3: return 4;
            default: return 3;
        }
    }
    int Product(IEnumerable<int> inputs)
    {
        int value = 1;
        foreach (int number in inputs)  
            if (number != 0) 
                value *= number;
        return value;
    }

    void GetPrimes()
    {
        chosenPrimes = primes.Shuffle().Take(4).OrderBy(x => x).ToArray();
        Debug.LogFormat("[Factoring Maze #{0}] The primes chosen by the module are {1}", moduleId, chosenPrimes.Join());
    }
    
    void PlacePrimes()
    {
        for (int i = 0; i < 16; i++)
        {
            int value = 1;
            for (int j = 0; j < 4; j++)
                if (!maze[i].Contains(directions[j]))
                    value *= chosenPrimes[j];
            primeGrid[i] = value;
            buttons[i].GetComponentInChildren<TextMesh>().text = AddNewline(value.ToString());
            buttons[i].GetComponentInChildren<TextMesh>().characterSize = GetSize(value.ToString());
        }
    }

    void LogPrimes()
    {
        Debug.LogFormat("[Factoring Maze #{0}] The grid of primes is as follows:", moduleId);
        List<string> temp = new List<string>();
        for (int i = 0; i < 16; i++)
        {
            temp.Add(primeGrid[i].ToString());
            if ((i % 4) == 3)
            {
                Debug.LogFormat("[Factoring Maze #{0}] {1}", moduleId, temp.Join(" "));
                temp.Clear();
            }
        }
    }
    void LogMaze()
    {
        Debug.LogFormat("[Factoring Maze #{0}] The generated with its walls is as follows:", moduleId);
        for (int i = 0; i < 16; i++)
        {
            int pointer = 2 * i + 10 * (i / 4 + 1);
            if (!maze[i].Contains('U')) loggingMaze[pointer - 9] = '─';
            if (!maze[i].Contains('L')) loggingMaze[pointer - 1] = '│';
            if (!maze[i].Contains('D')) loggingMaze[pointer + 9] = '─';
            if (!maze[i].Contains('R')) loggingMaze[pointer + 1] = '│';
        }
        string ToBeLogged = "";
        for (int i = 0; i < loggingMaze.Length; i++)
        {
            ToBeLogged += loggingMaze[i];
            if (ToBeLogged.Length == 9)
            {
                Debug.LogFormat("[Factoring Maze #{0}] {1}", moduleId, ToBeLogged);
                ToBeLogged = "";
            }
        }
    }

    void GenerateMaze()
    {
        startingNode = UnityEngine.Random.Range(0, 16);
        currentNode = startingNode;
        do
        {
            visitOrder.Push(currentNode);
            visited[currentNode] = true;
            while (GetAdjacents(currentNode).All(x => visited[x]))
                if (visitOrder.Count() != 0)
                    currentNode = visitOrder.Pop();
                else goto End; //Goto moment :ultratrolled:   // Escapes from the outer do-while loop.
            int chosenDestination = GetAdjacents(currentNode).Where(x => !visited[x]).PickRandom();
            int dir = Array.IndexOf(offsets, currentNode - chosenDestination);
            maze[currentNode] += directions[dir];
            maze[chosenDestination] += directions[(dir + 2) % 4];
            currentNode = chosenDestination;
        } while (currentNode != startingNode);
        End:;
    }

    string FindPath(int start, int end)
    {
        if (start == end)
            return string.Empty;
        Queue<int> q = new Queue<int>();
        List<Movement> allMoves = new List<Movement>();
        q.Enqueue(start);
        while (q.Count > 0)
        {
            int subject = q.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                if (maze[subject].Contains(directions[i]) && !allMoves.Any(x => x.start == subject - offsets[i]))
                {
                    q.Enqueue(subject - offsets[i]);
                    allMoves.Add(new Movement(subject, subject - offsets[i], i));
                }
            }
            if (subject == end) break;
        }
        if (allMoves.Count != 0)
        {
            Debug.LogFormat("<Factoring Maze #{0}> Pathfinder path: {1}", moduleId, allMoves.Join(", "));
            Movement lastMove = allMoves.First(x => x.end == end);
            List<Movement> path = new List<Movement>() { lastMove };
            while (lastMove.start != start)
            {
                lastMove = allMoves.First(x => x.end == lastMove.start);
                path.Add(lastMove);
            }
            path.Reverse();
            string solution = string.Empty;
            for (int i = 0; i < path.Count; i++)
                solution += path[i].direction;
            return solution;
        }
        else return string.Empty;
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} move ULDR to move in those directions.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        string[] parameters = Command.Trim().ToUpperInvariant().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (Regex.IsMatch(Command, @"^\s*move\s+[ULDR]+\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            int pos = curPos;
            List<int> presses = new List<int>();
            foreach (char letter in parameters[1].ToUpperInvariant())
            {
                int dirNum = directions.IndexOf(letter);
                if (!GetAdjacents(pos).Contains(pos - offsets[dirNum]))
                {
                    yield return "sendtochaterror Command left bounds of maze, command aborted.";
                    yield break;
                }
                else
                    presses.Add(pos -= offsets[dirNum]);
            }
            foreach (int value in presses)
            {
                buttons[value].OnInteract();
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            string command = FindPath(curPos, endPos);
            foreach (char movement in command)
            {
                buttons[curPos - offsets[directions.IndexOf(movement)]].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}

public class Movement
{
    public int start;
    public int end;
    public char direction;
    public Movement(int s, int e, int d)
    {
        start = s;
        end = e;
        direction = "ULDR"[d];
    }
    public override string ToString()
    {
        return string.Format("({0}, {1}, {2})", start, end, direction); //Here for debugging purposes
    }
}