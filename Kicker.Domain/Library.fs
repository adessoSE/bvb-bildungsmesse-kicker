﻿namespace Kicker.Domain

open System

type Game =
    private
        { Tiles: TileValue [,]
          Settings: GameSettings
          mutable Status: GameStatus
          mutable PreviousStatus: GameStatus }

module Game =    
    let create (settings: GameSettings) =
        let tiles = Array2D.create settings.FieldWidth settings.FieldHeight EmptyTile

        let addOut col =
            for row in 0 .. settings.goalTop - 1 do
                tiles[col, row] <- OutTile

            for row in settings.goalBottom .. settings.FieldHeight - 1 do
                tiles[col, row] <- OutTile

        addOut 0
        addOut (settings.FieldWidth - 1)

        
        let setTeam team col =
            let players = settings.Players |> Seq.filter (fun p -> p.Team = team) |> Seq.toArray
            let count = players.Length
            let center = settings.FieldHeight / 2
            let half = count / 2
            for i in 0..(half - 1) do
                tiles[col, center - 1 - i] <- PlayerTile players[i]
            for i in half..(count - 1) do
                tiles[col, center + 2 * half - i] <- PlayerTile players[i]
        
        setTeam Team.BVB (settings.FieldWidth / 2 - 2)
        setTeam Team.ADESSO (settings.FieldWidth / 2 + 2)
        
        tiles[settings.FieldWidth / 2, settings.FieldHeight / 2] <- BallTile

        { Tiles = tiles; Settings = settings; Status = Running; PreviousStatus = NotRunning }

    let directions =
        [ Direction.Left
          Direction.Right
          Direction.Up
          Direction.Down ]

    let find value { Tiles = tiles } = tiles |> Utils.find2D value

    let private isValid (col, row) { Game.Settings = settings } =
        0 <= col
        && col < settings.FieldWidth
        && 0 <= row
        && row < settings.FieldHeight

    let get (col, row) { Tiles = tiles } = tiles[col, row]

    let private tryGet coordinate game =
        if isValid coordinate game then
            get coordinate game |> Some
        else
            None

    let private set (col, row) value { Tiles = tiles } = tiles[col, row] <- value

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

        let spielstand =
            match game.Status with
            | StoppedWithGoal stand -> stand
            | _ -> game.Settings.Spielstand
        
        for col in 0 .. game.Settings.FieldWidth - 1 do
            for row in 0 .. game.Settings.FieldHeight - 1 do
                match game.Tiles[col, row] with
                | EmptyTile -> ()
                | OutTile -> ()
                | BallTile -> ballPos <- (col, row)
                | PlayerTile p -> players <- {Player = p; Position = (col, row)} :: players

        { GameState.Settings = game.Settings
          Players = players |> Seq.toArray
          BallPosition = ballPos
          Status = game.Status
          PreviousStatus = game.PreviousStatus
          Spielstand = spielstand }

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

        let playerCoord = game |> find (PlayerTile player)

        match playerCoord with
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
                if target <> start then
                    game |> set start EmptyTile
                    game |> set target BallTile
                    Moved [ MovedBall target ]
                else
                    BlockedByObstacle
            | _ -> BlockedByObstacle
        | None -> PlayerNotFound

    let private checkGoal player game result =
        let { Game.Settings = { FieldWidth = width } } = game
                
        let ballInGoal = function
            | MovedBall (col, _) -> col = 0 || col = width - 1
            | _ -> false
            
        match result with
        | Moved moved when moved |> List.exists ballInGoal ->
            let spielstand = game.Settings.Spielstand.increment player.Team
            game.Status <- StoppedWithGoal spielstand
            game.PreviousStatus <- Running
            Goal (moved, player, spielstand)
        | x -> x

    let private toggleGameStatus (game:Game) =
        match game.Status with
        | StoppedByAdmin ->
            game.Status <- Running
            game.PreviousStatus <- StoppedByAdmin
            Resumed
        | Running ->
            game.Status <- StoppedByAdmin
            game.PreviousStatus <- Running
            Paused
        | _ -> Ignored
        
    let processCommand command (game: Game) =
        match game.Status with
        | Running ->
            match command with
            | Move (player, direction) -> move player direction game |> checkGoal player game
            | Kick player -> kick player game |> checkGoal player game
            | TogglePause -> toggleGameStatus game
        | StoppedByAdmin ->
            match command with
            | TogglePause -> toggleGameStatus game
            | _ -> Ignored
        | _ -> Ignored
            
    let processClientCommand key command (game: Game) =
        let player = game.Settings.PlayerMapping
                     |> Map.tryFind key
                     |> Option.defaultValue {Player.Team = Team.BVB; Number = 0}
                     
        match command with
        | ClientMove direction -> Move (player, direction)
        | ClientKick -> Kick player
        |> (fun c ->
                let result = processCommand c game
                (c, result))
        