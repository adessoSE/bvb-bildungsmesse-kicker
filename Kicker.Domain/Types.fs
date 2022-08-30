namespace Kicker.Domain

open System.Collections.Generic

type Team =
    | BVB = 1
    | ADESSO = 2

type Player = { Team: Team; Number: int }

type GameSettings =
    { FieldHeight: int
      FieldWidth: int
      GoalHeight: int
      Players: Player list
      PlayerMapping: Map<string, Player> }
    
type GameSettings with
    static member create fieldHeight goalHeight =
        if Utils.isEven fieldHeight then failwith "Nur ungerade Höhe ist erlaubt"
        if Utils.isEven goalHeight then failwith "Nur ungerade Torhöhe ist erlaubt"
        
        let widthToHeightRatio = 1.5441
        let fieldWidth = int (float fieldHeight * widthToHeightRatio) + 2 |> Utils.makeOdd
        
        { FieldHeight = fieldHeight
          FieldWidth = fieldWidth
          GoalHeight = goalHeight
          Players = [
              for i in 1..5 do yield { Player.Team = Team.ADESSO; Number = i }
              for i in 1..5 do yield { Player.Team = Team.BVB; Number = i }
          ]
          PlayerMapping = Map.empty }
    static member defaultSettings = GameSettings.create 9 3
    member this.goalTop = (this.FieldHeight - this.GoalHeight) / 2
    member this.goalBottom = this.goalTop + this.GoalHeight
    member this.withPlayerMapping (mapping: IReadOnlyDictionary<string, Player>) =
        { this with PlayerMapping = mapping |> Seq.map(|KeyValue|) |> Map.ofSeq }
    member this.withPlayers (players: Player seq) =
        { this with Players = players |> Seq.toList }

type TileValue =
    | EmptyTile
    | OutTile
    | PlayerTile of Player
    | BallTile

type GameStatus =
    | Running
    | NotRunning
    | StoppedWithGoal
    | StoppedByAdmin

type Coordinate = int * int

type PlayerState =
    { Position: Coordinate
      Player: Player }

type GameState =
    { Settings: GameSettings
      Players: PlayerState array
      BallPosition: Coordinate
      Status: GameStatus
      PreviousStatus: GameStatus }

type Direction =
    | Up = 0
    | Down = 1
    | Left = 2
    | Right = 3

type MovedObject =
    | MovedBall of Coordinate
    | MovedPlayer of Player * Coordinate

type CommandResult =
    | Ignored
    | BlockedByObstacle
    | PlayerNotFound
    | Paused
    | Resumed
    | Moved of MovedObject list
    | Goal of (MovedObject list * Player)
    
type GameCommand =
    | TogglePause
    | Move of Player * Direction
    | Kick of Player
    
type ClientCommand =
    | ClientMove of Direction
    | ClientKick
    
type GameNotification =
    | State of GameState
    | ResultNotification of (GameCommand * CommandResult)
