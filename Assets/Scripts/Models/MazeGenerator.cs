using System;
using System.Collections.Generic;
using System.Text;

public class Cell
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsVisited { get; set; }
    public bool[] Doors { get; set; } = new bool[4]; // North, East, South, West
    public bool[] DoorsPlaced { get; set; } = new bool[4]; // Whether a door has been placed in the corresponding direction

    public bool IsPlaced { get; set; } = false;

    public Cell(int x, int y)
    {
        X = x;
        Y = y;
        IsVisited = false;
    }

    public int DoorCount()
    {
        int count = 0;
        foreach (bool door in Doors)
        {
            if (door) count++;
        }
        return count;
    }
}

public class MazeGenerator
{
    private readonly int width;
    private readonly int height;
    private readonly Cell[,] cells;
    private readonly Random random;

    private static readonly (int X, int Y)[] Directions =
    {
        (0, -1), // North
        (1, 0),  // East
        (0, 1),  // South
        (-1, 0)  // West
    };

    public int Width => width;
    public int Height => height;
    public Cell[,] Cells => cells;

    public MazeGenerator(int width, int height, int? seed = null)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
        }

        this.width = width;
        this.height = height;
        random = seed.HasValue ? new Random(seed.Value) : new Random();
        cells = new Cell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = new Cell(x, y);
            }
        }
    }

    public List<(Cell Cell, int Direction)> GetAllNeighbors(Cell cell)
    {
        List<(Cell Cell, int Direction)> neighbors = new();

        for (int i = 0; i < Directions.Length; i++)
        {
            int nextX = cell.X + Directions[i].X;
            int nextY = cell.Y + Directions[i].Y;

            if (nextX >= 0 && nextX < width && nextY >= 0 && nextY < height)
            {
                neighbors.Add((cells[nextX, nextY], i));
            }
        }

        return neighbors;
    }

    public List<Cell> GetUnvisitedNeighbors(Cell cell)
    {
        List<Cell> unvisitedNeighbors = new();

        foreach ((Cell neighbor, _) in GetAllNeighbors(cell))
        {
            if (!neighbor.IsVisited)
            {
                unvisitedNeighbors.Add(neighbor);
            }
        }

        return unvisitedNeighbors;
    }

    public void RemoveWall(Cell currentCell, Cell nextCell)
    {
        int dx = nextCell.X - currentCell.X;
        int dy = nextCell.Y - currentCell.Y;

        if (dx == 0 && dy == -1)
        {
            currentCell.Doors[0] = true;
            nextCell.Doors[2] = true;
        }
        else if (dx == 1 && dy == 0)
        {
            currentCell.Doors[1] = true;
            nextCell.Doors[3] = true;
        }
        else if (dx == 0 && dy == 1)
        {
            currentCell.Doors[2] = true;
            nextCell.Doors[0] = true;
        }
        else if (dx == -1 && dy == 0)
        {
            currentCell.Doors[3] = true;
            nextCell.Doors[1] = true;
        }
    }

    public Cell[,] GenerateMaze(int startX = 0, int startY = 0, double newestBias = 1.0)
    {
        if (startX < 0 || startX >= width)
        {
            throw new ArgumentOutOfRangeException(nameof(startX));
        }

        if (startY < 0 || startY >= height)
        {
            throw new ArgumentOutOfRangeException(nameof(startY));
        }

        Cell start = cells[startX, startY];
        start.IsVisited = true;

        List<Cell> active = new() { start };

        while (active.Count > 0)
        {
            Cell cell = random.NextDouble() < newestBias
                ? active[active.Count - 1]
                : active[random.Next(active.Count)];

            List<Cell> neighbors = GetUnvisitedNeighbors(cell);

            if (neighbors.Count > 0)
            {
                Cell nextCell = neighbors[random.Next(neighbors.Count)];
                RemoveWall(cell, nextCell);
                nextCell.IsVisited = true;
                active.Add(nextCell);
            }
            else
            {
                active.Remove(cell);
            }
        }

        return cells;
    }

    public void InjectLoops(double loopChance = 0.1)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Cell cell = cells[x, y];

                if (x + 1 < width)
                {
                    Cell neighbor = cells[x + 1, y];
                    if (!cell.Doors[1] && random.NextDouble() < loopChance)
                    {
                        RemoveWall(cell, neighbor);
                    }
                }

                if (y + 1 < height)
                {
                    Cell neighbor = cells[x, y + 1];
                    if (!cell.Doors[2] && random.NextDouble() < loopChance)
                    {
                        RemoveWall(cell, neighbor);
                    }
                }
            }
        }
    }

    public string DisplayMaze()
    {
        StringBuilder builder = new();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                builder.Append('+');
                builder.Append(cells[x, y].Doors[0] ? "   " : "---");
            }

            builder.AppendLine("+");

            for (int x = 0; x < width; x++)
            {
                builder.Append(cells[x, y].Doors[3] ? ' ' : '|');
                builder.Append("   ");
            }

            builder.AppendLine("|");
        }

        for (int x = 0; x < width; x++)
        {
            builder.Append('+');
            builder.Append(cells[x, height - 1].Doors[2] ? "   " : "---");
        }

        builder.Append('+');
        return builder.ToString();
    }
}
