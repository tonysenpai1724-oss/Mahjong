using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public Dictionary<Vector2Int, ENodeType> dicNodeGenerated;
    public Vector2Int startPos, endPos;
    public int mazeSize;
    public List<Vector2Int> generatedPath;
    public Dictionary<EDirection, Vector2Int> dicDirections = new Dictionary<EDirection, Vector2Int>()
    {
        { EDirection.Up, Vector2Int.up },
        { EDirection.Down, Vector2Int.down },
        { EDirection.Left, Vector2Int.left },
        { EDirection.Right, Vector2Int.right }
    };
    public void GenerateMaze(int mazeSize, float monsterRate = 20, float treasureRate = 5)
    {
        this.mazeSize = mazeSize;
        if (mazeSize < 3)
        {
            Debug.LogError("Maze size must be greater than 3");
            return;
        }
        GenerateMap();
    }
    [Button()]
    public void GenerateMap()
    {
        dicNodeGenerated = new Dictionary<Vector2Int, ENodeType>();
        for (int x = 0; x < mazeSize; x++)
        {
            for (int y = 0; y < mazeSize; y++)
            {
                dicNodeGenerated.Add(new Vector2Int(x, y), ENodeType.Wall);
            }
        }
        startPos = new Vector2Int(Random.Range(0, mazeSize), Random.Range(0, mazeSize));
        dicNodeGenerated[startPos] = ENodeType.Start;
        while (endPos == startPos)
        {
            endPos = new Vector2Int(Random.Range(0, mazeSize), Random.Range(0, mazeSize));
        }
        GenerateFarestPath();
        AddValidNode();
    }
    [Button()]
    public void GenerateFarestPath()
    {
        List<List<Vector2Int>> lstPaths = new List<List<Vector2Int>>();
        List<EDirection> lstValidDir = dicDirections.Keys.ToList();
        for (int i = 0; i < mazeSize * 2; i++)
        {
            lstPaths.Add(new List<Vector2Int>() { startPos });
        }
        int count = 0;
        while (count < mazeSize * mazeSize * mazeSize * 3)
        {
            count++;
            foreach (var path in lstPaths)
            {
                lstValidDir.Clear();
                Vector2Int lastNode = path.Last();
                foreach (var direction in dicDirections)
                {
                    Vector2Int nextNode = lastNode + direction.Value;
                    if (ValidNode(nextNode, path, lastNode))
                    {
                        lstValidDir.Add(direction.Key);
                    }
                }
                if (lstValidDir.Count > 0)
                {
                    EDirection randomDir = lstValidDir[Random.Range(0, lstValidDir.Count)];
                    path.Add(lastNode + dicDirections[randomDir]);
                    count++;
                }
            }
        }
        generatedPath = lstPaths.OrderByDescending(x => x.Count).First();
        endPos = generatedPath.Last();
        dicNodeGenerated[endPos] = ENodeType.End;
        foreach (var node in generatedPath)
        {
            if (dicNodeGenerated[node] == ENodeType.Wall)
                dicNodeGenerated[node] = ENodeType.Empty;
        }
        DebugCustom.Log("Generated path: " + generatedPath.Count);
    }
    [Button()]
    void AddValidNode()
    {
        int count = 0;

        List<EDirection> lstValidDir = new List<EDirection>();
        while (count < mazeSize * mazeSize * mazeSize)
        {
            count++;
            lstValidDir.Clear();
            Vector2Int lastNode = generatedPath.GetRandom();
            foreach (var direction in dicDirections)
            {
                Vector2Int nextNode = lastNode + direction.Value;
                if (ValidNode(nextNode, generatedPath, lastNode))
                {
                    lstValidDir.Add(direction.Key);
                }
            }
            if (lstValidDir.Count > 0)
            {
                EDirection randomDir = lstValidDir[Random.Range(0, lstValidDir.Count)];
                generatedPath.Add(lastNode + dicDirections[randomDir]);
                dicNodeGenerated[lastNode + dicDirections[randomDir]] = ENodeType.Empty;
                count++;
            }
        }
    }
    bool ValidNode(Vector2Int node, List<Vector2Int> path, Vector2Int lastNode)
    {
        if (node.x < 0 || node.x >= mazeSize || node.y < 0 || node.y >= mazeSize)
        {
            return false;
        }
        foreach (var item in path)
        {
            if (item != lastNode)
            {
                if (Mathf.Abs(node.x - item.x) + Mathf.Abs(node.y - item.y) <= 1)
                    return false;
            }
        }
        return true;
    }
    float offsetTime = 0.05f;
    private void Update()
    {
        offsetTime -= Time.deltaTime;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) && offsetTime <= 0)
        {
            Move(EDirection.Left);
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) && offsetTime <= 0)
        {
            Move(EDirection.Right);
        }
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) && offsetTime <= 0)
        {
            Move(EDirection.Up);
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) && offsetTime <= 0)
        {
            Move(EDirection.Down);
        }
    }
    void Move(EDirection direction)
    {
        if (dicNodeGenerated.ContainsKey(startPos + dicDirections[direction]))
        {
            if(dicNodeGenerated[startPos + dicDirections[direction]] == ENodeType.Empty)
            {
                dicNodeGenerated[startPos] = ENodeType.Empty;
                startPos += dicDirections[direction];
                dicNodeGenerated[startPos] = ENodeType.Start;
            }
            offsetTime = 0.1f;
        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (dicNodeGenerated == null)
            return;
        foreach (var node in dicNodeGenerated)
        {
            Gizmos.color = Color.black;
            if (node.Value == ENodeType.Empty)
            {
                Gizmos.color = Color.white;
            }
            if (node.Value == ENodeType.Start)
            {
                Gizmos.color = Color.green;
            }
            if (node.Value == ENodeType.End)
            {
                Gizmos.color = Color.red;
            }
            if (node.Value == ENodeType.Monster)
            {
                Gizmos.color = Color.blue;
            }
            if (node.Value == ENodeType.Treasure)
            {
                Gizmos.color = Color.yellow;
            }
            Gizmos.DrawCube(new Vector3(node.Key.x, node.Key.y, 0), Vector3.one);
        }
    }
#endif
}
