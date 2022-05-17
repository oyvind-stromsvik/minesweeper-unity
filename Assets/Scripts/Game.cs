using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class Game : MonoBehaviour {

    public int width = 16;
    public int height = 16;
    public int mineCount = 32;

    private Board board;
    private Cell[,] state;
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
        
        state = new Cell[width, height];

        GenerateCells();
        GenerateMines();
        GenerateNumbers();

        Camera.main.transform.position = new Vector3(width / 2f, height / 2f, -10f);
        board.Draw(state);
    }

    private void GenerateCells() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Cell cell = new Cell();
                cell.position = new Vector3Int(x, y, 0);
                cell.type = Cell.Type.Empty;
                state[x, y] = cell;
            }
        }
    }

    private void GenerateMines() {
        for (int i = 0; i < mineCount; i++) {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            while (state[x, y].type == Cell.Type.Mine) {
                x++;

                if (x >= width) {
                    x = 0;
                    y++;

                    if (y >= height) {
                        y = 0;
                    }
                }
            }

            state[x, y].type = Cell.Type.Mine;
        }
    }

    private void GenerateNumbers() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Cell cell = state[x, y];

                if (cell.type == Cell.Type.Mine) {
                    continue;
                }

                cell.number = CountMines(x, y);

                if (cell.number > 0) {
                    cell.type = Cell.Type.Number;
                }

                state[x, y] = cell;
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
        state[cellPosition.x, cellPosition.y] = cell;
        board.Draw(state);
    }

    private void Reveal() {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);
        Cell cell = GetCell(cellPosition.x, cellPosition.y);

        // Cannot reveal if already revealed or while flagged
        if (cell.type == Cell.Type.Invalid || cell.revealed || cell.flagged) {
            return;
        }

        switch (cell.type) {
            case Cell.Type.Mine:
                Explode(cell);
                break;

            case Cell.Type.Empty:
                StartCoroutine(Flood(cell));
                CheckWinCondition();
                break;
            
            case Cell.Type.Number:
                cell.revealed = true;
                state[cellPosition.x, cellPosition.y] = cell;
                CheckWinCondition();
                break;
            
            case Cell.Type.Invalid:
            default:
                throw new ArgumentOutOfRangeException();
        }

        board.Draw(state);
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
                        adjacentCell.revealed = true;
                        state[adjacentCell.position.x, adjacentCell.position.y] = adjacentCell;
                    }
                }
            }
            board.Draw(state);
        }
    }

    private void Explode(Cell cell) {
        Debug.Log("Game Over!");
        gameover = true;

        // Set the mine as exploded
        cell.exploded = true;
        cell.revealed = true;
        state[cell.position.x, cell.position.y] = cell;

        // Reveal all other mines
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                cell = state[x, y];

                if (cell.type == Cell.Type.Mine) {
                    cell.revealed = true;
                    state[x, y] = cell;
                }
            }
        }
    }

    private IEnumerator Flood(Cell cell) {
        // Recursive exit conditions
        if (cell.revealed) {
            yield break;

        }

        if (cell.type is Cell.Type.Mine or Cell.Type.Invalid) {
            yield break;

        }

        // Reveal the cell
        cell.revealed = true;
        state[cell.position.x, cell.position.y] = cell;

        // Wait before continuing the flood
        board.Draw(state);
        // TODO: After switching this to a Coroutine we should probably guard against clicking while the animation plays.
        // TODO: Also if we click "R" to restart the game while the animation plays it will continue after the game resets.
        yield return new WaitForEndOfFrame();

        // Keep flooding if the cell is empty, otherwise stop at numbers
        if (cell.type == Cell.Type.Empty) {
            StartCoroutine(Flood(GetCell(cell.position.x - 1, cell.position.y)));
            StartCoroutine(Flood(GetCell(cell.position.x + 1, cell.position.y)));
            StartCoroutine(Flood(GetCell(cell.position.x, cell.position.y - 1)));
            StartCoroutine(Flood(GetCell(cell.position.x, cell.position.y + 1)));
        }
    }

    private void CheckWinCondition() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Cell cell = state[x, y];

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
                Cell cell = state[x, y];

                if (cell.type == Cell.Type.Mine) {
                    cell.flagged = true;
                    state[x, y] = cell;
                }
            }
        }
    }

    private Cell GetCell(int x, int y) {
        if (IsValid(x, y)) {
            return state[x, y];
        }

        return new Cell();
    }

    private bool IsValid(int x, int y) {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}
