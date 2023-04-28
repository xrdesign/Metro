# Metro Game Server API

This document describes the API for interacting with the game through the websocket server.


# Commands

Note: All of these commands can return "Error, I don't understand your command". string.


---

## get_state

Get the state of a single game.

#### Inputs

- game_id: The game_id of the game to retrieve serialized state from. unsigned integer

#### Outputs
- score: The game's current score. float
- time: The game's current time in seconds. float
- isPause: Whether the game is currently paused. boolean
- isGameover: Whether the game is currently ended. boolean
- freeTrains: Number of free trains currently available between all transport lines in the game. integer
- stations: Array of stations contained within the game.
  - id: ID of station. This is relative to game, so stations from separate games can have the same ids. integer
  - unique_id: Unique ID of station. This is it's unity uuid, so it is unique among all stations, even between games. integer
  - type: "station"
  - shape: Station shape. Either "Sphere", "Cone", or "Cube"
  - x: station x position in world space. float
  - y: station y position in world space. float
  - z: station z position in world space. float
  - timer: Game over timer on station. Counts up while full with passengers. 47 full seconds of full passengers means game over.
  - cnt_[destination]: Number of passengers bound for certain station types. cnt_sphere, cnt_cone, cnt_cube. integer
- lines: Array of transport lines that the game has.
  - id: ID of transport line. This is relative to game, so lines from separate games can have the same ids. integer
  - unique_id: Unique ID of transport line. This is it's unity uuid, so it is unique among all lines, even between games. integer
  - type: "line"
- trains: Array of trains that the game currently has.
  - unique_id: Unique ID of the train. This is it's unity uuid, so it is unique among all trains, even between games. integer
  - type: "train"
  - position: 0 -> 1 position of train along its track. float
  - speed: Speed of the train. float
  - direction: Direction the train is headed: -1 or 1. float
  - line_id: uuid of the transport line this train is apart of. integer
  - cnt_[destination]: Number of passengers bound for certain station types. cnt_sphere, cnt_cone, cnt_cube. integer
- segments: Array of segments of track for game.
  - type: "segment"
  - length: 20
  - which_line: the uuid of the transport line this segment is apart of. integer
  - from_station: the uuid of the first station in the segment. integer
  - to_station: the uuid of the second station in the segment. integer


---

## take_action

Take some specified action on the game.

#### Inputs

- game_id The game_id of the game to queue an action for.
- arguments: Arguments of the action to queue.
  - action: The action to take. Can currently be "insert_station", "remove_station", or "remove_track" Options laid down below.
    - insert_station: "arguments" needs to contain line_index && (station_index || station_name) && insert_index.
	  - line_index: the index of the line to insert the station at.
	  - station_index: index of the station.
	  - station_name: name of the station to remove.
	  - insert_index: index to insert at
	- remove_station: "arguments" needs to contain line_index && (station_index || station_name).
	  - line_index: index of the line to remove on.
	  - station_index: index of the station to remove.
	  - station_name: name of the station to remove.
	- remove_track: "arguments" needs to contain thhe following:
	  - line_index: index of the line to remove.
	

#### outputs

Returns back "ERROR: \<Error Message\>". string. *unless* the action was valid, in which case it returns "Success"


#### Examples

Sending the following commands:
```
{"command":"take_action", "game_id":0, "arguments":{"action":"insert_station", "line_index":0, "station_index":0, "insert_index":0}}
{"command":"take_action", "game_id":0, "arguments":{"action":"insert_station", "line_index":0, "station_index":1, "insert_index":1}}
{"command":"take_action", "game_id":0, "arguments":{"action":"insert_station", "line_index":0, "station_index":2, "insert_index":2}}
```
Would result in the first station being connected to the second station, and the second station being connected to the third station, in the first game, on the first line.

---

## get_actions

Get all actions you are allowed to use via take_action

#### Inputs

*NA*

#### Outputs

- (Array)
  - "insert_station"
  - "remove_station"
  - "remove_track"


---

## reset_game

Resets a specific game.

#### Inputs

- game_id: The id of the game to reset.

#### Outputs

For output, it returns the serialized gamestate of the game that was reset. This is identical to the output of the above get_state command.