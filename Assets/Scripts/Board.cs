using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class Board : MonoBehaviour {

    public Tilemap tilemap { get; private set; }

    public Tile tileNum1;
    public Tile tileNum2;
    public Tile tileNum3;
    public Tile tileNum4;
    public Tile tileNum5;
    public Tile tileNum6;
    public Tile tileNum7;
    public Tile tileNum8;
    public Tile tileEmpty;
    public Tile tileExploded;
    public Tile tileFlag;
    public Tile tileMine;
    public Tile tileUnknown;

    private void Awake() {
        tilemap = GetComponent<Tilemap>();
    }

    public void Draw(Cell[,] cells) {
        int width = cells.GetLength(0);
        int height = cells.GetLength(1);

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Cell cell = cells[x, y];
                tilemap.SetTile(cell.position, GetTile(cell));
            }
        }
    }

    private Tile GetTile(Cell cell) {
        if (cell.revealed) {
            return GetRevealedTile(cell);
        }

        if (cell.flagged) {
            return tileFlag;
        }

        return tileUnknown;
    }

    private Tile GetRevealedTile(Cell cell) {
        switch (cell.type) {
            case Cell.Type.Empty: 
                return tileEmpty;
            
            case Cell.Type.Mine: 
                return cell.exploded ? tileExploded : tileMine;
            
            case Cell.Type.Number: 
                return GetNumberTile(cell);

            case Cell.Type.Invalid:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private Tile GetNumberTile(Cell cell) {
        switch (cell.number) {
            case 1: return tileNum1;
            case 2: return tileNum2;
            case 3: return tileNum3;
            case 4: return tileNum4;
            case 5: return tileNum5;
            case 6: return tileNum6;
            case 7: return tileNum7;
            case 8: return tileNum8;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

}
