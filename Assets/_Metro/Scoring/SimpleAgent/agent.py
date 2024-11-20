import sys
import math
import websocket
import importlib
import json
from nltk import edit_distance
from time import sleep
from pprint import pprint
from functools import partial
import copy, random

import MetroWrapper

def send_and_recieve(ws, message):
    tries = 0
    ws.send(message)
    while tries < 3:
        try:
            data = ws.recv()
            return data
        except:
            print("recieve failed trying again")
            tries += 1
            ws = websocket.create_connection('ws://localhost:3000/metro')
            ws.send(message)

def insert_station(ws, line, station, insert):
    command =  {
        "command":"take_action",
        "game_id":0,
        "arguments":{
            "action":"insert_station",
            "line_index":line,
            "station_index":station,
            "insert_index":insert
        }
    }
    res = send_and_recieve(ws, json.dumps(command))

def connect_unconnect_stations(ws, game):
    stations = game.stations
    if len(stations) == 3:
        # Initial Condition
        insert_station(ws, 0, 0, 0)
        insert_station(ws, 0, 1, 1)

        insert_station(ws, 1, 1, 0)
        insert_station(ws, 1, 2, 1)

        insert_station(ws, 2, 2, 0)
        insert_station(ws, 2, 0, 1)

        return

    print("GameState Before:")
    print(game.Evaluate())
    new_station = stations[-1]

    lowestCost = 1000000000000
    bestInsert = (0,0,0)
    bestGame = None
    game.Print()
    for line in game.lines:
        if(len(line.segments) <= 0):
            continue
        for insert_index in range(len(line.segments) + 1):
            gameCopy = copy.deepcopy(game)
            gameCopy.InsertStation(new_station.id, line.id, insert_index)
            gameCopy.UpdateSegments();
            gameCopy.UpdateNeighbors();
            score = 0
            for newline in gameCopy.lines:
                score += newline.totalLength
            if(score <= lowestCost):
                lowestCost = score
                bestInsert = (line.id, new_station.id, insert_index)
                print("new lowest cost: ")
                print(lowestCost)
                print(bestInsert)
                bestGame = copy.deepcopy(gameCopy)
    print(lowestCost)
    command =  {
        "command":"take_action",
        "game_id":0,
        "arguments":{
            "action":"insert_station",
            "line_index":bestInsert[0],
            "station_index":bestInsert[1],
            "insert_index":bestInsert[2]
        }
    }

    res = send_and_recieve(ws, json.dumps(command))


    # Evaluate:
    sleep(1)
    getGamesCommand = {
        'command': 'get_state',
        'game_id': 0
    }
    gameStateRaw = send_and_recieve(ws, json.dumps(getGamesCommand))
    gameState = json.loads(gameStateRaw)
    updatedGame = MetroWrapper.GameState(gameState)

    print("Expected GameState:")
    print(bestGame.Evaluate())
    print("Score after action: ")
    print(updatedGame.Evaluate())
    print("")
    return

    # old method:
    connected_stations = set()
    for segment in game.segments:
        connected_stations.add(segment.a);
        connected_stations.add(segment.b);

    for station in stations:
        if station.id in connected_stations:
            continue

        nearestSegment = None
        min = 9999999
        for segment in game.segments:
            distance = get_distance(station.pos, game, segment)
            if distance < min:
                min = distance
                nearestSegment = segment

        connect_along_segment(ws, station, nearestSegment)
        connected_stations.add(station.id)
        continue

def connect_along_segment(ws, station, segment):
    command =  {
        "command":"take_action",
        "game_id":0,
        "arguments":{
            "action":"insert_station",
            "line_index":segment.l,
            "station_index":station.id,
            "insert_index":segment.index
        }
    }
    print("Sending Command: ")
    print(command)
    res = send_and_recieve(ws, json.dumps(command))
    print("Connected Staiton!")
    print(station.id)

def dot(a, b):
    return (a[0] * b[0]) + (a[1] * b[1]) + (a[2] * b[2])

def vec_from_points(a, b):
    return (b[0] - a[0], b[1] - a[1], b[2] - a[2])

def distance_between_points(a,b):
    l = vec_from_points(a,b)
    return math.sqrt(dot(a,b))

def get_distance(stationPos, game, segment):
    a = game.stations[segment.a].pos
    b = game.stations[segment.b].pos

    fromToStation = vec_from_points(a, stationPos)
    fromToEnd = vec_from_points(a, b)

    projAmount = dot(fromToStation, fromToEnd) / dot(fromToEnd, fromToEnd)
    proj = (fromToEnd[0] * projAmount, fromToEnd[1] * projAmount, fromToEnd[2] * projAmount)

    nearestPoint = (a[0] + proj[0], a[1] + proj[1], a[2] + proj[2])
    return distance_between_points(nearestPoint, stationPos)

