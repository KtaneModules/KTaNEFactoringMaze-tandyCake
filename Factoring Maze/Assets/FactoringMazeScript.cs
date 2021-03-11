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
    int curRow;
    int curCol;

    int endPos;
    int endRow;
    int endCol;

    int[] offsets = new int[] { 4, 1, -4, -1 };
    string directions = "ULDR";
    string[] directionNames = new string[] { "Up", "Left", "Down", "Right" };
    string[] nodeNames = new string[] { "A1", "B1", "C1", "D1", "A2", "B2", "C2", "D2", "A3", "B3", "C3", "D3", "A4", "B4", "C4", "D4" };

    int currentNode;
    bool[] visited = new bool[16];
    int backtrackCount = 0;
    List<int> visitOrder = new List<int>();
    int startingNode;

    void Awake () {
        moduleId = moduleIdCounter++;

        foreach (KMSelectable thinmgy in buttons) 
        {
            thinmgy.OnInteract += delegate () { Press(thinmgy); return false; };
        }


    }

    void Press(KMSelectable button)
    {
        if (moduleSolved)
        {
            return;
        }
        int offset = curPos - Array.IndexOf(buttons, button);
        if (GetAdjacents(curPos).Contains(Array.IndexOf(buttons, button)))
        {
            char dirPressed = directions[Array.IndexOf(offsets, offset)];
            if (maze[curPos].Contains(dirPressed))
            {
                Debug.LogFormat("[Factoring Maze #{0}] You moved {1} from {2}.", moduleId, directionNames[Array.IndexOf(offsets, offset)], nodeNames[curPos]);
                SetColors(curPos, 0);
                SetCurPos(Array.IndexOf(buttons, button));
                SetColors(curPos, 1);
                if (curPos == endPos)
                {
                    StartCoroutine(Solve());
                }
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
        //maze = new string[] { "RD", "LR", "LR", "L", "UD", "RD", "LR", "L", "UR", "ULD", "RD", "L", "R", "ULR", "ULR", "L"};
        GenerateMaze();
        GetEndPos();
        GetStartPos();
        for (int i = 0; i < 16; i++)
        {
            buttons[i].GetComponentInChildren<TextMesh>().text = maze[i];
        }
        GetPrimes();
        //PlacePrimes();
        //LogPrimes();
        LogMaze();
        Debug.Log(FindPath(curPos, endPos));
    }

    void GetStartPos()
    {
        curPos = UnityEngine.Random.Range(0, 16);
        curRow = curPos / 4;
        curCol = curPos % 4;
        Debug.LogFormat("[Factoring Maze #{0}] Your starting position is in row {1} and column {2}.", moduleId, curRow + 1, curCol + 1);
        SetColors(curPos, 1); //Set start pos to white.
    }

    void GetEndPos()
    {
        if (Bomb.GetSerialNumberNumbers().All(x => x == 0)) endPos = 0;
        else endPos = (Product(Bomb.GetSerialNumberNumbers().ToArray()) - 1) % 16;
        endRow = endPos / 4;
        endCol = endPos % 4;
        Debug.LogFormat("[Factoring Maze #{0}] Your goal's position is in row {1} and column {2}.", moduleId, endRow + 1, endCol + 1);
    }

    void SetCurPos(int position)
    {
        curPos = position;
        curRow = position / 4;
        curCol = position % 4;
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
        moduleSolved = true;
        GetComponent<KMBombModule>().HandlePass();
        Debug.LogFormat("[Factoring Maze #{0}] Goal position reached. Module solved!", moduleId);
        yield return null;
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
    int Product(int[] inputs)
    {
        int value = 1;
        foreach (int number in inputs)
        {
            if (number != 0) value *= number;
        }
        return value;
    }

    void GetPrimes()
    {
        primes.Shuffle();
        List<int> temp = new List<int>();
        temp = primes.Take(4).ToList();
        temp.Sort();
        chosenPrimes = temp.ToArray();
        Debug.LogFormat("[Factoring Maze #{0}] The primes chosen by the module are {1}", moduleId, chosenPrimes.Join());
    }
    
    void PlacePrimes()
    {
        for (int i = 0; i < 16; i++)
        {
            int value = 1;
            for (int j = 0; j < 4; j++)
            {
                if (!maze[i].Contains(directions[j]))
                {
                    value *= chosenPrimes[j];
                }
            }
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
            int pointer = (2 * i) + 10 * (i / 4 + 1);
            if (!maze[i].Contains('U')) loggingMaze[pointer - 9] = '─';
            if (!maze[i].Contains('L')) loggingMaze[pointer - 1] = '│';
            if (!maze[i].Contains('D')) loggingMaze[pointer + 9] = '─';
            if (!maze[i].Contains('R')) loggingMaze[pointer + 1] = '│';
        }
        List<char> ToBeLogged = new List<char>();
        for (int i = 0; i < loggingMaze.Length; i++)
        {
            ToBeLogged.Add(loggingMaze[i]);
            if ((i % 9) == 8)
            {
                Debug.LogFormat("[Factoring Maze #{0}] {1}", moduleId, ToBeLogged.Join(""));
                ToBeLogged.Clear();
            }
        }
    }

    void GenerateMaze()
    {
        startingNode = UnityEngine.Random.Range(0, 16);
        int chosenDestination = 0;
        currentNode = startingNode;

        int iterations = 0;
        while ((currentNode != startingNode) || (iterations == 0))
        {
            if (iterations > 1000)
            {
                Debug.Log("uuuuuuuu maze machine broke");
                Debug.Log(visited.Join(" "));
                Solve();
                break;
            }
            visited[currentNode] = true;
            visitOrder.Add(currentNode);
            if (CheckAdjacents(currentNode))
            {
                if (currentNode == startingNode && iterations != 0)
                {
                    break;
                }
                Debug.Log(currentNode);
                Debug.Log(visited.Join());
                Debug.Log(visitOrder.Join());
                chosenDestination = GetAdjacents(currentNode).Where(x => !visited[x]).PickRandom();
                int direction = Array.IndexOf(offsets, currentNode - chosenDestination);
                if (!maze[currentNode].Contains(directions[direction])) // If there's no passage here, make one.
                {
                    maze[currentNode] += directions[direction];
                }
                if (!maze[chosenDestination].Contains(directions[(direction + 2) % 4])) // Carves a passage from destination to the start again.
                {
                    maze[chosenDestination] += directions[(direction + 2) % 4];
                }
            }

            for (int i = 0; i < 16; i++)
            {
                buttons[i].GetComponentInChildren<TextMesh>().text = maze[i];
            }
            visitOrder.Add(currentNode);
            currentNode = chosenDestination;
            iterations++;
        }
    }

    string FindPath(int start, int end)
    {
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
            {
                solution += path[i].direction;
            }

            return solution;
        }
        else return string.Empty;
    }

    bool CheckAdjacents(int input)
    {
        List<int> adjacents = GetAdjacents(input);
        if (adjacents.Where(x => !visited[x]).Count() == 0)
        {
            backtrackCount++;
            currentNode = visitOrder[visitOrder.Count() - 1 - backtrackCount];
            if (currentNode == startingNode)
            {
                return true;
            }
            return CheckAdjacents(currentNode);
        }
        else return true;

    }


    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} move ULDR to move in those directions.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        string[] parameters = Command.Trim().ToUpperInvariant().Split(' ');
        if (Regex.IsMatch(Command, @"^\s*move\s+[ULDR]+\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            int pos = curPos;
            List<int> presses = new List<int>();
            foreach (char letter in parameters[1])
            {
                int dirNum = Array.IndexOf(directions.ToCharArray(), letter);
                if (!GetAdjacents(pos).Contains(pos - offsets[dirNum]))
                {
                    yield return "sendtochaterror " + "Command left bounds of maze, command aborted.";
                    yield break;
                }
                else 
                {
                    presses.Add(pos - offsets[dirNum]);
                    pos = pos - offsets[dirNum];
                }
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
      yield return null;
    }
}

public class Movement
{
    public int start { get; set; }
    public int end { get; set; }
    public char direction { get; set; }

    public Movement(int s, int e, int d)
    {
        start = s;
        end = e;
        direction = "ULDR"[d];
    }
    public string ToString()
    {
        return string.Format("({0}, {1}, {2})", start, end, direction); //Here for debugging purposes
    }
}