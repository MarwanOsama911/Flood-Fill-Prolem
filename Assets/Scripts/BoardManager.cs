using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [SerializeField] private int columnCount, rowCount;
    [SerializeField] private List<Tile> tilesPrefab;
    [SerializeField] private Transform tileOffset;
    [SerializeField] private Transform tilesParent;
    
    public bool IsBoardUpdating { get; private set; }

    private int _horizontalMatches = 1;
    private Tile[,] _tiles;
    private int _gridSize;
    
    #region BoardManager Singleton

    public static BoardManager Instance { get; private set; }

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        }
    }

    #endregion
    
    private void Start() {
        _gridSize = columnCount * rowCount;
        _tiles = new Tile[columnCount, rowCount];
        CreateBoard(columnCount, rowCount);
    }
    
    #region Board Creation
    private void CreateBoard(int cols, int rows) {
        
        var previousLeft = new Tile[cols];

        for (var x = 0; x < cols; x++) {
            for (var y = 0; y < rows; y++) {
                
                //avoid to have multiple of the same tiles nearest to each other in a row
                var possibleTiles = new List<Tile>();
                possibleTiles.AddRange(tilesPrefab);
                possibleTiles.Remove(previousLeft[y]);

                var tileTemp = possibleTiles[Random.Range(0, possibleTiles.Count)];

                previousLeft[y] = tileTemp;

                var tile = Instantiate(tileTemp, tilesParent, true);
                tile.transform.position = GridToWorldCoordinates(x, y);
                tile.hideFlags = HideFlags.HideInHierarchy; //keeping the hierarchy clean
                tile.ColNumber = x;
                tile.RowNumber = y;
                _tiles[x, y] = tile;
            }
        }
    }

    #endregion
    
    #region Board Utilis

    public Vector3 WorldToGridCoordinates(Vector3 point) {
        var position = tileOffset.position;
        var gridPoint = new Vector3((int) ((point.x - position.x) / _gridSize),
            (int) ((point.y - position.y) / _gridSize), 0f);
        return gridPoint;
    }

    public Vector3 GridToWorldCoordinates(int cols, int rows) {
        var position = tileOffset.position;
        var worldPoint = new Vector3(position.x + cols, position.y + rows, position.z);
        return worldPoint;
    }

    #endregion
    
    #region board Update

        public async Task UpdateBoard(Tile tileToRemove) {
            IsBoardUpdating = true;
            _tiles[tileToRemove.ColNumber, tileToRemove.RowNumber] = null;
            DestroyImmediate(tileToRemove.gameObject);
            await ShiftTilesDown(tileToRemove.ColNumber, tileToRemove.RowNumber);
            await FindMatchedTiles();
            IsBoardUpdating = false;
        }
        
        private async Task UpdateBoard() {
            
            for (int x = 0; x < columnCount; x++) {
                for (int y = 0; y < rowCount; y++) {
                    if (_tiles[x, y] == null) {
                        continue;
                    }

                    if (_tiles[x, y].IsMatched) {
                        DestroyImmediate(_tiles[x, y]?.gameObject);
                        _tiles[x, y] = null;
                    }
                }
            }

            await FindNullTiles();
        }
        
        private async Task FindNullTiles() {
            
            for (int x = 0; x < columnCount; x++) {
                for (int y = 0; y < rowCount; y++) {
                    if (_tiles[x, y] == null) {
                        await ShiftTilesDown(x, y);
                        break;
                    }
                }
            }
        }
        
        private async Task ShiftTilesDown(int x, int yStart) {
            
            for (int y = yStart; y < rowCount - 1; y++) {
                _tiles[x, y] = _tiles[x, y + 1];

                if (_tiles[x, y] == null) {
                    continue;
                }

                _tiles[x, y].RowNumber--;
            }

            _tiles[x, rowCount - 1] = null;
            UpdateTilesPosition();
            await Task.CompletedTask;
        }
        private void UpdateTilesPosition() {
            
            for (int col = 0; col < columnCount; col++) {
                for (int row = 0; row < rowCount; row++) {
                    _tiles[col, row]?.UpdatePosition();
                }
            }
            
        }

        #endregion

    #region Matching Tiles
    //preforming Flood fill algorithm technique 
    private async Task FindMatchedTiles() {
        
        for (int x = 0; x < columnCount - 1; x++) {
            for (int y = 0; y < rowCount - 1; y++) {
                if (_tiles[x, y] != null) {
                    CheckIfMatch(x, y);
                }
            }
        }

        await Task.CompletedTask;
        await UpdateBoard();
    }
    private void CheckIfMatch(int row, int col) {
        
        CheckIfLeftMatch(row, col);
        CheckIfRightMatch(row, col);

        if (_horizontalMatches >= 3) {
            
            for (int x = 0; x < columnCount; x++) {
                for (int y = 0; y < rowCount; y++) {
                    if (_tiles[x, y] != null && _tiles[x, y].IsChecked) {
                        _tiles[x, y]. IsMatched = true;
                    }
                }
            }
        }
        else {
            for (int x = 0; x < columnCount; x++) {
                for (int y = 0; y < rowCount; y++) {
                    if (_tiles[x, y] != null && _tiles[x, y].IsChecked && !_tiles[x, y].IsMatched) {
                        _tiles[x, y].IsChecked = false;
                    }
                }
            }
        }
        _horizontalMatches = 1;
    }
    private void CheckIfLeftMatch(int x, int y) {
        
        //recursive
        Tile toCheck = _tiles[x, y];

        if (toCheck == null) {
            return;
        }

        toCheck.IsChecked= true;

        if (toCheck.ColNumber - 1 <= 0) {
            //if prev col doesnt exist
            return;
        }

        if (_tiles[x - 1, y] != null && IsEqual(_tiles[x - 1, y].TileColor, _tiles[x, y].TileColor)) {
            _horizontalMatches++;
            CheckIfLeftMatch(toCheck.ColNumber - 1, toCheck.RowNumber);
        }
    }
    private void CheckIfRightMatch(int x, int y) {
        
        //recursive
        Tile toCheck = _tiles[x, y];

        if (toCheck == null) {
            return;
        }

        toCheck.IsChecked = true;

        if (toCheck.ColNumber + 1 >= columnCount) {
            //if next col doesnt exist
            return;
        }

        if (_tiles[x + 1, y] != null && IsEqual(_tiles[x + 1, y].TileColor, _tiles[x, y].TileColor)) {
            _horizontalMatches++;
            CheckIfRightMatch(toCheck.ColNumber + 1, toCheck.RowNumber);
        }
    }
    private bool IsEqual(Color tileColor1, Color tileColor2) {
        return (tileColor1.r == tileColor2.r && tileColor1.b == tileColor2.b && tileColor1.g == tileColor2.g);
    }
    #endregion
}