# New Agent and DummyAgent classes for SpaceTransit
class Agent:
    def __init__(self, ws):
        self.ws = ws  # WebSocket instance to communicate with SpaceTransit
        self.num_paths = None  # Number of paths, initialized later
        self.all_stations = None  # All available stations, initialized later
        self.planned_paths = []  # List to store planned paths

    def initialize_paths(self, game_state):
        # Initialize paths based on the game state
        self.num_paths = len(game_state.lines)
        self.all_stations = game_state.stations
        self.planned_paths = [[] for _ in range(self.num_paths)]

    def generate_paths(self):
        pass  # Abstract method to be implemented by subclasses

    def find_closest(self, current_station, stations):
        closest = None
        min_distance = float('inf')
        for station in stations:
            distance = math.sqrt((station.pos[0] - current_station.pos[0]) ** 2 +
                                 (station.pos[1] - current_station.pos[1]) ** 2 +
                                 (station.pos[2] - current_station.pos[2]) ** 2)
            if distance < min_distance:
                min_distance = distance
                closest = station
        return closest

    def order_stations(self, station_list):
        if not station_list:
            return []

        ordered_list = [station_list[0]]  # Start with the first station
        remaining_stations = station_list[1:]  # Remaining stations to be ordered

        while remaining_stations:
            current_station = ordered_list[-1]  # Get the last added station
            next_station = self.find_closest(current_station, remaining_stations)
            ordered_list.append(next_station)
            remaining_stations.remove(next_station)

        return ordered_list

class DummyAgent(Agent):
    def generate_paths(self, game_state):
        # Initialize paths based on the current game state
        self.initialize_paths(game_state)

        # Assign each station to at least one path randomly
        for station in self.all_stations:
            selected_path_id = random.randint(0, self.num_paths - 1)
            self.planned_paths[selected_path_id].append(station)

        # Ensure all lines connect to at least two different stations
        for station_list in self.planned_paths:
            if len(station_list) < 1:
                station_id = random.randint(0, len(self.all_stations) - 1)
                station_list.append(self.all_stations[station_id])
            if len(station_list) < 2:
                station_id = random.randint(0, len(self.all_stations) - 1)
                while self.all_stations[station_id] == station_list[0]:
                    station_id = random.randint(0, len(self.all_stations) - 1)
                station_list.append(self.all_stations[station_id])

        # Optionally, each path can randomly include additional stations
        for station_list_id in range(len(self.planned_paths)):
            station_list = self.planned_paths[station_list_id]
            additional_stations = random.sample(self.all_stations, random.randint(0, len(self.all_stations) // 2))
            for station in additional_stations:
                station_list.append(station)
            # Order the List
            self.planned_paths[station_list_id] = self.order_stations(station_list)

        # Send the planned paths to the game using WebSocket
        for line_index, station_list in enumerate(self.planned_paths):
            for insert_index, station in enumerate(station_list):
                insert_station(self.ws, line_index, station.id, insert_index)

if __name__ == "__main__":
    ws = websocket.create_connection('ws://localhost:3000/metro')

    numStations = 0
    getGamesCommand = {
        'command': 'get_state',
        'game_id': 0
    }

    while True:
        # get next game state:
        gameStateRaw = send_and_recieve(ws, json.dumps(getGamesCommand))
        try:
            gameState = json.loads(gameStateRaw)
        except:
            print(gameStateRaw)
            exit()
        game = MetroWrapper.GameState(gameState)
        stations = game.stations
        if len(stations) > numStations:
            numStations = len(stations)
            connect_unconnect_stations(ws, game)
        sleep(1)

    # ws = websocket.create_connection('ws://localhost:3000/metro')
    # agent = DummyAgent(ws)

    # getGamesCommand = {
    #     'command': 'get_state',
    #     'game_id': 0
    # }

    # while True:
    #     # get next game state:
    #     gameStateRaw = send_and_recieve(ws, json.dumps(getGamesCommand))
    #     try:
    #         gameState = json.loads(gameStateRaw)
    #     except:
    #         print(gameStateRaw)
    #         exit()
    #     game = MetroWrapper.GameState(gameState)
    #     stations = game.stations
    #     if len(stations) > 0:
    #         agent.generate_paths(game)
    #     sleep(1)

