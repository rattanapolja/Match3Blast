using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GridManager : MonoBehaviour
{
    [Header("Piece Template")]
    [SerializeField] private GameObject m_CornerPrefab;
    [SerializeField] private GameObject m_PiecePrefab;
    [SerializeField] private GameObject m_BombPrefab;
    [SerializeField] private GameObject m_DiscoPrefab;

    [Header("Grid Setting")]
    [SerializeField] private Transform m_PieceContent;
    [SerializeField] private Transform m_CornerContent;
    [SerializeField] private int m_Rows = 8;
    [SerializeField] private int m_Columns = 8;
    [SerializeField] private float m_FallDuration = 0.5f;
    [SerializeField] private Color[] m_AvailableColors;

    [Header("UI")]
    [SerializeField] private TMP_Text m_ScoreText;
    [SerializeField] private Button m_RestartBtn;

    private Piece[,] m_Grid;
    private Vector2 m_StartPosition;
    private float m_PieceSize;
    private float m_GridWidth;
    private float m_GridHeight;
    private int m_CurrentScore;

    private void Start()
    {
        m_RestartBtn.onClick.AddListener(() =>
        {
            SceneManager.LoadSceneAsync(0);
        });
        AdjustGridSize();
        GenerateGrid();
    }

    private void AdjustGridSize()
    {
        RectTransform canvasRect = m_PieceContent.GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        m_PieceSize = Mathf.Min(canvasWidth / m_Rows, canvasHeight / m_Columns);
        m_GridWidth = m_Rows * m_PieceSize;
        m_GridHeight = m_Columns * m_PieceSize;
        m_StartPosition = new Vector2(-(m_GridWidth / 2) + (m_PieceSize / 2), (m_GridHeight / 2) - (m_PieceSize / 2));

        m_PiecePrefab.GetComponent<RectTransform>().sizeDelta = new Vector2(m_PieceSize - 10, m_PieceSize - 10);
        m_BombPrefab.GetComponent<RectTransform>().sizeDelta = new Vector2(m_PieceSize - 10, m_PieceSize - 10);
        m_DiscoPrefab.GetComponent<RectTransform>().sizeDelta = new Vector2(m_PieceSize - 10, m_PieceSize - 10);
        m_CornerPrefab.GetComponent<RectTransform>().sizeDelta = new Vector2(m_PieceSize, m_PieceSize);
    }

    private void GenerateGrid()
    {
        m_Grid = new Piece[m_Rows, m_Columns];
        for (int r = 0; r < m_Rows; r++)
        {
            for (int c = 0; c < m_Columns; c++)
            {
                SpawnPiece(r, c);
            }
        }

        StartCoroutine(EnsureInitialMatches());
    }

    private IEnumerator EnsureInitialMatches()
    {
        for (int r = 0; r < m_Rows; r++)
        {
            for (int c = 0; c < m_Columns; c++)
            {
                List<Piece> matches;
                do
                {
                    m_Grid[r, c].SetColor(GetRandomColor());
                    yield return matches = FindMatchingPieces(m_Grid[r, c]);
                }
                while (matches.Count < 3);
            }
        }
    }

    private Piece SpawnPiece(int row, int column)
    {
        Vector2 position = CalculatePiecePosition(row, column);

        GameObject corner = Instantiate(m_CornerPrefab, m_CornerContent);
        corner.GetComponent<RectTransform>().anchoredPosition = position;

        GameObject pieceObj = ObjectPool.Instance.GetFromPool(m_PiecePrefab.name);
        pieceObj.transform.SetParent(m_PieceContent, false);
        pieceObj.GetComponent<RectTransform>().anchoredPosition = position;
        pieceObj.SetActive(true);

        Piece pieceComponent = pieceObj.GetComponent<Piece>();
        pieceComponent.Initialize(PieceType.Normal, row, column, GetRandomColor(), HandleNormalPieceClick);
        m_Grid[row, column] = pieceComponent;
        return pieceComponent;
    }

    private void ReSpawnPiece(int row, int column)
    {
        GameObject pieceObj = ObjectPool.Instance.GetFromPool(m_PiecePrefab.name);
        pieceObj.transform.SetParent(m_PieceContent, false);
        pieceObj.GetComponent<RectTransform>().anchoredPosition =
            new Vector2(m_StartPosition.x + row * m_PieceSize, m_PieceSize * (m_Columns + 1));

        Piece pieceComponent = pieceObj.GetComponent<Piece>();
        pieceComponent.Initialize(PieceType.Normal, row, column, GetRandomColor(), HandleNormalPieceClick);

        m_Grid[row, column] = pieceComponent;
        m_Grid[row, column].MoveTo(CalculatePiecePosition(row, column), m_FallDuration);
    }

    private Vector2 CalculatePiecePosition(int row, int column)
    {
        float x = m_StartPosition.x + row * m_PieceSize;
        float y = m_StartPosition.y - column * m_PieceSize;

        return new Vector2(x, y);
    }

    private Color GetRandomColor()
    {
        return m_AvailableColors[Random.Range(1, m_AvailableColors.Length)];
    }

    public void HandleNormalPieceClick(Piece piece)
    {
        List<Piece> matchingPieces = FindMatchingPieces(piece);
        if (matchingPieces.Count >= 3)
        {
            DestroyPieces(matchingPieces, piece);
        }
    }

    public void HandleBombPieceClick(Piece piece)
    {
        int row = piece.ItemData.Row;
        int column = piece.ItemData.Column;
        List<Piece> piecesToDestroy = new List<Piece>();

        for (int r = 0; r < m_Rows; r++)
        {
            if (m_Grid[r, column] != null)
                piecesToDestroy.Add(m_Grid[r, column]);
        }
        for (int c = 0; c < m_Columns; c++)
        {
            if (m_Grid[row, c] != null && c != column)
                piecesToDestroy.Add(m_Grid[row, c]);
        }

        DestroyPieces(piecesToDestroy, piece);
    }

    public void HandleDiscoPieceClick(Piece piece)
    {
        Color color = piece.ItemData.PieceColor;
        List<Piece> piecesToDestroy = new List<Piece>();

        for (int r = 0; r < m_Rows; r++)
        {
            for (int c = 0; c < m_Columns; c++)
            {
                if (m_Grid[r, c] != null && m_Grid[r, c].ItemData.PieceColor == color)
                {
                    piecesToDestroy.Add(m_Grid[r, c]);
                }
            }
        }

        DestroyPieces(piecesToDestroy, piece);
    }

    private List<Piece> FindMatchingPieces(Piece piece)
    {
        List<Piece> matchingPieces = new List<Piece>();
        Queue<Piece> piecesToCheck = new Queue<Piece>();
        HashSet<Piece> checkedPieces = new HashSet<Piece>();

        piecesToCheck.Enqueue(piece);
        checkedPieces.Add(piece);

        while (piecesToCheck.Count > 0)
        {
            Piece currentPiece = piecesToCheck.Dequeue();
            matchingPieces.Add(currentPiece);

            foreach (Piece neighbor in GetNeighbors(currentPiece))
            {
                if (neighbor != null && neighbor.ItemData.PieceColor == piece.ItemData.PieceColor && !checkedPieces.Contains(neighbor))
                {
                    piecesToCheck.Enqueue(neighbor);
                    checkedPieces.Add(neighbor);
                }
            }
        }

        return matchingPieces;
    }

    private List<Piece> GetNeighbors(Piece piece)
    {
        List<Piece> neighbors = new List<Piece>();
        int row = piece.ItemData.Row;
        int column = piece.ItemData.Column;

        if (row > 0) neighbors.Add(m_Grid[row - 1, column]);
        if (row < m_Rows - 1) neighbors.Add(m_Grid[row + 1, column]);
        if (column > 0) neighbors.Add(m_Grid[row, column - 1]);
        if (column < m_Columns - 1) neighbors.Add(m_Grid[row, column + 1]);

        return neighbors;
    }

    private void DestroyPieces(List<Piece> pieces, Piece selectPiece)
    {
        int pieceCount = pieces.Count;
        m_CurrentScore += (int)(pieceCount * 1.2f);
        m_ScoreText.text = $"Score: {m_CurrentScore}";

        PieceData currentData = selectPiece.ItemData;
        Color temp = selectPiece.ItemData.PieceColor;

        foreach (var piece in pieces)
        {
            m_Grid[piece.ItemData.Row, piece.ItemData.Column] = null;
            ObjectPool.Instance.ReturnToPool(piece.gameObject);
        }

        if (currentData.Type == PieceType.Normal)
        {
            if (pieceCount >= 10)
            {
                CreateSpecialPiece(currentData, m_DiscoPrefab, PieceType.Disco, temp);
            }
            else if (pieceCount >= 6)
            {
                CreateSpecialPiece(currentData, m_BombPrefab, PieceType.Bomb, m_AvailableColors[0]);
            }
        }

        FallPieces();
    }

    private void CreateSpecialPiece(PieceData selectPiece, GameObject prefab, PieceType type, Color color)
    {
        GameObject specialPieceObj = ObjectPool.Instance.GetFromPool(prefab.name);
        specialPieceObj.transform.SetParent(m_PieceContent, false);
        specialPieceObj.GetComponent<RectTransform>().anchoredPosition =
            new Vector2(m_StartPosition.x + selectPiece.Row * m_PieceSize, m_PieceSize * (m_Columns + 1));
        Piece specialPieceComponent = specialPieceObj.GetComponent<Piece>();

        if (type == PieceType.Disco)
        {
            specialPieceComponent.Initialize(type, selectPiece.Row, selectPiece.Column, color, HandleDiscoPieceClick);
        }
        else if (type == PieceType.Bomb)
        {
            specialPieceComponent.Initialize(type, selectPiece.Row, selectPiece.Column, color, HandleBombPieceClick);
        }

        specialPieceComponent.MoveTo(CalculatePiecePosition(selectPiece.Row, selectPiece.Column), m_FallDuration);
        specialPieceObj.SetActive(true);
        m_Grid[selectPiece.Row, selectPiece.Column] = specialPieceComponent;
    }

    private void FallPieces()
    {
        for (int r = 0; r < m_Rows; r++)
        {
            for (int c = m_Columns - 1; c >= 0; c--)
            {
                if (m_Grid[r, c] == null)
                {
                    for (int cc = c - 1; cc >= 0; cc--)
                    {
                        if (m_Grid[r, cc] != null)
                        {
                            Piece fallingPiece = m_Grid[r, cc];
                            fallingPiece.SetPosition(r,c);
                            fallingPiece.MoveTo(CalculatePiecePosition(r, c), m_FallDuration);
                            m_Grid[r, c] = fallingPiece;
                            m_Grid[r, cc] = null;
                            break;
                        }
                    }
                }
            }
        }
        FillEmptySpaces();
    }

    private void FillEmptySpaces()
    {
        for (int c = 0; c < m_Columns; c++)
        {
            for (int r = 0; r < m_Rows; r++)
            {
                if (m_Grid[r, c] == null)
                {
                    ReSpawnPiece(r, c);
                }
            }
        }
    }
}