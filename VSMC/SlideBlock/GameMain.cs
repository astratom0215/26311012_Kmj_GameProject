// -------------------------------------------------------------------------------------------------------------------------------------------------------------
// Author: 3dapi (https://github.com/3dapi)
// -------------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Vortice.DirectWrite;
using Vortice.Mathematics;

enum GameState
{
    Title,
    StageSelect,
    Play,
    Clear,
    Fail
}

enum BlockType
{
    Basic,
    Crown,
    Reverse
}

enum Direction
{
    None,
    Up,
    Down,
    Left,
    Right
}

readonly record struct Cell(int Row, int Col);

sealed class BlockData
{
    public BlockType Type { get; }
    public Cell Position { get; set; }

    public BlockData(BlockType type, int row, int col)
    {
        Type = type;
        Position = new Cell(row, col);
    }

    public BlockData Clone()
    {
        return new BlockData(Type, Position.Row, Position.Col);
    }
}

sealed class PortalPair
{
    public Cell A { get; }
    public Cell B { get; }

    public PortalPair(Cell a, Cell b)
    {
        A = a;
        B = b;
    }
}

sealed class StageData
{
    public string Name { get; init; } = string.Empty;
    public int MaxMoves { get; init; }
    public Cell Goal { get; init; }
    public List<BlockData> Blocks { get; init; } = new();
    public HashSet<Cell> Pillars { get; init; } = new();
    public List<PortalPair> Portals { get; init; } = new();
}

class GameMain : G2AppBase
{

    public override System.Drawing.Size ScreenSize => GameGlobal.ScreenSize;
    public override string GameName => GameGlobal.GameName;

    private const float BoardX = 447.0f;
    private const float BoardY = 120.0f;
    private const float BoardWidth = 386.0f;
    private const float BoardHeight = 400.0f;
    private const float CellWidth = BoardWidth / 8.0f;
    private const float CellHeight = BoardHeight / 8.0f;

    private GameState _state = GameState.Title;

    private G2Texture? _mapTexture;
    private G2Texture? _titleTexture;
    private G2Texture? _btnStartTexture;
    private G2Texture? _btnExitTexture;
    private G2Texture? _btnResetTexture;
    private G2Texture? _btnRestartTexture;

    private G2Texture? _basicBlockTexture;
    private G2Texture? _crownBlockTexture;
    private G2Texture? _goalBlockTexture;
    private G2Texture? _reverseBlockTexture;

    private G2Texture? _pillarTexture;
    private G2Texture? _bluePortalTexture;
    private G2Texture? _purplePortalTexture;

    private G2AudioSound? _sndClick;
    private G2AudioMp3? _sndSlide;

    private G2Font? _fontSmall;
    private G2Font? _fontCenter;
    private G2Font? _fontBigCenter;

    private bool _soundEnabled = true;

    private readonly Rectangle _btnStartRect = new(330, 490, 180, 117);
    private readonly Rectangle _btnExitRect = new(770, 490, 180, 115);
    private readonly Rectangle _resetRect = new(585, 555, 110, 71);
    private readonly Rectangle _restartRect = new(210, 290, 180, 118);
    private readonly Rectangle _resultExitRect = new(890, 290, 180, 115);

    private readonly Rectangle[] _stageRects =
    {
        new Rectangle(360, 290, 70, 84),
        new Rectangle(605, 290, 70, 84),
        new Rectangle(850, 290, 70, 84)
    };

    private List<StageData> _stages = new();
    private StageData? _currentStage;
    private List<BlockData> _blocks = new();
    private int _currentStageIndex;
    private int _moveCount;

