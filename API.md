# Metro Game Server API

This document describes the API for interacting with the game through the websocket server.


# Commands

Note: All of these commands can return Errors, along with the error message and stack trace.


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
	  - station_name: name of the station insert
	  - insert_index: index to insert at
	- remove_station: "arguments" needs to contain line_index && (station_index || station_name).
	  - line_index: index of the line to remove on.
	  - station_index: index of the station to remove.
	  - station_name: name of the station to remove.
	- remove_track: "arguments" needs to contain the following:
	  - line_index: index of the line to remove.
	- add_train: "arguments" needs to contain the following:
    	  - line_index: index of the line to add a train to
    	- remove_train: "arguments" needs to contain the following:
    	  - line_index: index of the line to remove a train from


- Notes:  Inserting a station to an empty line will not create a visual change in the game until the second station is added.  Once two stations are added to a transport line, a train is added and the line begins to function.  When inserting a station at an index that already contains a station, the station at and after that index are moved down the line to make room for the new station.  If both a station index and station name are provided the station name is used and the index is ignored.
	

#### outputs

- Status: "Success". String.
- ActionID: ID of action just queued. Poll for action queue status with get_action_queue and get_action_finished commands. int.


#### Examples

Sending the following commands:
```
{"command":"take_action", "game_id":0, "arguments":{"action":"insert_station", "line_index":0, "station_index":0, "insert_index":0}} 
//Station 0 is added to the beginning of line 0, with only 1 station the line is not yet visible

{"command":"take_action", "game_id":0, "arguments":{"action":"insert_station", "line_index":0, "station_index":1, "insert_index":1}} 
//Station 1 is added to the end of line 0, now the line is visible and a train is added

{"command":"take_action", "game_id":0, "arguments":{"action":"insert_station", "line_index":0, "station_index":2, "insert_index":1}} 
//Station 2 is added to line 0 before station 1 and after station 0.
```
Would result in the first station being connected to the second station, and the second station being connected to the third station, in the first game, on the first line.

---

## get_potential_actions

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

---

## reset_scene

Resets every game.

#### Inputs

N/A

#### Outputs

N/A

---

## get_action_queue

Gets the current action queue (for all games, as all share a Action Queue ID pool).

#### Inputs

*NA*

#### Outputs

- All of the IDs in the queue. Array of ints.

---

## get_action_finished

Gets whether the action with the supplied action_id is finished. This could be because the action was actually finished, or that it was never queued in the first place. Internally, it just checks whether the id is in the queue, so if it was never queued, it'll still return that it was "finished"

#### Inputs

- action_id: the id of the action to check. int.

#### Outputs

- Whether the action is still being executed. bool.
