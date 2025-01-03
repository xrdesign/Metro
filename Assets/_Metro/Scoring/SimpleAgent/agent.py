import websocket
import json
from time import sleep
import random
from typing import List
from MetroWrapper import GameState
import MetroWrapper
from path_finder_utils import GeometryUtils, AStarPathFinder, DijkstraPathFinder

def send_and_recieve(ws, message):
    tries = 0
    ws.send(message)
    while tries < 3:
        try:
            data = ws.recv()
            return data
        except websocket.WebSocketException as e:
            print(f"Receive failed: {e}, trying again")
            tries += 1
            try:
                ws = websocket.create_connection('ws://192.168.1.18:3000/metro')
                ws.send(message)
            except websocket.WebSocketException as e:
                print(f"Connection failed: {e}")
                tries += 1

def insert_station(ws, line, station, insert, game_id = 0):
    command =  {
        "command":"take_action",
        "game_id":game_id,
        "arguments":{
            "action":"insert_station",
            "line_index":line,
            "station_index":station,
            "insert_index":insert
        }
    }
    res = send_and_recieve(ws, json.dumps(command))

def remove_station(ws, line, station):
    command =  {
        "command":"take_action",
        "game_id":0,
        "arguments":{
            "action":"remove_station",
            "line_index":line,
            "station_index":station
        }
    }
    res = send_and_recieve(ws, json.dumps(command))

def remove_track(ws, line):
    command =  {
        "command":"take_action",
        "game_id":0,
        "arguments":{
            "action":"remove_track",
            "line_index":line
        }
    }
    res = send_and_recieve(ws, json.dumps(command))


class CostHandler:
    @staticmethod
    def calculate_path_length(all_stations, planned_paths):
        total_length = 0
        for path in planned_paths:
            for i in range(len(path) - 1):
                station_a = path[i].pos
                station_b = path[i + 1].pos
                total_length += GeometryUtils.distance_between_points(station_a, station_b)
        return total_length

    @staticmethod
    def dijkstra_routes_cost(all_stations, planned_paths):
        path_finder = DijkstraPathFinder(stations=all_stations, planned_paths=planned_paths)
        return path_finder.find_all_routes(print_data=False)

    @staticmethod
    def astar_routes_cost(all_stations, planned_paths):
        path_finder = AStarPathFinder(stations=all_stations, planned_paths=planned_paths)
        return path_finder.find_all_routes(print_data=False)


# New Agent and BruteForceAgent classes for SpaceTransit
class Agent:
    def __init__(self, ws, game_id = 0):
        self.ws = ws  # WebSocket instance to communicate with SpaceTransit
        self.num_paths = None  # Number of paths, initialized later
        self.all_stations = None  # All available stations, initialized later
        self.planned_paths = []  # List to store planned paths
        self.cost = float('inf')
        self.init = False
        self.game_id = game_id

    def initialize_records(self, game_state):
        # Initialize paths based on the game state
        self.num_paths = len(game_state.lines)
        self.all_stations = game_state.stations
        self.planned_paths = [[] for _ in range(self.num_paths)]
        self.cost = float('inf')
        self.init = True


    def check_for_changes(self, game_state):
        if self.num_paths != len(game_state.lines) or len(self.all_stations) != len(game_state.stations):
            print("Gamestate Update!!!")
            if self.num_paths != len(game_state.lines):
                print(f"Number of paths has changed from {self.num_paths} to {len(game_state.lines)}.")
            if len(self.all_stations) != len(game_state.stations):
                print(f"Number of stations has changed from {len(self.all_stations)} to {len(game_state.stations)}.")
            return True
        return False

    def get_paths(self):
        """
        Abstract method to be implemented by subclasses.
        Should return a list of lists, where each inner list represents a planned path.
        """
        raise NotImplementedError("Subclasses must implement get_paths() to return a list of planned paths.")


    def find_closest(self, current_station, stations):
        closest = None
        min_distance = float('inf')
        for station in stations:
            distance = GeometryUtils.distance_between_points(station.pos, current_station.pos)
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

    def get_better_paths(self, cost_function, game_state, update_to_game):
        """
        Iteratively generates new paths and evaluates their cost using a specified cost function.
        Updates the planned paths if a better (lower cost) set of paths is found.
        The process continues for a maximum of 'num_steps' iterations or until no better path is found.

        Args:
            cost_function (callable): The name of the static method from CostHandler to use for calculating the cost.
        """
        # Ensure that the cost function provided is callable
        if not callable(cost_function):
            print(f"Error: The provided cost function is not callable.")
            return

        # Check if the game state has changed and reinitialize paths if needed
        if not self.init or self.check_for_changes(game_state):
            self.initialize_records(game_state)

        # Step 1: Use get_paths to generate new paths
        try:
            new_paths = self.get_paths()
        except NotImplementedError:
            print("Error: get_paths() must be implemented by a subclass.")
            return

        # Step 2: Calculate the cost of the new paths using the specified cost function
        new_cost = cost_function(all_stations=self.all_stations, planned_paths=new_paths)

        # Step 3: If the cost is lower than the current self.cost, update planned_paths and self.cost
        if new_cost < self.cost:
            print(f"Found a better path with a lower cost: {new_cost} (previous cost: {self.cost})!")
            previous_paths = self.planned_paths
            self.planned_paths = new_paths
            self.cost = new_cost
            # Send the planned paths to the game using WebSocket
            if update_to_game:
                for line_index, station_list in enumerate(previous_paths):
                    remove_track(self.ws, line_index)

                # print("Insert: ")
                for line_index, station_list in enumerate(self.planned_paths):
                    # print(f"line_index: {line_index}")
                    for insert_index, station in enumerate(station_list):
                        # print(f"{station.id} ", end=" ")
                        insert_station(self.ws, line_index, station.id, insert_index)
                    # print("\n")
            # dij_path_finder = DijkstraPathFinder(stations=self.all_stations, planned_paths=self.planned_paths)
            # dij_path_finder.find_all_routes(print_data=True)

            # ast_path_finder = AStarPathFinder(stations=self.all_stations, planned_paths=self.planned_paths)
            # ast_path_finder.find_all_routes(print_data=True)