    protected override void Initialize()
    {
        _mapTexture = new G2Texture("resource/grid_map/map.png");
        _titleTexture = new G2Texture("resource/ui/title.png");
        _btnStartTexture = new G2Texture("resource/ui/start.png");
        _btnExitTexture = new G2Texture("resource/ui/exit.png");
        _btnResetTexture = new G2Texture("resource/ui/reset.png");
        _btnRestartTexture = new G2Texture("resource/ui/restart.png");

        _basicBlockTexture = new G2Texture("resource/block/basic_block.png");
        _crownBlockTexture = new G2Texture("resource/block/crown_block.png");
        _goalBlockTexture = new G2Texture("resource/block/goal_block.png");
        _reverseBlockTexture = new G2Texture("resource/block/reverse_block.png");

        _pillarTexture = new G2Texture("resource/obstacle/pillar.png");
        _bluePortalTexture = new G2Texture("resource/portals/blue_portal.png");
        _purplePortalTexture = new G2Texture("resource/portals/purple_portal.png");

        _sndClick = new G2AudioSound("resource/sound/sound.wav");
        _sndSlide = new G2AudioMp3("resource/sound/slide.mp3");

        _fontSmall = new G2Font(
            "Malgun Gothic",
            18,
            FontWeight.Bold,
            Vortice.DirectWrite.FontStyle.Normal,
            TextAlignment.Leading,
            ParagraphAlignment.Near);

        _fontCenter = new G2Font(
            "Malgun Gothic",
            25,
            FontWeight.Bold,
            Vortice.DirectWrite.FontStyle.Normal,
            TextAlignment.Center,
            ParagraphAlignment.Center);

        _fontBigCenter = new G2Font(
            "Malgun Gothic",
            44,
            FontWeight.Bold,
            Vortice.DirectWrite.FontStyle.Normal,
            TextAlignment.Center,
            ParagraphAlignment.Center);

        _stages = CreateStages();

        ClearColor = new Color4(0.06f, 0.06f, 0.08f, 1.0f);
    }

    protected override void Update()
    {
        switch (_state)
        {
            case GameState.Title:
                UpdateTitle();
                break;

            case GameState.StageSelect:
                UpdateStageSelect();
                break;

            case GameState.Play:
                UpdatePlay();
                break;

            case GameState.Clear:
            case GameState.Fail:
                UpdateResult();
                break;
        }
    }

    protected override void Render()
    {
        switch (_state)
        {
            case GameState.Title:
                RenderTitle();
                break;

            case GameState.StageSelect:
                RenderStageSelect();
                break;

            case GameState.Play:
                RenderPlay();
                break;

            case GameState.Clear:
            case GameState.Fail:
                RenderResult();
                break;
        }
    }

    private void UpdateTitle()
    {
        if (!Input.IsButtonDown(MouseButtons.Left))
            return;

        Point mouse = Point.Round(Input.MousePosition);

        if (_btnStartRect.Contains(mouse))
        {
            PlayClick();
            _state = GameState.StageSelect;
        }
        else if (_btnExitRect.Contains(mouse))
        {
            PlayClick();
            Close();
        }
    }

    private void UpdateStageSelect()
    {
        if (Input.IsKeyDown(Keys.Escape))
        {
            _state = GameState.Title;
            return;
        }

        if (!Input.IsButtonDown(MouseButtons.Left))
            return;

        Point mouse = Point.Round(Input.MousePosition);

        for (int i = 0; i < _stageRects.Length && i < _stages.Count; i++)
        {

            if (_stageRects[i].Contains(mouse))
            {
                PlayClick();
                StartStage(i);
                return;
            }
        }
    }

    private void UpdatePlay()
    {
        if (Input.IsKeyDown(Keys.Escape))
        {
            _state = GameState.StageSelect;
            return;
        }

        if (Input.IsKeyDown(Keys.R))
        {
            PlayClick();
            ResetStage();
            return;
        }

        if (Input.IsButtonDown(MouseButtons.Left))
        {
            Point mouse = Point.Round(Input.MousePosition);
            if (_resetRect.Contains(mouse))
            {
                PlayClick();
                ResetStage();
                return;
            }
        }

        Direction inputDirection = ReadDirectionInput();
        if (inputDirection != Direction.None)
        {
            ExecuteMove(inputDirection);
        }
    }

