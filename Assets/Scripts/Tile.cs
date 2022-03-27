using System;
using DG.Tweening;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public Color TileColor { get; private set; }
    public int ColNumber { get; set; }
    public int RowNumber { get; set; }
    public bool IsChecked { get; set; }
    public bool IsMatched { get; set; }

    
    private void Awake() {
        TileColor = GetComponent<Renderer>().material.color;
    }

    private async void OnMouseDown() {
        if (!BoardManager.Instance.IsBoardUpdating) {
            await BoardManager.Instance.UpdateBoard(this);
        }
    }

    public void UpdatePosition() {
       var targetPosition = BoardManager.Instance.GridToWorldCoordinates(ColNumber, RowNumber);
       gameObject.transform.DOMove(targetPosition, 0.4f);
    }
}
