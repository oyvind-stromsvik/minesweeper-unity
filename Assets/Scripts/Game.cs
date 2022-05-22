using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Game : MonoBehaviour {

    public int width = 16;
    public int height = 16;
    public int mineCount = 32;

    private Board board;
    private Cell[,] cells;
    private bool gameover;

    private void OnValidate() {
        mineCount = Mathf.Clamp(mineCount, 0, width * height);
    }

    private void Awake() {
        board = GetComponentInChildren<Board>();
    }

    private void Start() {
        NewGame();
    }

    private void NewGame() {
        // Reset gameover bool in case we pressed R to restart the game after winning or losing.
        gameover = false;
        
        cells = new Cell[width, height];

        GenerateCells();
        GenerateMines();
        GenerateNumbers();

        Camera.main.transform.position = new Vector3(width / 2f, height / 2f, -10f);
        board.Draw(cells);
    }

    private void GenerateCells() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Cell cell = new Cell();
                cell.position = new Vector3Int(x, y, 0);
                cell.type = Cell.Type.Empty;
                cells[x, y] = cell;
            }
        }
    }

    private void GenerateMines() {
        for (int i = 0; i < mineCount; i++) {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            while (cells[x, y].type == Cell.Type.Mine) {
                x++;

                if (x >= width) {
                    x = 0;
                    y++;

                    if (y >= height) {
                        y = 0;
                    }
                }
            }

            cells[x, y].type = Cell.Type.Mine;
        }
    }

    private void GenerateNumbers() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Cell cell = cells[x, y];

                if (cell.type == Cell.Type.Mine) {
                    continue;
                }

                cell.number = CountMines(x, y);

                if (cell.number > 0) {
                    cell.type = Cell.Type.Number;
                }

                cells[x, y] = cell;
            }
        }
    }

    private int CountMines(int cellX, int cellY) {
        int count = 0;

        for (int adjacentX = -1; adjacentX <= 1; adjacentX++) {
            for (int adjacentY = -1; adjacentY <= 1; adjacentY++) {
                if (adjacentX == 0 && adjacentY == 0) {
                    continue;
                }

                int x = cellX + adjacentX;
                int y = cellY + adjacentY;

                if (GetCell(x, y).type == Cell.Type.Mine) {
                    count++;
                }
            }
        }

        return count;
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.R)) {
            NewGame();
        }

        if (gameover) {
            return;
        }
        
        if (Input.GetMouseButtonDown(1)) {
            Flag();
        }
        else if (Input.GetMouseButtonDown(0)) {
            Reveal();
        }
        else if (Input.GetMouseButtonDown(2)) {
            RevealAdjacent();
        }
    }

    private void Flag() {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);
        Cell cell = GetCell(cellPosition.x, cellPosition.y);

        // Cannot flag if already revealed
        if (cell.type == Cell.Type.Invalid || cell.revealed) {
            return;
        }

        cell.flagged = !cell.flagged;
        cells[cellPosition.x, cellPosition.y] = cell;
        board.Draw(cells);
    }

    private void Reveal() {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);
        Cell cell = GetCell(cellPosition.x, cellPosition.y);
        Reveal(cell);
    }
    
    private void Reveal(Cell cell) {
        // Cannot reveal if already revealed or while flagged
        if (cell.type == Cell.Type.Invalid || cell.revealed || cell.flagged) {
            return;
        }

        switch (cell.type) {
            case Cell.Type.Mine:
                Explode(cell);
                break;

            case Cell.Type.Empty:
                Flood(cell);
                CheckWinCondition();
                break;
            
            case Cell.Type.Number:
                cell.revealed = true;
                cells[cell.position.x, cell.position.y] = cell;
                CheckWinCondition();
                break;
            
            case Cell.Type.Invalid:
            default:
                throw new ArgumentOutOfRangeException();
        }

        board.Draw(cells);
    }

    /// <summary>
    /// Reveal all adjacent tiles if we click a number cell, but only if we've flagged the corresponding number of cells
    /// surrounding the clicked number.
    /// </summary>
    private void RevealAdjacent() {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);
        Cell cell = GetCell(cellPosition.x, cellPosition.y);

        if (!cell.revealed || cell.type != Cell.Type.Number) {
            return;
        }

        int flagCount = 0;
        for (int adjacentX = -1; adjacentX <= 1; adjacentX++) {
            for (int adjacentY = -1; adjacentY <= 1; adjacentY++) {
                if (adjacentX == 0 && adjacentY == 0) {
                    continue;
                }

                Cell adjacentCell = GetCell(cell.position.x + adjacentX, cell.position.y + adjacentY);
                if (adjacentCell.type == Cell.Type.Invalid) {
                    continue;
                }
                if (adjacentCell.flagged) {
                    flagCount++;
                }
            }
        }

        if (flagCount == cell.number) {
            for (int adjacentX = -1; adjacentX <= 1; adjacentX++) {
                for (int adjacentY = -1; adjacentY <= 1; adjacentY++) {
                    if (adjacentX == 0 && adjacentY == 0) {
                        continue;
                    }

                    Cell adjacentCell = GetCell(cell.position.x + adjacentX, cell.position.y + adjacentY);
                    if (adjacentCell.type == Cell.Type.Invalid) {
                        continue;
                    }
                    if (!adjacentCell.revealed && !adjacentCell.flagged) {
                        Reveal(adjacentCell);
                    }
                }
            }
        }
    }

    private void Explode(Cell cell) {
        Debug.Log("Game Over!");
        gameover = true;

        // Set the mine as exploded
        cell.exploded = true;
        cell.revealed = true;
        cells[cell.position.x, cell.position.y] = cell;

        // Reveal all other mines
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                cell = cells[x, y];

                if (cell.type == Cell.Type.Mine) {
                    cell.revealed = true;
                    cells[x, y] = cell;
                }
            }
        }
    }

    private void Flood(Cell cell) {
        // Recursive exit conditions
        if (cell.revealed) {
            return;

        }

        if (cell.type is Cell.Type.Mine or Cell.Type.Invalid) {
            return;

        }

        // Reveal the cell
        cell.revealed = true;
        cells[cell.position.x, cell.position.y] = cell;

        // Keep flooding if the cell is empty, otherwise stop at numbers
        if (cell.type == Cell.Type.Empty) {
            for (int adjacentX = -1; adjacentX <= 1; adjacentX++) {
                for (int adjacentY = -1; adjacentY <= 1; adjacentY++) {
                    if (adjacentX == 0 && adjacentY == 0) {
                        continue;
                    }

                    int x = cell.position.x + adjacentX;
                    int y = cell.position.y + adjacentY;

                    Flood(GetCell(x, y));
                }
            }
        }
    }

    private void CheckWinCondition() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Cell cell = cells[x, y];

                // All non-mine cells must be revealed to have won
                if (cell.type != Cell.Type.Mine && !cell.revealed) {
                    return; // no win
                }
            }
        }

        Debug.Log("Winner!");
        gameover = true;

        // Flag all the mines
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Cell cell = cells[x, y];

                if (cell.type == Cell.Type.Mine) {
                    cell.flagged = true;
                    cells[x, y] = cell;
                }
            }
        }
    }

    private Cell GetCell(int x, int y) {
        if (IsValid(x, y)) {
            return cells[x, y];
        }

        return new Cell();
    }

    private bool IsValid(int x, int y) {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}