    private void UpdateResult()
    {
        if (Input.IsKeyDown(Keys.R))
        {
            PlayClick();
            ResetStage();
            return;
        }

        if (_state == GameState.Clear && Input.IsKeyDown(Keys.N))
        {
            PlayClick();

            if (_currentStageIndex + 1 < _stages.Count)
                StartStage(_currentStageIndex + 1);
            else
                _state = GameState.StageSelect;

            return;
        }

        if (!Input.IsButtonDown(MouseButtons.Left))
            return;

        Point mouse = Point.Round(Input.MousePosition);

        if (_restartRect.Contains(mouse))
        {
            PlayClick();
            ResetStage();
        }
        else if (_resultExitRect.Contains(mouse))
        {
            PlayClick();
            _state = GameState.StageSelect;
        }
    }

    private void RenderTitle()
    {
        _mapTexture?.Draw(BoardX, BoardY);
        _titleTexture?.Draw(448, 60);

        DrawCellTexture(_basicBlockTexture, new Cell(3, 3), 113, 126, 44, 46);
        DrawCellTexture(_crownBlockTexture, new Cell(3, 4), 108, 131, 44, 46);
        DrawCellTexture(_goalBlockTexture, new Cell(4, 3), 109, 130, 44, 46);

        DrawAbsolute(_btnStartTexture, _btnStartRect, 250, 162);
        DrawAbsolute(_btnExitTexture, _btnExitRect, 250, 160);
    }

    private void RenderStageSelect()
    {
        _titleTexture?.Draw(448, 80);

        _fontBigCenter?.DrawText(
            "스테이지 선택",
            new Rect(160, 150, 960, 70),
            new Color4(1, 1, 1, 1));

        for (int i = 0; i < _stageRects.Length && i < _stages.Count; i++)
        {
            Rectangle rect = _stageRects[i];

            DrawAbsolute(
                _goalBlockTexture,
                rect,
                109,
                130);

            _fontCenter?.DrawText(
                $"STAGE {i + 1}",
                new Rect(rect.X - 55, rect.Y + 82, 180, 38),
                new Color4(1, 1, 1, 1));
        }

        _fontSmall?.DrawText(
            "마우스로 스테이지 선택 / ESC : 뒤로",
            new Rect(460, 470, 420, 40),
            new Color4(0.85f, 0.85f, 0.85f, 1));
    }

    private void RenderPlay()
    {
        RenderBoard();

        _fontSmall?.DrawText(
            $"STAGE {_currentStageIndex + 1}\n{_currentStage?.Name}\n\n이동 횟수 : {_moveCount} / {_currentStage?.MaxMoves}",
            new Rect(185, 130, 230, 180),
            new Color4(1, 1, 1, 1));

        _fontSmall?.DrawText(
            "WASD / 방향키 : 이동\nR : 리셋\nESC : 스테이지 선택",
            new Rect(860, 130, 230, 150),
            new Color4(1, 1, 1, 1));

        DrawAbsolute(_btnResetTexture, _resetRect, 367, 236);
    }

    private void RenderResult()
    {
        RenderBoard();

        string resultText = _state == GameState.Clear ? "CLEAR!" : "실패";
        Color4 resultColor = _state == GameState.Clear
            ? new Color4(0.95f, 0.85f, 0.2f, 1)
            : new Color4(1.0f, 0.35f, 0.35f, 1);

        _fontBigCenter?.DrawText(
            resultText,
            new Rect(160, 45, 960, 70),
            resultColor);

        DrawAbsolute(_btnRestartTexture, _restartRect, 361, 236);
        DrawAbsolute(_btnExitTexture, _resultExitRect, 250, 160);

        if (_state == GameState.Clear)
        {
            _fontSmall?.DrawText(
                "N : 다음 스테이지",
                new Rect(920, 430, 170, 40),
                new Color4(1, 1, 1, 1));
        }
    }

