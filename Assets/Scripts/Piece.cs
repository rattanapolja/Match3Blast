using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Piece : MonoBehaviour
{
    [SerializeField] private Button m_PieceBtn;

    public PieceData ItemData;

    private Action<Piece> m_OnPieceClicked;

    private void Awake()
    {
        m_PieceBtn.onClick.AddListener(OnPieceClicked);
    }

    public void Initialize(PieceType type, int row, int column, Color color, Action<Piece> onPieceClicked)
    {
        ItemData = new PieceData
        {
            Row = row,
            Column = column,
            PieceColor = color,
            Type = type
        };
        m_PieceBtn.image.color = color;
        m_OnPieceClicked = onPieceClicked;
    }

    public void SetColor(Color color)
    {
        ItemData.PieceColor = color;
        m_PieceBtn.image.color = color;
    }

    public void SetPosition(int row, int column)
    {
        ItemData.Row = row;
        ItemData.Column = column;
    }

    public void MoveTo(Vector2 targetPosition, float duration)
    {
        transform.DOLocalMove(targetPosition, duration);
    }

    private void OnPieceClicked()
    {
        m_OnPieceClicked?.Invoke(this);
    }
}

public struct PieceData
{
    public int Row;
    public int Column;
    public Color PieceColor;
    public PieceType Type;
}