def check_whether_not_crossed(station, station_list):
    assert station_list is not None
    # print("checking============================")
    whether_not_crossed = True
    if len(station_list)<=1:
        return whether_not_crossed
    for other_station in station_list:
        # print("??????????????????????")
        # print(f"other_station: {other_station.id}\nstation: {station.id}")
        if other_station.id == station.id:
            whether_not_crossed = False
    return whether_not_crossed

def check_whether_loop(station, station_list):
    assert station_list is not None
    assert len(station_list)>1
    if station_list[0] == station:
        return True
    else:
        return False

class StochasticGreedyAgent(Agent):
    def get_paths(self):
        planned_paths = [[] for _ in range(self.num_paths)]

        # Initial assignment with duplicate checking
        for station in self.all_stations:
            selected_path_id = random.randint(0, self.num_paths - 1)
            planned_paths[selected_path_id].append(station)

        # Ensure minimum stations with duplicate checking
        for station_list in planned_paths:
            if len(station_list) < 1:
                station_id = random.randint(0, len(self.all_stations) - 1)
                station_list.append(self.all_stations[station_id])
            if len(station_list) < 2:
                station_id = random.randint(0, len(self.all_stations) - 1)
                while self.all_stations[station_id] == station_list[0]:
                    station_id = random.randint(0, len(self.all_stations) - 1)
                station_list.append(self.all_stations[station_id])

        # Optionally, each path can randomly include additional stations
        for station_list_id in range(len(planned_paths)):
            station_list = planned_paths[station_list_id]
            additional_stations = random.sample(self.all_stations, random.randint(0, len(self.all_stations) // 2))
            whether_loop = False
            for station in additional_stations:
                if station == station_list[0]:
                    whether_loop = True
                if check_whether_not_crossed(station, station_list):
                    station_list.append(station)
            # Order the List
            planned_paths[station_list_id] = self.order_stations(station_list)
            # if whether_loop:
            #     planned_paths[station_list_id].append(planned_paths[station_list_id][0])

        return planned_paths

if __name__ == "__main__":
    game_count = 2
    ws = websocket.create_connection('ws://localhost:3000/metro')
    agents = []

    numStations = 0
    def getGamesCommand(game_id = 0):
        return {
            'command': 'get_state',
            'game_id': game_id
        }

    for i in range(game_count): # two games for now
        agent = StochasticGreedyAgent(ws, i)
        agents.append(agent)
    cnt = [0, 0]
    while True:
        for i in range(game_count):
            gameStateRaw = send_and_recieve(ws, json.dumps(getGamesCommand(i)))
            try:
                gameState = json.loads(gameStateRaw)
            except:
                print("Failed to parse game state:")
                print(gameStateRaw)
                continue
            game = MetroWrapper.GameState(gameState)
            stations = game.stations
            if len(stations) > 0:
                agents[i].get_better_paths(
                    cost_function=CostHandler.astar_routes_cost,
                    game_state=game,
                    update_to_game=True
                )
            cnt[i] += 1
            if cnt[i] % 10 == 1:
                print(f"game {i} status update:")
                print(f"score: {game.score}")
                print(f"time: {game.time} \n")
        sleep(0.2)

        # # get next game state:
        # gameStateRaw = send_and_recieve(ws, json.dumps(getGamesCommand()))
        # try:
        #     gameState = json.loads(gameStateRaw)
        # except:
        #     print(gameStateRaw)
        #     exit()
        # game = GameState(gameState)
        # stations = game.stations
        # if len(stations) > 0:
        #     agent.get_better_paths(
        #         cost_function=CostHandler.calculate_path_length,
        #         game_state=game,
        #         update_to_game=True
        #     )
        # sleep(1)