    private void RenderBoard()
    {
        if (_currentStage == null)
            return;

        _mapTexture?.Draw(BoardX, BoardY);

        for (int i = 0; i < _currentStage.Portals.Count; i++)
        {
            PortalPair pair = _currentStage.Portals[i];
            DrawCellTexture(_bluePortalTexture, pair.A, 123, 121, 42, 42);
            DrawCellTexture(_purplePortalTexture, pair.B, 124, 121, 42, 42);
        }

        DrawCellTexture(_goalBlockTexture, _currentStage.Goal, 109, 130, 44, 46);

        foreach (Cell pillar in _currentStage.Pillars)
        {
            DrawCellTexture(_pillarTexture, pillar, 89, 162, 32, 48);
        }

        foreach (BlockData block in _blocks)
        {
            switch (block.Type)
            {
                case BlockType.Basic:
                    DrawCellTexture(_basicBlockTexture, block.Position, 113, 126, 44, 46);
                    break;

                case BlockType.Crown:
                    DrawCellTexture(_crownBlockTexture, block.Position, 108, 131, 44, 46);
                    break;

                case BlockType.Reverse:
                    DrawCellTexture(_reverseBlockTexture, block.Position, 107, 126, 44, 46);
                    break;
            }
        }
    }

    private void StartStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= _stages.Count)
            return;

        _currentStageIndex = stageIndex;
        _currentStage = _stages[stageIndex];
        _blocks = _currentStage.Blocks.Select(block => block.Clone()).ToList();
        _moveCount = 0;
        _state = GameState.Play;
    }

    private void ResetStage()
    {
        if (_currentStage == null)
            return;

        _blocks = _currentStage.Blocks.Select(block => block.Clone()).ToList();
        _moveCount = 0;
        _state = GameState.Play;
    }

    private Direction ReadDirectionInput()
    {
        if (Input.IsKeyDown(Keys.Up) || Input.IsKeyDown(Keys.W))
            return Direction.Up;

        if (Input.IsKeyDown(Keys.Down) || Input.IsKeyDown(Keys.S))
            return Direction.Down;

        if (Input.IsKeyDown(Keys.Left) || Input.IsKeyDown(Keys.A))
            return Direction.Left;

        if (Input.IsKeyDown(Keys.Right) || Input.IsKeyDown(Keys.D))
            return Direction.Right;

        return Direction.None;
    }

    private void ExecuteMove(Direction inputDirection)
    {
        if (_currentStage == null)
            return;

        Dictionary<BlockData, int> maxSteps;
        Dictionary<BlockData, Direction> directions =
            ResolveDirections(inputDirection, out maxSteps);

        Dictionary<Cell, BlockData> occupancy =
            _blocks.ToDictionary(block => block.Position, block => block);

        bool anyMoved = false;

        anyMoved |= MoveBlocksInDirection(Direction.Up, directions, maxSteps, occupancy);
        anyMoved |= MoveBlocksInDirection(Direction.Down, directions, maxSteps, occupancy);
        anyMoved |= MoveBlocksInDirection(Direction.Left, directions, maxSteps, occupancy);
        anyMoved |= MoveBlocksInDirection(Direction.Right, directions, maxSteps, occupancy);

        _moveCount++;

        if (anyMoved)
            PlaySlide();

        if (IsStageClear())
        {
            _state = GameState.Clear;
            return;
        }

        if (_moveCount >= _currentStage.MaxMoves)
        {
            _state = GameState.Fail;
        }
    }

    private Dictionary<BlockData, Direction> ResolveDirections(
        Direction inputDirection,
        out Dictionary<BlockData, int> maxSteps)
    {
        Dictionary<BlockData, Direction> directions = new();
        maxSteps = new Dictionary<BlockData, int>();

        Direction reverseDirection = Opposite(inputDirection);

        foreach (BlockData block in _blocks)
        {
            directions[block] = block.Type == BlockType.Reverse
                ? reverseDirection
                : inputDirection;

            maxSteps[block] = int.MaxValue;
        }

        bool horizontal = inputDirection is Direction.Left or Direction.Right;

        var groups = _blocks.GroupBy(block =>
        {
            int line = horizontal ? block.Position.Row : block.Position.Col;
            int segment = GetSegmentIndex(block, horizontal, directions);
            return (line, segment);
        });

        foreach (var group in groups)
        {
            List<BlockData> inputBlocks = group
                .Where(block => directions[block] == inputDirection)
                .ToList();

            List<BlockData> reverseBlocks = group
                .Where(block => directions[block] == reverseDirection)
                .ToList();

            if (inputBlocks.Count == 0 || reverseBlocks.Count == 0)
                continue;

            if (!TryGetCollisionGap(
                    inputBlocks,
                    reverseBlocks,
                    inputDirection,
                    horizontal,
                    out int emptyGap))
            {
                continue;
            }

            if (inputBlocks.Count == reverseBlocks.Count)
            {
                int sameSteps = emptyGap / 2;
                int inputExtraStep = emptyGap % 2;

                foreach (BlockData block in inputBlocks)
                    maxSteps[block] = sameSteps + inputExtraStep;

                foreach (BlockData block in reverseBlocks)
                    maxSteps[block] = sameSteps;
            }
            else
            {
                Direction winnerDirection =
                    inputBlocks.Count > reverseBlocks.Count
                        ? inputDirection
                        : reverseDirection;

                foreach (BlockData block in inputBlocks)
                {
                    directions[block] = winnerDirection;
                    maxSteps[block] = int.MaxValue;
                }

                foreach (BlockData block in reverseBlocks)
                {
                    directions[block] = winnerDirection;
                    maxSteps[block] = int.MaxValue;
                }
            }
        }

        return directions;
    }

    private int GetSegmentIndex(
        BlockData block,
        bool horizontal,
        Dictionary<BlockData, Direction> directions)
    {
        if (_currentStage == null)
            return 0;

        int segment = 0;

        if (horizontal)
        {
            foreach (Cell pillar in _currentStage.Pillars)
            {
                if (pillar.Row == block.Position.Row && pillar.Col < block.Position.Col)
                    segment++;
            }

            foreach (BlockData other in _blocks)
            {
                if (ReferenceEquals(block, other))
                    continue;

                if (directions[other] != Direction.None)
                    continue;

                if (other.Position.Row == block.Position.Row &&
                    other.Position.Col < block.Position.Col)
                {
                    segment++;
                }
            }
        }
        else
        {
            foreach (Cell pillar in _currentStage.Pillars)
            {
                if (pillar.Col == block.Position.Col && pillar.Row < block.Position.Row)
                    segment++;
            }

            foreach (BlockData other in _blocks)
            {
                if (ReferenceEquals(block, other))
                    continue;

                if (directions[other] != Direction.None)
                    continue;

                if (other.Position.Col == block.Position.Col &&
                    other.Position.Row < block.Position.Row)
                {
                    segment++;
                }
            }
        }

        return segment;
    }

    private static bool TryGetCollisionGap(
        List<BlockData> inputBlocks,
        List<BlockData> reverseBlocks,
        Direction inputDirection,
        bool horizontal,
        out int emptyGap)
    {
        int MinAxis(IEnumerable<BlockData> blocks)
        {
            return horizontal
                ? blocks.Min(block => block.Position.Col)
                : blocks.Min(block => block.Position.Row);
        }

        int MaxAxis(IEnumerable<BlockData> blocks)
        {
            return horizontal
                ? blocks.Max(block => block.Position.Col)
                : blocks.Max(block => block.Position.Row);
        }

        switch (inputDirection)
        {
            case Direction.Right:
            case Direction.Down:
                {
                    int inputFront = MaxAxis(inputBlocks);
                    int reverseFront = MinAxis(reverseBlocks);

                    emptyGap = reverseFront - inputFront - 1;
                    return emptyGap >= 0;
                }

            case Direction.Left:
            case Direction.Up:
                {
                    int inputFront = MinAxis(inputBlocks);
                    int reverseFront = MaxAxis(reverseBlocks);

                    emptyGap = inputFront - reverseFront - 1;
                    return emptyGap >= 0;
                }

            default:
                emptyGap = 0;
                return false;
        }
    }

    private bool MoveBlocksInDirection(
        Direction direction,
        Dictionary<BlockData, Direction> directions,
        Dictionary<BlockData, int> maxSteps,
        Dictionary<Cell, BlockData> occupancy)
    {
        IEnumerable<BlockData> movingBlocks = _blocks
            .Where(block => directions[block] == direction);

        movingBlocks = direction switch
        {
            Direction.Up => movingBlocks.OrderBy(block => block.Position.Row),
            Direction.Down => movingBlocks.OrderByDescending(block => block.Position.Row),
            Direction.Left => movingBlocks.OrderBy(block => block.Position.Col),
            Direction.Right => movingBlocks.OrderByDescending(block => block.Position.Col),
            _ => movingBlocks
        };

        bool anyMoved = false;

        foreach (BlockData block in movingBlocks.ToList())
        {
            Cell oldPosition = block.Position;
            occupancy.Remove(oldPosition);

            int blockMaxSteps = maxSteps.TryGetValue(block, out int limit)
                ? limit
                : int.MaxValue;

            Cell newPosition = MoveBlockToEnd(
                oldPosition,
                direction,
                occupancy,
                blockMaxSteps);

            block.Position = newPosition;
            occupancy[newPosition] = block;

            if (newPosition != oldPosition)
                anyMoved = true;
        }

        return anyMoved;
    }

    private Cell MoveBlockToEnd(
        Cell start,
        Direction direction,
        Dictionary<Cell, BlockData> occupancy,
        int maxSteps)
    {
        if (_currentStage == null)
            return start;

        Cell current = start;
        int safety = 0;
        int movedSteps = 0;

        while (safety++ < 64 && movedSteps < maxSteps)
        {
            Cell next = Step(current, direction);

            if (!IsInsideBoard(next))
                break;

            if (_currentStage.Pillars.Contains(next))
                break;

            if (occupancy.ContainsKey(next))
                break;

            if (TryGetPortalExit(next, out Cell portalExit))
            {
                if (!IsInsideBoard(portalExit))
                    break;

                if (_currentStage.Pillars.Contains(portalExit))
                    break;

                if (occupancy.ContainsKey(portalExit))
                    break;

                current = portalExit;
                movedSteps++;
                continue;
            }

            current = next;
            movedSteps++;
        }

        return current;
    }

    private bool IsStageClear()
    {
        if (_currentStage == null)
            return false;

        BlockData? crown = _blocks.FirstOrDefault(block => block.Type == BlockType.Crown);
        return crown != null && crown.Position == _currentStage.Goal;
    }

    private bool TryGetPortalExit(Cell entry, out Cell exit)
    {
        if (_currentStage != null)
        {
            foreach (PortalPair pair in _currentStage.Portals)
            {
                if (pair.A == entry)
                {
                    exit = pair.B;
                    return true;
                }

                if (pair.B == entry)
                {
                    exit = pair.A;
                    return true;
                }
            }
        }

        exit = default;
        return false;
    }

    private static Cell Step(Cell cell, Direction direction)
    {
        return direction switch
        {
            Direction.Up => new Cell(cell.Row - 1, cell.Col),
            Direction.Down => new Cell(cell.Row + 1, cell.Col),
            Direction.Left => new Cell(cell.Row, cell.Col - 1),
            Direction.Right => new Cell(cell.Row, cell.Col + 1),
            _ => cell
        };
    }

    private static Direction Opposite(Direction direction)
    {
        return direction switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => Direction.None
        };
    }

    private static bool IsInsideBoard(Cell cell)
    {
        return cell.Row >= 0 && cell.Row < 8 &&
               cell.Col >= 0 && cell.Col < 8;
    }

    private void DrawCellTexture(
        G2Texture? texture,
        Cell cell,
        float sourceWidth,
        float sourceHeight,
        float maxWidth,
        float maxHeight)
    {
        if (texture == null)
            return;

        float scale = Math.Min(maxWidth / sourceWidth, maxHeight / sourceHeight);
        float drawWidth = sourceWidth * scale;
        float drawHeight = sourceHeight * scale;

        float cellX = BoardX + cell.Col * CellWidth;
        float cellY = BoardY + cell.Row * CellHeight;

        float x = cellX + (CellWidth - drawWidth) / 2.0f;
        float y = cellY + (CellHeight - drawHeight) / 2.0f;

        texture.Draw(
            new Rect(x, y, drawWidth, drawHeight),
            new Rect(0, 0, sourceWidth, sourceHeight));
    }

    private static void DrawAbsolute(
        G2Texture? texture,
        Rectangle rectangle,
        float sourceWidth,
        float sourceHeight)
    {
        if (texture == null)
            return;

        texture.Draw(
            new Rect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height),
            new Rect(0, 0, sourceWidth, sourceHeight));
    }

    private void PlayClick()
    {
        if (_soundEnabled)
            _sndClick?.Play();
    }

    private void PlaySlide()
    {
        if (_soundEnabled)
            _sndSlide?.Play(false);
    }

    private static List<StageData> CreateStages()
    {
        return new List<StageData>
        {
            new StageData
            {
                Name = "기본 이동", //답: 상 -> 좌 -> 하
                MaxMoves = 3,
                Goal = new Cell(6, 1),
                Blocks = new List<BlockData>
                {
                    new BlockData(BlockType.Crown, 6, 5),
                    new BlockData(BlockType.Basic, 3, 4)
                },
                Pillars = new HashSet<Cell>
                {
                    new Cell(7, 1),
                }
            },

            new StageData
            {
                Name = "반전",    //답: 우 -> 하 -> 우 -> 상
                MaxMoves = 4,
                Goal = new Cell(0, 6),
                Blocks = new List<BlockData>
                {
                    new BlockData(BlockType.Crown, 1, 1),
                    new BlockData(BlockType.Reverse, 1, 6),
                },
                Pillars = new HashSet<Cell>
                {
                    new Cell(7, 3),
                    new Cell(6, 7)
                }
            },

            new StageData
            {
                Name = "포탈",    //답: 하 -> 좌 -> 상
                MaxMoves = 3,
                Goal = new Cell(0, 4),
                Blocks = new List<BlockData>
                {
                    new BlockData(BlockType.Crown, 1, 1),
                    new BlockData(BlockType.Basic, 1, 4)
                },
                Pillars = new HashSet<Cell>
                {
                    new Cell(5, 7),
                    new Cell(7, 6)
                },
                Portals = new List<PortalPair>
                {
                    new PortalPair(
                        new Cell(2, 4),
                        new Cell(5, 1))
                }
            }
        };
    }

    public override void Dispose()
    {
        _sndClick?.Dispose();
        _sndSlide?.Dispose();

        _fontSmall?.Dispose();
        _fontCenter?.Dispose();
        _fontBigCenter?.Dispose();

        _mapTexture?.Dispose();
        _titleTexture?.Dispose();
        _btnStartTexture?.Dispose();
        _btnExitTexture?.Dispose();
        _btnResetTexture?.Dispose();
        _btnRestartTexture?.Dispose();

        _basicBlockTexture?.Dispose();
        _crownBlockTexture?.Dispose();
        _goalBlockTexture?.Dispose();
        _reverseBlockTexture?.Dispose();

        _pillarTexture?.Dispose();
        _bluePortalTexture?.Dispose();
        _purplePortalTexture?.Dispose();

        base.Dispose();
    }
}
