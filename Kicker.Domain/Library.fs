namespace Kicker.Domain

open System

module Utils =
    let find2D item (arr: 'T [,]) =
        let rec go x y =
            if y >= arr.GetLength 1 then
                None
            elif x >= arr.GetLength 0 then
                go 0 (y + 1)
            elif arr.[x, y] = item then
                Some(x, y)
            else
                go (x + 1) y

        go 0 0

    let spiral =
        let rec f (x, y) d m =
            seq {
                let mutable x = x
                let mutable y = y

                while 2 * x * d < m do
                    yield x, y
                    x <- x + d

                while 2 * y * d < m do
                    yield x, y
                    y <- y + d

                yield! f (x, y) -d (m + 1)
            }

        f (0, 0) 1 1

type GameSettings =
    { FieldHeight: int
      FieldWidth: int
      GoalHeight: int }

    static member create fieldHeight goalHeight =
        let widthToHeightRatio = 1.5441
        let fieldWidth = int (float fieldHeight * widthToHeightRatio) + 2

        { FieldHeight = fieldHeight
          FieldWidth = fieldWidth
          GoalHeight = goalHeight }

    static member defaultSettings = GameSettings.create 9 3
    member this.goalTop = (this.FieldHeight - this.GoalHeight) / 2
    member this.goalBottom = this.goalTop + this.GoalHeight

module Game =

    type Team =
        | Team1 = 0
        | Team2 = 1

    type Player = { Team: Team; Number: int }

    type TileValue =
        | EmptyTile
        | OutTile
        | PlayerTile of Player
        | BallTile

    type Game =
        private
            { Tiles: TileValue [,]
              Settings: GameSettings }
    
    type Coordinate = int * int
    
    type PlayerState =
        { Position: Coordinate
          Player: Player }

    type GameState =
        { Settings: GameSettings
          Players: PlayerState array
          BallPosition: Coordinate }

    let create (settings: GameSettings) =
        let tiles = Array2D.create settings.FieldWidth settings.FieldHeight EmptyTile

        let addOut col =
            for row in 0 .. settings.goalTop - 1 do
                tiles.[col, row] <- OutTile

            for row in settings.goalBottom .. settings.FieldHeight - 1 do
                tiles.[col, row] <- OutTile

        addOut 0
        addOut (settings.FieldWidth - 1)

        tiles.[2, 2] <- PlayerTile { Team = Team.Team1; Number = 1 }
        tiles.[2, 3] <- PlayerTile { Team = Team.Team1; Number = 2 }
        tiles.[2, 4] <- PlayerTile { Team = Team.Team1; Number = 3 }

        tiles.[11, 2] <- PlayerTile { Team = Team.Team2; Number = 1 }
        tiles.[11, 3] <- PlayerTile { Team = Team.Team2; Number = 2 }
        tiles.[11, 4] <- PlayerTile { Team = Team.Team2; Number = 3 }

        tiles.[8, 3] <- BallTile

        { Tiles = tiles; Settings = settings }

    type Direction =
        | Up = 0
        | Down = 1
        | Left = 2
        | Right = 3

    let directions =
        [ Direction.Left
          Direction.Right
          Direction.Up
          Direction.Down ]

    let find value { Tiles = tiles } = tiles |> Utils.find2D value

    type MovedObject =
        | MovedBall of Coordinate
        | MovedPlayer of Player * Coordinate

    type MoveResult =
        | BlockedByObstacle
        | PlayerNotFound
        | Moved of MovedObject list

    let private isValid (col, row) { Game.Settings = settings } =
        0 <= col
        && col < settings.FieldWidth
        && 0 <= row
        && row < settings.FieldHeight

    let private get (col, row) { Tiles = tiles } = tiles.[col, row]

    let private tryGet coordinate game =
        if isValid coordinate game then
            get coordinate game |> Some
        else
            None

    let private set (col, row) value { Tiles = tiles } = tiles.[col, row] <- value

    let private isFree c game =
        isValid c game
        && match get c game with
           | EmptyTile -> true
           | _ -> false

    let private getValidCoordinate coordinate game =
        if isValid coordinate game then
            Some coordinate
        else
            None

    let private getTile (x, y) direction game =
        let coordinate =
            match direction with
            | Direction.Up -> (x, y - 1)
            | Direction.Down -> (x, y + 1)
            | Direction.Left -> (x - 1, y)
            | Direction.Right -> (x + 1, y)
            | _ -> ArgumentOutOfRangeException() |> raise

        getValidCoordinate coordinate game

    let private rand = Random()

    let private findFreeTile (x, y) direction game =
        let isBlocked coordinate = game |> isFree coordinate |> not

        Utils.spiral
        |> Seq.map (fun (x, y) ->
            if rand.Next() % 2 = 0 then
                x, y
            else
                x, -y)
        |> Seq.map (fun (x, y) ->
            match direction with
            | Direction.Left -> (x, y)
            | Direction.Right -> (-x, y)
            | Direction.Up -> (y, x)
            | Direction.Down -> (y, -x)
            | _ -> ArgumentOutOfRangeException() |> raise)
        |> Seq.map (fun (x', y') -> (x + x', y + y'))
        |> Seq.skipWhile isBlocked
        |> Seq.head

    let getState (game: Game) =
        let mutable players = []
        let mutable ballPos = (0, 0)

        for col in 0 .. game.Settings.FieldWidth - 1 do
            for row in 0 .. game.Settings.FieldHeight - 1 do
                match game.Tiles.[col, row] with
                | EmptyTile -> ()
                | OutTile -> ()
                | BallTile -> ballPos <- (col, row)
                | PlayerTile p -> players <- {Player = p; Position = (col, row)} :: players

        { GameState.Settings = game.Settings
          Players = players |> Seq.toArray
          BallPosition = ballPos }

    let private move (player: Player) direction game =
        let coordinate = game |> find (PlayerTile player)

        match coordinate with
        | Some coordinate ->
            let nextTile = game |> getTile coordinate direction

            match nextTile with
            | Some nextCoordinate ->
                let nextValue = game |> get nextCoordinate

                match nextValue with
                | EmptyTile ->
                    game |> set nextCoordinate (PlayerTile player)
                    game |> set coordinate EmptyTile
                    Moved [ MovedPlayer(player, nextCoordinate) ]
                | BallTile ->
                    let ballCoordinate =
                        match game |> getTile nextCoordinate direction with
                        | Some coordinate when isFree coordinate game -> coordinate
                        | _ -> findFreeTile nextCoordinate direction game

                    game |> set nextCoordinate (PlayerTile player)
                    game |> set coordinate EmptyTile
                    game |> set ballCoordinate BallTile

                    Moved [ MovedPlayer(player, nextCoordinate)
                            MovedBall ballCoordinate ]
                | _ -> BlockedByObstacle
            | None -> BlockedByObstacle
        | None -> PlayerNotFound

    let private kick (player: Player) game =
        let tryGetBall coordinate direction =
            let ballTile = game |> getTile coordinate direction

            match ballTile with
            | Some ballCoordinate ->
                match game |> tryGet ballCoordinate with
                | Some BallTile -> Some(ballCoordinate, direction)
                | _ -> None
            | _ -> None

        let coordinate = game |> find (PlayerTile player)

        match coordinate with
        | Some coordinate ->
            let ball =
                directions
                |> Seq.choose (tryGetBall coordinate)
                |> Seq.tryHead

            match ball with
            | Some (start, direction) ->
                let rec whileEmptyGo count coordinate =
                    if count <= 0 then
                        coordinate
                    else
                        let next = getTile coordinate direction game

                        match next with
                        | Some c when isFree c game -> whileEmptyGo (count - 1) c
                        | _ -> coordinate

                let target = whileEmptyGo 4 start
                game |> set start EmptyTile
                game |> set target BallTile
                Moved [ MovedBall target ]
            | _ -> BlockedByObstacle
        | None -> PlayerNotFound

    type GameCommand =
        | Move of Player * Direction
        | Kick of Player
        
    type GameNotification =
        | State of GameState
        | MoveNotification of MoveResult
        
    let processCommand = function
        | Move (player, direction) -> move player direction
        | Kick player -> kick player