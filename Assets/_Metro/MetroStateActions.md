
# H-AT

## SpaceMetro Game State

- Current Score
- Current Time
- number of trains free / total
- number of cars free / total
- number of interchange upgrades

- [Station] list (max size 50+?)
  - Station
    - type
    - position
    - time until overcrowded
    - isInterchange
    - [Passenger] list of waiting (max size?)
      - Passenger
        - desired destinaiton Type

- [TransportLine] list (max size ~7)
  - TransportLine
    - isDeployed
    - [Station] path list as nodes
    - [TrackSegment] path list as links
      - TrackSegment
        - Station1
        - Station2
        - index
    - [Train] list
      - Train
        - position along line
        - speed
        - direction
        - number of cars
        - [Passenger] list
  

## Valid Game Actions

- Edit TransportLines
  - Insert Station to path list at index I
  - Remove Station from path list at index I
  - Remove all Stations from path list
  - Add Train at Position on line and Direction
  - Add car to Train X
  - Move Train X from TransportLine T to U at Pos and Dir

- Make Station Interchange




















## useful utility methods
- closest stations of type in space
- closest path to station of type along TransportLine(s)
- transportLine path distance
